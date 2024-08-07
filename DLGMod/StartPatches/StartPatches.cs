﻿using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace DLGMod.StartPatches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class WelcomeSpeechPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StarterSetUp(StartOfRound __instance)
        {
            __instance.shipIntroSpeechSFX = DLGModMain.MissionControlQuotesSFX[2];
        }

        [HarmonyPatch("OnShipLandedMiscEvents")]
        [HarmonyPostfix]
        public static void CheckChosenLevel(StartOfRound __instance)
        {
            if (__instance.currentLevelID != 3)
            {
                DLGModMain.SendAmmunition(__instance.connectedPlayersAmount + 1);
            }
        }

    }

    [HarmonyPatch(typeof(Terminal))]
    internal class AmmunitionPatch
    {
        internal static bool shouldBeSent = false;

        internal static bool resupplyOrdered = false;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void SendAmmunition(Terminal __instance)
        {
            if (!shouldBeSent) return;
            if (!__instance.IsHost) return;

            Item shotgunItem = Resources.FindObjectsOfTypeAll<AllItemsList>()[0].itemsList[59];
            Item shotgunShell = Resources.FindObjectsOfTypeAll<AllItemsList>()[0].itemsList[60];

            __instance.buyableItemsList[0] = shotgunItem;
            __instance.buyableItemsList[1] = shotgunShell;

            List<int> ammunitionOrder = new List<int>();

            if (!resupplyOrdered)
            {
                for (int i = 0; i < DLGModMain.playersAmount; i++)
                {
                    ammunitionOrder.Add(0);
                    ammunitionOrder.Add(1);
                    ammunitionOrder.Add(1);
                    ammunitionOrder.Add(1);
                    ammunitionOrder.Add(1);
                    ammunitionOrder.Add(1);
                    ammunitionOrder.Add(1);
                    ammunitionOrder.Add(1);
                }

                __instance.orderedItemsFromTerminal = ammunitionOrder;
                __instance.numberOfItemsInDropship = ammunitionOrder.Count;
            }
            else
            {
                for (int i = 0; i < DLGModMain.playersAmount; i++)
                {
                    ammunitionOrder.Add(1);
                    ammunitionOrder.Add(1);
                    ammunitionOrder.Add(1);
                    ammunitionOrder.Add(1);
                    ammunitionOrder.Add(1);
                    ammunitionOrder.Add(1);
                    ammunitionOrder.Add(1);
                }

                __instance.orderedItemsFromTerminal = ammunitionOrder;
                __instance.numberOfItemsInDropship = ammunitionOrder.Count;

                resupplyOrdered = false;
            }

            shouldBeSent = false;
        }
    }

    [HarmonyPatch(typeof(HUDManager))]
    internal class ChatCommandsPatch
    {
        internal static bool isConfirmation = false;

        [HarmonyPatch("AddPlayerChatMessageClientRpc")]
        [HarmonyPostfix]
        public static void GetMessage(ref string chatMessage, ref int playerId, HUDManager __instance)
        {
            if (!__instance.IsHost) return;
            if (playerId == -1) return;

            if (chatMessage == "/resupply" && !isConfirmation)
            {
                isConfirmation = true;
                __instance.AddTextToChatOnServer("Please confrim resupply order by sending '/confirm' message or deny it by sending any other");
            }
            else if (chatMessage == "/confirm" && isConfirmation)
            {
                isConfirmation = false;
                __instance.AddTextToChatOnServer($"{__instance.playersManager.allPlayerScripts[playerId].playerUsername} ordered a resupply!");
                DLGModMain.SendResupply(DLGModMain.playersAmount);
            }
            else if (isConfirmation && chatMessage != "/resupply")
            {
                isConfirmation = false;
                __instance.AddTextToChatOnServer($"Order is cancelled by {__instance.playersManager.allPlayerScripts[playerId].playerUsername}");
            }
        }

        [HarmonyPatch("AddTextMessageClientRpc")]
        [HarmonyPrefix]
        public static void GetSwarmMessage(ref string chatMessage)
        {
            DLGModMain.logger.LogInfo(chatMessage);

            TimeOfDay timeOfDay = GameObject.FindObjectOfType<TimeOfDay>();

            switch (chatMessage)
            {
                case "SWARM!":
                    timeOfDay.TimeOfDayMusic.volume = 1f;
                    timeOfDay.TimeOfDayMusic.PlayOneShot(DLGModMain.MissionControlQuotesSFX[1], 1f);
                    timeOfDay.TimeOfDayMusic.PlayOneShot(DLGModMain.swarmSFX[1], 1f);
                    return;
                case "THEY ARE HERE!!!":
                    timeOfDay.TimeOfDayMusic.clip = DLGModMain.swarmSFX[0];
                    timeOfDay.TimeOfDayMusic.Play();
                    timeOfDay.TimeOfDayMusic.loop = true;
                    SwarmPatch.isSwarmSFXLooped = true;
                    return;
                case "SWARM IS ALMOST OVER":
                    timeOfDay.TimeOfDayMusic.PlayOneShot(DLGModMain.MissionControlQuotesSFX[0], 1f);
                    timeOfDay.TimeOfDayMusic.loop = false;
                    SwarmPatch.isSwarmSFXLooped = false;
                    SwarmPatch.isSwarmSFXFading = true;
                    return;
            }
        }
    }

    [HarmonyPatch(typeof(TimeOfDay))]
    internal class SwarmPatch
    {
        internal static float chance = 0;

        internal static bool isSwarm = false;
        internal static bool isSwarmSFXLooped = false;
        internal static bool isSwarmSFXFading = false;

        internal static HUDManager hudManager = GameObject.FindObjectOfType<HUDManager>();

        [HarmonyPatch("SyncTimeClientRpc")]
        [HarmonyPostfix]
        public static void test(TimeOfDay __instance)
        {
            if (!__instance.IsHost) return;

            RoundManager roundManager = GameObject.FindObjectOfType<RoundManager>();

            int rollDice = UnityEngine.Random.Range(30, 100);

            int enemiesAmount = 0;

            DLGModMain.logger.LogInfo(__instance.TimeOfDayMusic.loop);

            foreach (EnemyAI enemy in GameObject.FindObjectsOfType<EnemyAI>())
            {
                if (!enemy.isEnemyDead && (enemy.enemyType.enemyName == "Crawler" || enemy.enemyType.enemyName == "Bunker Spider")) enemiesAmount++;
            }

            if (rollDice <= chance && enemiesAmount < 3)
            {
                for (int i = 0; i < (DLGModMain.playersAmount) * 9; i++)
                {
                    int enemyToSpawn = UnityEngine.Random.Range(1, 4) == 1 ? 1 : 4;
                    EnemyVent vent = roundManager.allEnemyVents[UnityEngine.Random.Range(1, roundManager.allEnemyVents.Length)];

                    roundManager.SpawnEnemyOnServer(vent.transform.position, vent.transform.eulerAngles.y, enemyToSpawn);
                }

                isSwarm = true;

                hudManager.AddTextToChatOnServer("SWARM!");

                chance = 0f;
            }
            else if (enemiesAmount >= 3)
            {
                chance += 0.25f;
            }
            else
            {
                if (isSwarm)
                {
                    hudManager.AddTextToChatOnServer("SWARM IS ALMOST OVER");
                    isSwarm = false;
                }

                chance += 10f;
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void ChangeSwarmSFX(TimeOfDay __instance)
        {
            if (isSwarm && !__instance.TimeOfDayMusic.isPlaying && !isSwarmSFXLooped)
            {
                hudManager.AddTextToChatOnServer("THEY ARE HERE!!!");
            }
            if (!isSwarm && __instance.TimeOfDayMusic.isPlaying)
            {
                if (isSwarmSFXFading)
                {
                    if (__instance.TimeOfDayMusic.volume > 0)
                    {
                        __instance.TimeOfDayMusic.volume -= 0.08f * Time.deltaTime;
                    }
                    else
                    {
                        __instance.TimeOfDayMusic.Stop();
                        SwarmPatch.isSwarmSFXFading = false;
                    }
                }
            }
        }
    }
}
