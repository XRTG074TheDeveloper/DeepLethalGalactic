using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace DLGMod.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class GameControllerPatch
    {
        internal static bool hasStartedHost = false;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StarterSetUp(StartOfRound __instance)
        {
            hasStartedHost = false;

            SwarmPatch.isSwarm = false;
            SwarmPatch.isSwarmSFXFading = false;
            SwarmPatch.chance = 0;
            SwarmPatch.dangerLevel = 5f;
            SwarmPatch.enemiesToSpawn = 0;

            SwarmPatch.hasStarted = false;

            GameObject.FindObjectOfType<TimeOfDay>().TimeOfDayMusic.Stop();

            GameObject.FindObjectOfType<Terminal>().terminalNodes.specialNodes[13].displayText =
                ">MOONS\r\n" +
                "To see the list of moons the autopilot can route to.\r\n\r\n" +
                ">DLGMISSION\r\n" +
                "To open DLG Mission Controller Hub, where you can changed properties of your mission.\r\n\r\n" +
                ">STORE\r\n" +
                "To see the company store's selection of useful items.\r\n\r\n" +
                ">BESTIARY\r\n" +
                "To see the list of wildlife on record.\r\n\r\n" +
                ">STORAGE\r\n" +
                "To access objects placed into storage.\r\n\r\n" +
                ">OTHER\r\n" +
                "To see the list of other commands\r\n\r\n" +
                "[numberOfItemsOnRoute]\r\n";

            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex].result.displayText =
            "You are not able to order ammunition pack unless you are on the mission!\n\n";
            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex].result.terminalOptions = null;
            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                    .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex].result.overrideOptions = false;

            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex + 1].result.displayText =
            "You are not able to order supply drop unless you are on the mission!\n\n";
            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex + 1].result.terminalOptions = null;
            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                    .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex + 1].result.overrideOptions = false;

            MissionControllerPatch.DLGMissionHub.displayText =
            "Welcome to DLG Mission Controller Hub! Here you can view and change your mission properties such as " +
            "mission hazard (difficulty) level\n\n" +
            "Current mission properties:\n" +
            $"Hazard level - {SwarmPatch.hazardLevel}: {MissionControllerPatch.hazardTitles[SwarmPatch.hazardLevel - 1]}\n\n" +
            ">HAZARD (1-5)\n" +
            "To change mission hazard (difficulty) level. Type this command with integer within the range (1-5)\n\n";
            MissionControllerPatch.isOnTheMission = false;

            SwarmPatch.swarmEnemiesIndex.Clear();

            SwarmPatch.hudManager = GameObject.FindObjectOfType<HUDManager>();

            DLGModMain.logger.LogInfo("Initializing game:\n" +
                $"\tMission properties are unlocked\n" +
                $"\tAmmunition Pack and Ressuply Drop are unbuyable now");
        }

        [HarmonyPatch("OnPlayerConnectedClientRpc")]
        [HarmonyPostfix]
        public static void OnClientConnected(StartOfRound __instance)
        {
            if (__instance.IsHost)
            {
                DLGModMain.logger.LogInfo("New client connected. Sending DLG NetStuff sync request...");

                GameObject.FindObjectOfType<HUDManager>().AddTextToChatOnServer($"dlgnetsync_missionhazard_{SwarmPatch.hazardLevel}");
            }
        }

        [HarmonyPatch("StartGame")]
        [HarmonyPostfix]
        public static void OnGameStarted(StartOfRound __instance)
        {
            if (__instance.currentLevelID != 3)
            {
                if (!hasStartedHost)
                {
                    hasStartedHost = true;
                    if (__instance.IsHost)
                    {
                        GameObject.FindObjectOfType<HUDManager>().AddTextToChatOnServer("dlgnetsync_onmission");
                    }
                }
                else
                {
                    return;
                }

                DLGModMain.SendAmmunition(__instance.connectedPlayersAmount + 1);

                GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                    .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex].result.displayText =
                "You have requested to order ammunition pack. Amount: [variableAmount].\n" +
                "Total cost of items: [totalCost].\n\n" +
                "Please CONFIRM or DENY\n";
                GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                    .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex].result.terminalOptions = AmmunitionPatch.ammunitionPackTO;
                GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                    .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex].result.overrideOptions = true;

                GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                    .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex + 1].result.displayText =
                "You have requested to order resupply drop. Amount: [variableAmount].\n" +
                "Total cost of items: [totalCost].\n\n" +
                "Please CONFIRM or DENY\n";
                GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                    .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex + 1].result.terminalOptions = AmmunitionPatch.supplyDropTO;
                GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                    .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex + 1].result.overrideOptions = true;

                MissionControllerPatch.DLGMissionHub.displayText =
                "Welcome to DLG Mission Controller Hub! Here you can view and change your mission properties such as " +
                "mission hazard (difficulty) level\n\n" +
                "Current mission properties:\n" +
                $"Hazard level - {SwarmPatch.hazardLevel}: {MissionControllerPatch.hazardTitles[SwarmPatch.hazardLevel - 1]}\n\n" +
                "You are not able change your mission settings on the mission!\n\n";
                MissionControllerPatch.isOnTheMission = true;

                SwarmPatch.dangerLevel = 1f;

                foreach (char ch in __instance.currentLevel.riskLevel)
                {
                    switch (ch)
                    {
                        case 'A':
                            SwarmPatch.dangerLevel += 7f;
                            break;
                        case 'S':
                            SwarmPatch.dangerLevel += 15f;
                            break;
                        case '+':
                            SwarmPatch.dangerLevel += 5f;
                            break;
                    }
                }

                SwarmPatch.SetUpSwarmStuff(__instance.allPlayerScripts);

                RoundManager roundManager = GameObject.FindObjectOfType<RoundManager>();

                roundManager.currentLevel.Enemies[roundManager.currentLevel.Enemies.FindIndex(enemy => enemy.enemyType.enemyName == "Hoarding bug")]
                    .enemyType.enemyPrefab.AddComponent<DLGEnemyAI>();
                roundManager.currentLevel.Enemies[roundManager.currentLevel.Enemies.FindIndex(enemy => enemy.enemyType.enemyName == "Hoarding bug")]
                   .enemyType.enemyPrefab.AddComponent<Light>().color = Color.yellow;
                roundManager.currentLevel.Enemies[roundManager.currentLevel.Enemies.FindIndex(enemy => enemy.enemyType.enemyName == "Hoarding bug")]
                   .enemyType.enemyPrefab.GetComponent<Light>().intensity = 15;
                roundManager.currentLevel.Enemies[roundManager.currentLevel.Enemies.FindIndex(enemy => enemy.enemyType.enemyName == "Hoarding bug")]
                   .enemyType.enemyPrefab.GetComponent<Light>().range = 15;

                DLGModMain.logger.LogInfo("Starting game:\n" +
                $"\tMoon danger level: {SwarmPatch.dangerLevel}\n" +
                $"\tMission hazard level: {SwarmPatch.hazardLevel}\n\n" +
                $"\tMission properties are locked\n" +
                $"\tAmmunition Pack and Ressuply Drop are buyable now");
            }
        }

        [HarmonyPatch("ShipLeave")]
        [HarmonyPostfix]
        public static void OnShipStartLeaving()
        {
            hasStartedHost = false;

            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex].result.displayText =
            "You are not able to order ammunition pack unless you are on the mission!\n\n";
            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex].result.terminalOptions = null;
            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                    .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex].result.overrideOptions = false;

            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex + 1].result.displayText =
            "You are not able to order supply drop unless you are on the mission!\n\n";
            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex + 1].result.terminalOptions = null;
            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[0]
                    .compatibleNouns[AmmunitionPatch.ammunitionCompatibleNodeIndex + 1].result.overrideOptions = false;

            MissionControllerPatch.DLGMissionHub.displayText =
            "Welcome to DLG Mission Controller Hub! Here you can view and change your mission properties such as " +
            "mission hazard (difficulty) level\n\n" +
            "Current mission properties:\n" +
            $"Hazard level - {SwarmPatch.hazardLevel}: {MissionControllerPatch.hazardTitles[SwarmPatch.hazardLevel - 1]}\n\n" +
            ">HAZARD (1-5)\n" +
            "To change mission hazard (difficulty) level. Type this command with integer within the range (1-5)\n\n";
            MissionControllerPatch.isOnTheMission = false;

            SwarmPatch.isSwarm = false;
            SwarmPatch.isSwarmSFXFading = false;
            SwarmPatch.chance = 0;
            SwarmPatch.dangerLevel = 5f;
            SwarmPatch.enemiesToSpawn = 0;

            SwarmPatch.hasStarted = false;

            DLGTipsPatch.ammunitionRecieved = false;

            ChatCommandsPatch.PerformSwarmAction("finishSwarm");

            DLGModMain.logger.LogInfo("Finishing game:\n" +
                $"\tMission properties are unlocked\n" +
                $"\tAmmunition Pack and Ressuply Drop are unbuyable now");
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

        internal static int ammunitionNodeIndex;
        internal static int ammunitionCompatibleNodeIndex;
        internal static int ammunitionItemIndex;

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        public static void SetUpAmmunition(Terminal __instance)
        {
            List<Item> tempBuyableItems = __instance.buyableItemsList.ToList();

            if (ammunitionNodeIndex == 0)
            {
                ammunitionItemIndex = tempBuyableItems.Count;
            }

            tempBuyableItems.Add(new Item() { itemName = "Ammunition Pack", creditsWorth = 100 }); // Ammunition pack
            tempBuyableItems.Add(new Item() { itemName = "Supply Drop", creditsWorth = 20 }); // Resupply drop

            __instance.buyableItemsList = tempBuyableItems.ToArray();

            if (__instance.terminalNodes.allKeywords[ammunitionNodeIndex].name != "Ammunition Pack")
            {
                List<TerminalKeyword> tempTerminalNodes = __instance.terminalNodes.allKeywords.ToList();

                if (ammunitionNodeIndex == 0)
                {
                    ammunitionNodeIndex = tempTerminalNodes.Count;
                }

                tempTerminalNodes.Add(new TerminalKeyword() { word = "ammunition pack", defaultVerb = tempTerminalNodes[0], name = "Ammunition Pack" });
                tempTerminalNodes.Add(new TerminalKeyword() { word = "supply drop", defaultVerb = tempTerminalNodes[0], name = "Supply Drop" });

                __instance.terminalNodes.allKeywords = tempTerminalNodes.ToArray();

                List<CompatibleNoun> tempCompatibleNouns = tempTerminalNodes[0].compatibleNouns.ToList();

                if (ammunitionCompatibleNodeIndex == 0)
                {
                    ammunitionCompatibleNodeIndex = tempCompatibleNouns.Count;
                }

                TerminalNode ammuntionPackNode = new TerminalNode()
                {
                    buyItemIndex = ammunitionItemIndex,
                    isConfirmationNode = false,
                    clearPreviousText = true,
                    terminalEvent = "",
                    displayText = "Ordered [variableAmount] ammunition packs. " +
                    "Your new balance is [playerCredits].\r\n\r\n" +
                    "Our contractors enjoy fast, free shipping while on the job! " +
                    "Any purchased items will arrive hourly at your approximate location.\r\n\r\n",
                    name = "buyAmmunitionPack2",
                    overrideOptions = false
                };
                TerminalNode supplyDropNode = new TerminalNode()
                {
                    buyItemIndex = ammunitionItemIndex + 1,
                    isConfirmationNode = false,
                    clearPreviousText = true,
                    terminalEvent = "",
                    displayText = "Ordered [variableAmount] supply drops. " +
                    "Your new balance is [playerCredits].\r\n\r\n" +
                    "Our contractors enjoy fast, free shipping while on the job! " +
                    "Any purchased items will arrive hourly at your approximate location.\r\n\r\n",
                    name = "buyResupplyDrop2",
                    overrideOptions = false
                };

                ammunitionPackTO = new CompatibleNoun[]
                {
                new CompatibleNoun() {noun = tempTerminalNodes[3], result = ammuntionPackNode},
                new CompatibleNoun() {noun = tempTerminalNodes[4], result = tempTerminalNodes[0].compatibleNouns[2].result.terminalOptions[1].result}
                };
                supplyDropTO = new CompatibleNoun[]
                {
                new CompatibleNoun() {noun = tempTerminalNodes[3], result = supplyDropNode},
                new CompatibleNoun() {noun = tempTerminalNodes[4], result = tempTerminalNodes[0].compatibleNouns[2].result.terminalOptions[1].result}
                };

                tempCompatibleNouns.Add(new CompatibleNoun()
                {
                    noun = tempTerminalNodes[ammunitionNodeIndex],
                    result = new TerminalNode()
                    {
                        buyItemIndex = ammunitionItemIndex,
                        isConfirmationNode = true,
                        clearPreviousText = true,
                        itemCost = 100,
                        displayText =
                        "You are not able to order ammunition pack unless you are on the mission!\n\n",
                        name = "buyAmmunitionPack1",
                        terminalOptions = null,
                        terminalEvent = "",
                        overrideOptions = false
                    }
                });
                tempCompatibleNouns.Add(new CompatibleNoun()
                {
                    noun = tempTerminalNodes[ammunitionNodeIndex + 1],
                    result = new TerminalNode()
                    {
                        buyItemIndex = ammunitionItemIndex + 1,
                        isConfirmationNode = true,
                        clearPreviousText = true,
                        itemCost = 20,
                        displayText =
                        "You are not able to order supply drop unless you are on the mission!\n\n",
                        name = "buyResupplyDrop1",
                        terminalOptions = null,
                        terminalEvent = "",
                        overrideOptions = false
                    }
                });

                __instance.terminalNodes.allKeywords[0].compatibleNouns = tempCompatibleNouns.ToArray();
            }
        }

        [HarmonyPatch("InitializeItemSalesPercentages")]
        [HarmonyPostfix]
        public static void SetUpAmmunition2(Terminal __instance)
        {
            List<int> tempItemSalesPercentages = __instance.itemSalesPercentages.ToList();

            DLGModMain.logger.LogInfo(tempItemSalesPercentages.Count);

            tempItemSalesPercentages.Add(100);
            tempItemSalesPercentages.Add(100);

            __instance.itemSalesPercentages = tempItemSalesPercentages.ToArray();
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
                ammunitionOrder.Add(ammunitionItemIndex);
                __instance.orderedItemsFromTerminal = ammunitionOrder;
                __instance.numberOfItemsInDropship = ammunitionOrder.Count;

                DLGModMain.logger.LogInfo("Ordered ammunition pack");
            }

            shouldBeSent = false;
        }
    }

    [HarmonyPatch(typeof(Terminal))]
    internal class MissionControllerPatch
    {
        internal static bool isDLGMissionHubOpened = false;
        internal static bool isOnTheMission = false;

        internal static TerminalNode DLGMissionHub = new TerminalNode()
        {
            clearPreviousText = true,
            terminalEvent = ""
        };

        internal static string[] hazardTitles = new string[]
        {
            "EASY",
            "NORMAL",
            "RISKY",
            "REALLY HARD",
            "LETHAL"
        };

        public static void UpdateMissionValues()
        {
            if (!isOnTheMission)
            {
                MissionControllerPatch.DLGMissionHub.displayText =
                "Welcome to DLG Mission Controller Hub! Here you can view and change your mission properties such as " +
                "mission hazard (difficulty) level\n\n" +
                "Current mission properties:\n" +
                $"Hazard level - {SwarmPatch.hazardLevel}: {MissionControllerPatch.hazardTitles[SwarmPatch.hazardLevel - 1]}\n\n" +
                ">HAZARD (1-5)\n" +
                "To change mission hazard (difficulty) level. Type this command with integer within the range (1-5)\n\n";
            }
            else
            {
                MissionControllerPatch.DLGMissionHub.displayText =
                "Welcome to DLG Mission Controller Hub! Here you can view and change your mission properties such as " +
                "mission hazard (difficulty) level\n\n" +
                "Current mission properties:\n" +
                $"Hazard level - {SwarmPatch.hazardLevel}: {MissionControllerPatch.hazardTitles[SwarmPatch.hazardLevel - 1]}\n\n" +
                "You are not able change your mission settings on the mission!\n\n";
            }
        }

        [HarmonyPatch("ParsePlayerSentence")]
        [HarmonyPostfix]
        public static void MissionControllerHub(ref TerminalNode __result, Terminal __instance)
        {
            string playerText = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);

            StringBuilder stringBuilder = new StringBuilder();

            foreach (char ch in playerText)
            {
                if (!char.IsPunctuation(ch))
                {
                    stringBuilder.Append(ch);
                }
            }

            playerText = stringBuilder.ToString().ToLower();

            if (playerText.Contains("dlg"))
            {
                for (int num = playerText.Length; num > 3; num--)
                {
                    if ("dlgmission".StartsWith(playerText.Substring(0, num)))
                    {
                        isDLGMissionHubOpened = true;
                        UpdateMissionValues();
                        __result = DLGMissionHub;
                        break;
                    }
                }
            }
            else if (isDLGMissionHubOpened && !isOnTheMission)
            {
                string[] arguments = playerText.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

                for (int num = arguments[0].Length; num > 2; num--)
                {
                    if ("hazard".StartsWith(arguments[0].Substring(0, num)))
                    {
                        if (arguments.Length > 1)
                        {
                            try
                            {
                                int newHazardLevel = int.Parse(arguments[1]);

                                if (newHazardLevel > 0 && newHazardLevel < 6)
                                {
                                    GameObject.FindObjectOfType<HUDManager>().AddTextToChatOnServer($"dlgnetsync_missionhazard_{newHazardLevel}");

                                    __result = new TerminalNode()
                                    {
                                        displayText = $"Successfully changed mission hazard level to\n" +
                                        $"{newHazardLevel}: {hazardTitles[newHazardLevel - 1]}\n\n",
                                        clearPreviousText = true
                                    };
                                }
                                else
                                {
                                    __result = new TerminalNode()
                                    {
                                        displayText = $"Operation failed because the integer you typed was out of bounds\n\n",
                                        clearPreviousText = true,
                                        terminalEvent = ""
                                    };
                                }
                            }
                            catch
                            {
                                __result = new TerminalNode()
                                {
                                    displayText = $"Operation failed because the text you typed wasn't an integer\n\n",
                                    clearPreviousText = true,
                                    terminalEvent = ""
                                };
                            }

                        }
                        else
                        {
                            __result = new TerminalNode()
                            {
                                displayText = $"Operation failed because you didn't type hazard level integer\n\n",
                                clearPreviousText = true,
                                terminalEvent = ""
                            };
                        }
                    }
                }

                isDLGMissionHubOpened = false;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerPatch
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

                if (currentHealProgress > 1f && __instance.health < 100f)
                {
                    DLGModMain.logger.LogInfo("Healed 1HP from TZP-MedKit");

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
        public static void PerformSwarmAction(string action)
        {
            TimeOfDay timeOfDay = GameObject.FindObjectOfType<TimeOfDay>();

            switch (action)
            {
                case "startSwarm":
                    timeOfDay.TimeOfDayMusic.volume = 1f;
                    timeOfDay.TimeOfDayMusic.PlayOneShot(DLGModMain.swarmSFX[1]);
                    return;
                case "loopSwarmMusic":
                    timeOfDay.TimeOfDayMusic.clip = DLGModMain.swarmSFX[0];
                    timeOfDay.TimeOfDayMusic.Play();
                    timeOfDay.TimeOfDayMusic.loop = true;
                    return;
                case "finishSwarm":
                    timeOfDay.TimeOfDayMusic.loop = false;
                    SwarmPatch.isSwarmSFXFading = true;
                    return;
            }
        }
    }

    [HarmonyPatch(typeof(HUDManager))]
    internal class DLGNetStuffSync
    {
        [HarmonyPatch("AddChatMessage")]
        [HarmonyPrefix]
        private static bool RecieveSyncRequest(ref string chatMessage, ref string nameOfUserWhoTyped)
        {
            DLGModMain.logger.LogInfo(chatMessage);

            if (nameOfUserWhoTyped == "" && chatMessage.ToLower().Contains("dlgnetsync"))
            {
                DLGModMain.logger.LogInfo("Recieved DLG NetStuff sync request. Parsing request arguments...");

                string[] requestArguments = chatMessage.Split('_');

                switch (requestArguments[1])
                {
                    case "onmission":
                        DLGModMain.logger.LogInfo("Set Client on the mission!");
                        GameControllerPatch.OnGameStarted(GameObject.FindObjectOfType<StartOfRound>());

                        break;
                    case "missionhazard":
                        int newHazardLevel = int.Parse(requestArguments[2]);

                        SwarmPatch.hazardLevel = newHazardLevel;

                        DLGModMain.logger.LogInfo($"Set client hazard level to {newHazardLevel} - {MissionControllerPatch.hazardTitles[newHazardLevel - 1]}");
                        break;
                    case "swarm":
                        if (requestArguments[2] == "start")
                        {
                            ChatCommandsPatch.PerformSwarmAction("startSwarm");

                            DLGModMain.logger.LogInfo($"Started swarm on Client!");
                        }
                        else if (requestArguments[2] == "finish")
                        {
                            ChatCommandsPatch.PerformSwarmAction("finishSwarm");

                            DLGModMain.logger.LogInfo($"Finished swarm on Client!");
                        }
                        break;
                    default:
                        DLGModMain.logger.LogError($"Invalid DLG NetStuff sync request: {chatMessage}");
                        break;
                }

                return false;
            }
            else
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(ItemDropship))]
    internal class SpawnAmmunitionPatch
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
                    if (itemsToDeliver[i] == AmmunitionPatch.ammunitionItemIndex)
                    {
                        int shotgunIndex = allItemsList.FindIndex(item => item.name == "Shotgun");
                        int shotgunAmmmoIndex = allItemsList.FindIndex(item => item.name == "GunAmmo");

                        DLGModMain.logger.LogInfo("Dropship opened. Spawning:");

                        for (int j = 0; j < 1; j++)
                        {
                            DLGModMain.logger.LogInfo("\tShotgun");

                            GameObject obj = GameObject.Instantiate(allItemsList[shotgunIndex].spawnPrefab,
                                __instance.itemSpawnPositions[num].position, Quaternion.identity); // Shotgun
                            obj.GetComponent<GrabbableObject>().fallTime = 0f;
                            obj.GetComponent<NetworkObject>().Spawn();
                            num = ((num < 3) ? (num + 1) : 0);
                        }
                        for (int j = 0; j < 12; j++)
                        {
                            DLGModMain.logger.LogInfo("\tShotgun Ammo");

                            GameObject obj = GameObject.Instantiate(allItemsList[shotgunAmmmoIndex].spawnPrefab,
                                __instance.itemSpawnPositions[num].position, Quaternion.identity); // Shotgun ammo
                            obj.GetComponent<GrabbableObject>().fallTime = 0f;
                            obj.GetComponent<NetworkObject>().Spawn();
                            num = ((num < 3) ? (num + 1) : 0);
                        }

                        itemsToDeliver.Remove(AmmunitionPatch.ammunitionItemIndex);
                        i--;
                    }
                    else if (itemsToDeliver[i] == AmmunitionPatch.ammunitionItemIndex + 1)
                    {
                        int shotgunAmmmoIndex = allItemsList.FindIndex(item => item.name == "GunAmmo");
                        int tzpMedkitIndex = allItemsList.FindIndex(item => item.name == "TZPInhalant");

                        DLGModMain.logger.LogInfo("Dropship opened. Spawning:");

                        for (int j = 0; j < DLGModMain.playersAmount * 7; j++)
                        {
                            DLGModMain.logger.LogInfo("\tShotgun Ammo");

                            GameObject obj = GameObject.Instantiate(allItemsList[shotgunAmmmoIndex].spawnPrefab,
                                __instance.itemSpawnPositions[num].position, Quaternion.identity); // Shotgun ammo
                            obj.GetComponent<GrabbableObject>().fallTime = 0f;
                            obj.GetComponent<NetworkObject>().Spawn();
                            num = ((num < 3) ? (num + 1) : 0);
                        }
                        for (int j = 0; j < DLGModMain.playersAmount; j++)
                        {
                            DLGModMain.logger.LogInfo("\tTZP-MedKit");

                            GameObject obj = GameObject.Instantiate(allItemsList[tzpMedkitIndex].spawnPrefab,
                                __instance.itemSpawnPositions[num].position, Quaternion.identity); // Health
                            obj.GetComponent<GrabbableObject>().fallTime = 0f;
                            obj.GetComponent<NetworkObject>().Spawn();
                            num = ((num < 3) ? (num + 1) : 0);
                        }

                        itemsToDeliver.Remove(AmmunitionPatch.ammunitionItemIndex + 1);
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
    internal class DLGTipsPatch
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
                    DLGModMain.logger.LogInfo("Try displaying Out of ammo Hint");

                    hudManager.DisplayTip("Out of ammo?",
                        "Don't worry! You can buy supply drop from the terminal store",
                        false,
                        true, "DLG_ResupplyTip");
                }
            }
        }
    }
}