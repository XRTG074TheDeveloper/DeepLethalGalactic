using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace DLGMod.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class GameControllerPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StarterSetUp(StartOfRound __instance)
        {
            __instance.shipIntroSpeechSFX = DLGModMain.MissionControlQuotesSFX[2];

            SwarmPatch.isSwarm = false;
            SwarmPatch.isSwarmSFXFading = false;
            SwarmPatch.chance = 0;
            SwarmPatch.dangerLevel = 5f;

            GameObject.FindObjectOfType<TimeOfDay>().TimeOfDayMusic.Stop();

            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0].compatibleNouns[42].result.displayText =
            "You are not able to order ammunition pack unless you are on the mission!\n\n";
            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0].compatibleNouns[42].result.terminalOptions = null;

            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0].compatibleNouns[43].result.displayText =
            "You are not able to order supply drop unless you are on the mission!\n\n";
            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0].compatibleNouns[43].result.terminalOptions = null;

            SwarmPatch.swarmEnemiesIndex.Clear();

            SwarmPatch.hudManager = GameObject.FindObjectOfType<HUDManager>();
        }

        [HarmonyPatch("StartGame")]
        [HarmonyPostfix]
        public static void OnGameStarted(StartOfRound __instance)
        {
            if (__instance.currentLevelID != 3)
            {
                DLGModMain.SendAmmunition(__instance.connectedPlayersAmount + 1);

                GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0].compatibleNouns[42].result.displayText =
                "You have requested to order ammunition pack. Amount: [variableAmount].\n" +
                "Total cost of items: [totalCost].\n\n" +
                "Please CONFIRM or DENY\n";
                GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0].compatibleNouns[42].result.terminalOptions = AmmunitionPatch.ammunitionPackTO;

                GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0].compatibleNouns[43].result.displayText =
                "You have requested to order resupply drop. Amount: [variableAmount].\n" +
                "Total cost of items: [totalCost].\n\n" +
                "Please CONFIRM or DENY\n";
                GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0].compatibleNouns[43].result.terminalOptions = AmmunitionPatch.supplyDropTO;

                SwarmPatch.dangerLevel = 5f;

                foreach (char ch in __instance.currentLevel.riskLevel)
                {
                    switch (ch)
                    {
                        case 'A':
                            SwarmPatch.dangerLevel += 10f;
                            break;
                        case 'S':
                            SwarmPatch.dangerLevel += 30f;
                            break;
                        case '+':
                            if (SwarmPatch.dangerLevel == 15f)
                            {
                                SwarmPatch.dangerLevel += 10f;
                            }
                            else
                            {
                                SwarmPatch.dangerLevel += 20f;
                            }
                            break;
                    }
                }
            }
        }

        [HarmonyPatch("ShipLeave")]
        [HarmonyPostfix]
        public static void OnShipStartLeaving()
        {
            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0].compatibleNouns[42].result.displayText =
            "You are not able to order ammunition pack unless you are on the mission!\n\n";
            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0].compatibleNouns[42].result.terminalOptions = null;

            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0].compatibleNouns[43].result.displayText =
            "You are not able to order supply drop unless you are on the mission!\n\n";
            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0].compatibleNouns[43].result.terminalOptions = null;

            SwarmPatch.isSwarm = false;
            SwarmPatch.isSwarmSFXFading = true;
            SwarmPatch.chance = 0;
        }
    }

    [HarmonyPatch(typeof(Terminal))]
    internal class AmmunitionPatch
    {
        internal static bool shouldBeSent = false;

        internal static bool resupplyOrdered = false;

        internal static bool purchaseEnabled = false;

        internal static List<Item> allItemsList = Resources.FindObjectsOfTypeAll<AllItemsList>()[0].itemsList;

        internal static CompatibleNoun[] ammunitionPackTO;
        internal static CompatibleNoun[] supplyDropTO;

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        public static void SetUpAmmunition(Terminal __instance)
        {
            if (!__instance.IsHost) return;

            List<Item> tempBuyableItems = __instance.buyableItemsList.ToList();

            tempBuyableItems.Add(new Item() { itemName = "Ammunition Pack", creditsWorth = 100 }); // Ammunition pack
            tempBuyableItems.Add(new Item() { itemName = "Supply Drop", creditsWorth = 20 }); // Resupply drop

            __instance.buyableItemsList = tempBuyableItems.ToArray();

            List<TerminalKeyword> tempTerminalNodes = __instance.terminalNodes.allKeywords.ToList();

            tempTerminalNodes.Add(new TerminalKeyword() { word = "ammunition pack", defaultVerb = tempTerminalNodes[0], name = "Ammunition Pack" });
            tempTerminalNodes.Add(new TerminalKeyword() { word = "supply drop", defaultVerb = tempTerminalNodes[0], name = "Supply Drop" });

            __instance.terminalNodes.allKeywords = tempTerminalNodes.ToArray();

            List<CompatibleNoun> tempCompatibleNouns = tempTerminalNodes[0].compatibleNouns.ToList();

            TerminalNode ammuntionPackNode = new TerminalNode()
            {
                buyItemIndex = 14,
                isConfirmationNode = false,
                clearPreviousText = true,
                terminalEvent = "",
                displayText = "Yay",
                name = "buyAmmunitionPack2"
            };
            TerminalNode supplyDropNode = new TerminalNode()
            {
                buyItemIndex = 15,
                isConfirmationNode = false,
                clearPreviousText = true,
                terminalEvent = "",
                displayText = "Yay",
                name = "buyResupplyDrop2"
            };

            ammunitionPackTO = new CompatibleNoun[]
            {
                new CompatibleNoun() {noun = tempTerminalNodes[3], result = ammuntionPackNode},
                new CompatibleNoun() {noun = tempTerminalNodes[4], result = tempTerminalNodes[0].compatibleNouns[0].result.terminalOptions[1].result}
            };
            supplyDropTO = new CompatibleNoun[]
            {
                new CompatibleNoun() {noun = tempTerminalNodes[3], result = supplyDropNode},
                 new CompatibleNoun() {noun = tempTerminalNodes[4], result = tempTerminalNodes[0].compatibleNouns[0].result.terminalOptions[1].result}
            };

            tempCompatibleNouns.Add(new CompatibleNoun()
            {
                noun = tempTerminalNodes[139],
                result = new TerminalNode()
                {
                    buyItemIndex = 14,
                    isConfirmationNode = true,
                    clearPreviousText = true,
                    itemCost = 100,
                    overrideOptions = true,
                    displayText =
                    "You are not able to order ammunition pack unless you are on the mission!\n\n",
                    name = "buyAmmunitionPack1",
                    terminalOptions = null
                }
            });
            tempCompatibleNouns.Add(new CompatibleNoun()
            {
                noun = tempTerminalNodes[140],
                result = new TerminalNode()
                {
                    buyItemIndex = 15,
                    isConfirmationNode = true,
                    clearPreviousText = true,
                    itemCost = 20,
                    overrideOptions = true,
                    displayText =
                    "You are not able to order supply drop unless you are on the mission!\n\n",
                    name = "buyResupplyDrop1",
                    terminalOptions = null
                }
            });

            __instance.terminalNodes.allKeywords[0].compatibleNouns = tempCompatibleNouns.ToArray();
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void SendAmmunition(Terminal __instance)
        {
            if (!shouldBeSent) return;
            if (!__instance.IsHost) return;

            List<int> ammunitionOrder = new List<int>();

            if (!resupplyOrdered)
            {
                for (int i = 0; i < DLGModMain.playersAmount; i++)
                {
                    ammunitionOrder.Add(14);
                }

                __instance.orderedItemsFromTerminal = ammunitionOrder;
                __instance.numberOfItemsInDropship = ammunitionOrder.Count;
            }
            else
            {
                for (int i = 0; i < DLGModMain.playersAmount; i++)
                {
                    ammunitionOrder.Add(15);
                }

                __instance.orderedItemsFromTerminal = ammunitionOrder;
                __instance.numberOfItemsInDropship = ammunitionOrder.Count;

                resupplyOrdered = false;
            }

            shouldBeSent = false;
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB))]
    internal static class PlayerControllerPatch
    {
        internal static float currentHealProgress = 0f;

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void TZP_Treatment(PlayerControllerB __instance)
        {
            if (__instance.drunknessInertia > 0f)
            {
                if (__instance.drunknessInertia > 1f)
                {
                    __instance.drunknessInertia = 1f;
                }

                if (__instance.health >= 20f && __instance.criticallyInjured && __instance.bleedingHeavily)
                {
                    __instance.MakeCriticallyInjured(false);
                }
                __instance.drunkness = 0f;
                currentHealProgress += Time.deltaTime * 10f;
                if (currentHealProgress > 1f)
                {
                    __instance.health++;
                    GameObject.FindObjectOfType<HUDManager>().UpdateHealthUI(__instance.health, false);
                    currentHealProgress = 0f;
                }
            }
        }
    }

    [HarmonyPatch(typeof(HUDManager))]
    internal class ChatCommandsPatch
    {
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
                    timeOfDay.TimeOfDayMusic.PlayOneShot(DLGModMain.swarmSFX[0]);
                    return;
                case "THEY ARE HERE!!!":
                    timeOfDay.TimeOfDayMusic.clip = DLGModMain.swarmSFX[1];
                    timeOfDay.TimeOfDayMusic.Play();
                    timeOfDay.TimeOfDayMusic.loop = true;
                    return;
                case "SWARM IS ALMOST OVER":
                    timeOfDay.TimeOfDayMusic.loop = false;
                    SwarmPatch.isSwarmSFXFading = true;
                    return;
            }
        }
    }

    [HarmonyPatch(typeof(TimeOfDay))]
    internal class SwarmPatch
    {
        internal static bool hasBeenCalled = false;

        internal static float dangerLevel = 5f;

        internal static List<int> swarmEnemiesIndex = new List<int>();

        internal static float chance = 0;

        internal static bool isSwarm = false;
        internal static bool isSwarmSFXFading = false;

        internal static HUDManager hudManager = GameObject.FindObjectOfType<HUDManager>();

        [HarmonyPatch("SyncTimeClientRpc")]
        [HarmonyPostfix]
        public static void RollSwarmDice(TimeOfDay __instance)
        {
            if (!__instance.IsHost) return;

            if (hasBeenCalled)
            {
                hasBeenCalled = false;
                return;
            }

            RoundManager roundManager = GameObject.FindObjectOfType<RoundManager>();

            if (swarmEnemiesIndex.Count == 0)
            {
                swarmEnemiesIndex.Add(((roundManager.currentLevel.Enemies.FindIndex(enemy => enemy.enemyType.enemyName == "Crawler"))));
            }

            int rollDice = UnityEngine.Random.Range(30, 100);

            int enemiesAmount = 0;

            foreach (EnemyAI enemy in GameObject.FindObjectsOfType<EnemyAI>())
            {
                if (!enemy.isEnemyDead && enemy.enemyType.enemyName == "Crawler") enemiesAmount++;
            }

            if (rollDice < chance && enemiesAmount < 3)
            {
                for (int i = 0; i < (DLGModMain.playersAmount) * (5 + dangerLevel / 15); i++)
                {
                    int enemyToSpawn = swarmEnemiesIndex[0];
                    EnemyVent vent = roundManager.allEnemyVents[UnityEngine.Random.Range(1, roundManager.allEnemyVents.Length)];

                    roundManager.SpawnEnemyOnServer(vent.transform.position, vent.transform.eulerAngles.y, enemyToSpawn);
                }

                isSwarm = true;

                hudManager.AddTextToChatOnServer("SWARM!");

                chance = 0f;
            }
            else if (enemiesAmount >= 3)
            {
                chance += 0.01f * dangerLevel * __instance.normalizedTimeOfDay;
            }
            else
            {
                if (isSwarm)
                {
                    hudManager.AddTextToChatOnServer("SWARM IS ALMOST OVER");
                    isSwarm = false;
                }

                chance += dangerLevel * __instance.normalizedTimeOfDay * Random.Range(0.5f, 1f);
            }

            hasBeenCalled = true;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void ChangeSwarmSFX(TimeOfDay __instance)
        {
            if (isSwarm && !__instance.TimeOfDayMusic.isPlaying && !__instance.TimeOfDayMusic.loop)
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

    [HarmonyPatch(typeof(ItemDropship))]
    internal static class SpawnAmmunitionPatch
    {
        internal static Terminal terminal = GameObject.FindObjectOfType<Terminal>();

        internal static List<Item> allItemsList = Resources.FindObjectsOfTypeAll<AllItemsList>()[0].itemsList;

        [HarmonyPatch("OpenShipDoorsOnServer")]
        [HarmonyPrefix]
        public static void SetUpAmmunitionPacks(ItemDropship __instance)
        {
            List<int> itemsToDeliver = Traverse.Create(__instance).Field("itemsToDeliver").GetValue() as List<int>;

            if (__instance.shipLanded && !__instance.shipDoorsOpened)
            {
                int num = 0;

                for (int i = 0; i < itemsToDeliver.Count; i++)
                {
                    if (itemsToDeliver[i] == 14)
                    {
                        for (int j = 0; j < DLGModMain.playersAmount; j++)
                        {
                            GameObject obj = GameObject.Instantiate(allItemsList[59].spawnPrefab,
                                __instance.itemSpawnPositions[num].position, Quaternion.identity); // Shotgun
                            obj.GetComponent<GrabbableObject>().fallTime = 0f;
                            obj.GetComponent<NetworkObject>().Spawn();
                            num = ((num < 3) ? (num + 1) : 0);
                        }
                        for (int j = 0; j < DLGModMain.playersAmount * 8; j++)
                        {
                            GameObject obj = GameObject.Instantiate(allItemsList[60].spawnPrefab,
                                __instance.itemSpawnPositions[num].position, Quaternion.identity); // Shotgun ammo
                            obj.GetComponent<GrabbableObject>().fallTime = 0f;
                            obj.GetComponent<NetworkObject>().Spawn();
                            num = ((num < 3) ? (num + 1) : 0);
                        }

                        itemsToDeliver.Remove(14);
                        i--;
                    }
                    else if (itemsToDeliver[i] == 15)
                    {
                        for (int j = 0; j < DLGModMain.playersAmount * 5; j++)
                        {
                            GameObject obj = GameObject.Instantiate(allItemsList[60].spawnPrefab,
                                __instance.itemSpawnPositions[num].position, Quaternion.identity); // Shotgun ammo
                            obj.GetComponent<GrabbableObject>().fallTime = 0f;
                            obj.GetComponent<NetworkObject>().Spawn();
                            num = ((num < 3) ? (num + 1) : 0);
                        }
                        for (int j = 0; j < DLGModMain.playersAmount; j++)
                        {
                            GameObject obj = GameObject.Instantiate(allItemsList[13].spawnPrefab,
                                __instance.itemSpawnPositions[num].position, Quaternion.identity); // Health
                            obj.GetComponent<GrabbableObject>().fallTime = 0f;
                            obj.GetComponent<NetworkObject>().Spawn();
                            num = ((num < 3) ? (num + 1) : 0);
                        }

                        itemsToDeliver.Remove(15);
                        i--;
                    }
                }
            }

            Traverse.Create(__instance).Field("itemsToDeliver").SetValue(itemsToDeliver);
        }

        [HarmonyPatch("OpenShipClientRpc")]
        [HarmonyPostfix]
        public static void OpenDropshipDoors()
        {
            DLGTipsPatch.ammunitionRecieved = true;
        }
    }

    [HarmonyPatch(typeof(TimeOfDay))]
    internal static class DLGTipsPatch
    {
        internal static HUDManager hudManager = GameObject.FindObjectOfType<HUDManager>();

        internal static bool ammunitionRecieved = false;

        [HarmonyPatch("SyncTimeClientRpc")]
        [HarmonyPostfix]
        public static void CheckForTipMoment()
        {
            if (ammunitionRecieved)
            {
                List<GunAmmo> shotgunAmmo = GameObject.FindObjectsOfType<GunAmmo>().ToList();

                if (shotgunAmmo.Count <= 2)
                {
                    hudManager.DisplayTip("Out of ammo?",
                        "Don't worry! You can buy supply drop from terminal store",
                        false,
                        true, "DLG_ResupplyTip");
                }
            }
        }
    }
}