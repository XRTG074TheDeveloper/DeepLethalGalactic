using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace DLGMod.StartPatches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class WelcomeSpeechPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void SetUpStarterSound(StartOfRound __instance)
        {
            __instance.shipIntroSpeechSFX = DLGModMain.MissionControlQuotesSFX[0];
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
            else if (__instance.groupCredits >= 60)
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
    }
}
