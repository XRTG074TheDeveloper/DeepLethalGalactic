using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLGMod.Patches
{
    [HarmonyPatch(typeof(TimeOfDay))]
    internal class SwarmPatch
    {
        internal static bool hasStarted = false;
        internal static bool hasBeenCalled = false;

        internal static float dangerLevel = 10;
        internal static int hazardLevel = 2;
        internal static float maxEnemiesAtTime;

        internal static GameObject[] outsideAINodes;

        internal static List<int> swarmEnemiesIndex = new List<int>();

        internal static float chance = 0;
        internal static int enemiesToSpawn = 0;
        internal static int outsideEnemiesToSpawn = 0;

        internal static bool isSwarm = false;
        internal static bool isSwarmSFXFading = false;

        internal static HUDManager hudManager = GameObject.FindObjectOfType<HUDManager>();

        internal static GameObject swarmAllocation;

        public static void SetUpSwarmStuff(PlayerControllerB[] allPlayersScripts)
        {
            if (hasStarted) return;

            RoundManager roundManager = GameObject.FindObjectOfType<RoundManager>();

            for (int i = 0; i < roundManager.currentLevel.Enemies.Count; i++)
            {
                if (roundManager.currentLevel.Enemies[i].enemyType.enemyName.Contains("DLG"))
                {
                    roundManager.currentLevel.Enemies.RemoveAt(i);
                }
            }
            if (roundManager.currentLevel.Enemies.FindIndex(enemy => enemy.enemyType.enemyName == "Crawler") != -1)
            {
                CreateNewSwarmEnemy("DLG[isOutside?][isStrong?]SwarmCrawler", "Crawler", roundManager, new CrawlerAI());
            }

            maxEnemiesAtTime = hazardLevel * Mathf.CeilToInt(dangerLevel / 10) * (0.75f + DLGModMain.playersAmount / allPlayersScripts.Length);

            swarmAllocation = new GameObject();
            swarmAllocation.AddComponent<SwarmAllocation>();

            swarmAllocation.GetComponent<SwarmAllocation>().maxEnemiesPerPlayer = Mathf.CeilToInt(maxEnemiesAtTime / DLGModMain.playersAmount);

            swarmAllocation.GetComponent<SwarmAllocation>().players = allPlayersScripts;

            swarmAllocation.GetComponent<SwarmAllocation>().enemiesTargeting = new int[allPlayersScripts.Length];

            hasStarted = true;

        }

        private static void CreateNewSwarmEnemy<T>(string swarmEnemyName, string originalEnemyName, RoundManager roundManager, T enemyAIType) 
            where T : EnemyAI
        {
            for (int i = 0; i < 4; i++)
            {
                string currentSwarmEnemyName = swarmEnemyName;

                switch (i)
                {
                    case 0:
                        currentSwarmEnemyName = currentSwarmEnemyName.Replace("[isStrong?]", "");
                        currentSwarmEnemyName = currentSwarmEnemyName.Replace("[isOutside?]", "");
                        break;
                    case 1:
                        currentSwarmEnemyName = currentSwarmEnemyName.Replace("[isStrong?]", "");
                        currentSwarmEnemyName = currentSwarmEnemyName.Replace("[isOutside?]", "Outside");
                        break;
                    case 2:
                        currentSwarmEnemyName = currentSwarmEnemyName.Replace("[isStrong?]", "Strong");
                        currentSwarmEnemyName = currentSwarmEnemyName.Replace("[isOutside?]", "");
                        break;
                    case 3:
                        currentSwarmEnemyName = currentSwarmEnemyName.Replace("[isStrong?]", "Strong");
                        currentSwarmEnemyName = currentSwarmEnemyName.Replace("[isOutside?]", "Outside");
                        break;
                }

                SpawnableEnemyWithRarity swarmEnemy = new SpawnableEnemyWithRarity();
                swarmEnemy.enemyType = roundManager.currentLevel.Enemies.Find(enemy => enemy.enemyType.enemyName == originalEnemyName).enemyType;
                swarmEnemy.rarity = 0;

                GameObject enemyPrefab = GameObject.Instantiate(swarmEnemy.enemyType.enemyPrefab);

                swarmEnemy.enemyType = ScriptableObject.CreateInstance<EnemyType>();

                swarmEnemy.enemyType.enemyPrefab = enemyPrefab;
                swarmEnemy.enemyType.enemyPrefab.name = currentSwarmEnemyName;
                swarmEnemy.enemyType.name = currentSwarmEnemyName;
                swarmEnemy.enemyType.enemyName = currentSwarmEnemyName;
                if (i % 2 != 0)
                {
                    swarmEnemy.enemyType.isOutsideEnemy = true;
                }

                DLGModMain.logger.LogInfo(currentSwarmEnemyName);

                enemyPrefab.GetComponent<T>().enemyType = swarmEnemy.enemyType;
                enemyPrefab.AddComponent<DLGEnemyAI>();

                roundManager.currentLevel.Enemies.Add(swarmEnemy);
            }
        }

        [HarmonyPatch("SyncTimeClientRpc")]
        [HarmonyPostfix]
        private static void RollSwarmDice(TimeOfDay __instance)
        {
            if (!__instance.IsHost) return;

            if (hasBeenCalled)
            {
                hasBeenCalled = false;
                return;
            }

            if (outsideAINodes == null)
            {
                outsideAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
            }

            RoundManager roundManager = GameObject.FindObjectOfType<RoundManager>();

            if (swarmEnemiesIndex.Count == 0)
            {
                if (roundManager.currentLevel.Enemies.FindIndex(enemy => enemy.enemyType.enemyName == "Crawler") != -1)
                {
                    swarmEnemiesIndex.Add(((roundManager.currentLevel.Enemies.FindIndex(enemy => enemy.enemyType.enemyName == "DLGSwarmCrawler"))));
                    swarmEnemiesIndex.Add(((roundManager.currentLevel.Enemies.FindIndex(enemy => enemy.enemyType.enemyName == "DLGOutsideSwarmCrawler"))));
                    swarmEnemiesIndex.Add(((roundManager.currentLevel.Enemies.FindIndex(enemy => enemy.enemyType.enemyName == "DLGStrongSwarmCrawler"))));
                    swarmEnemiesIndex.Add(((roundManager.currentLevel.Enemies.FindIndex(enemy => enemy.enemyType.enemyName == "DLGOutsideStrongSwarmCrawler"))));
                }
            }

            int rollDice = UnityEngine.Random.Range(30, 100);

            DLGModMain.logger.LogInfo($"Current swarm chance: {chance}\n" +
                $"Randomly chosen swarm value: {rollDice}");

            int enemiesAmount = 0;
            int outsideEnemiesAmount = 0;

            foreach (DLGEnemyAI enemy in GameObject.FindObjectsOfType<DLGEnemyAI>())
            {
                if (!enemy.enemyAI.isEnemyDead && enemy.name.ToString().Contains("Swarm") &&
                    !enemy.name.ToString().Contains("Outside")) enemiesAmount++;
            }
            foreach (DLGEnemyAI enemy in GameObject.FindObjectsOfType<DLGEnemyAI>())
            {
                if (!enemy.enemyAI.isEnemyDead && enemy.name.ToString().Contains("Swarm") &&
                    enemy.name.ToString().Contains("Outside")) outsideEnemiesAmount++;
            }

            if (rollDice < chance && enemiesToSpawn == 0)
            {
                StartOfRound startOfRound = GameObject.FindObjectOfType<StartOfRound>();

                enemiesToSpawn = Mathf.CeilToInt((0.75f + DLGModMain.playersAmount / startOfRound.allPlayerScripts.Length)
                    * hazardLevel * Mathf.CeilToInt(dangerLevel / 10));
                outsideEnemiesToSpawn = Mathf.CeilToInt((0.75f + DLGModMain.playersAmount / startOfRound.allPlayerScripts.Length)
                    * hazardLevel * Mathf.CeilToInt(dangerLevel / 10));

                SpawnSwarmEnemies(roundManager, enemiesAmount, outsideEnemiesAmount);

                isSwarm = true;

                hudManager.AddTextToChatOnServer("SWARM!");

                chance = 0f;
            }
            else if (enemiesToSpawn != 0)
            {
                if (isSwarm && (enemiesAmount < maxEnemiesAtTime || outsideEnemiesAmount < maxEnemiesAtTime))
                {
                    SpawnSwarmEnemies(roundManager, enemiesAmount, outsideEnemiesAmount);
                }
                chance += 0.01f * dangerLevel * __instance.normalizedTimeOfDay * hazardLevel;
            }
            else
            {
                if (isSwarm)
                {
                    hudManager.AddTextToChatOnServer("SWARM IS ALMOST OVER");
                    isSwarm = false;
                }

                chance += dangerLevel * __instance.normalizedTimeOfDay * UnityEngine.Random.Range(0.5f, 1f) * hazardLevel;
            }

            hasBeenCalled = true;
        }

        private static void SpawnSwarmEnemies(RoundManager roundManager, int currentEnemiesAmount, int currentOutsideEnemiesAmount)
        {
            //DLGModMain.logger.LogInfo($"{currentEnemiesAmount}\n{currentOutsideEnemiesAmount}\n{maxEnemiesAtTime}\n{enemiesToSpawn}\n{outsideEnemiesToSpawn}");

            for (int i = currentEnemiesAmount; i < maxEnemiesAtTime; i++)
            {
                int enemyToSpawn = UnityEngine.Random.Range(0, swarmEnemiesIndex.Count / 4);

                EnemyVent vent = roundManager.allEnemyVents[UnityEngine.Random.Range(1, roundManager.allEnemyVents.Length)];

                float randomNum = UnityEngine.Random.Range(0, 1f);

                if (randomNum > 0.8f)
                {
                    enemyToSpawn = swarmEnemiesIndex[2 + enemyToSpawn * 4];
                }
                else
                {
                    enemyToSpawn = 0 + swarmEnemiesIndex[0 + enemyToSpawn * 4];
                }

                roundManager.SpawnEnemyOnServer(vent.transform.position, vent.transform.eulerAngles.y, enemyToSpawn);

                enemiesToSpawn--;

                if (enemiesToSpawn == 0)
                {
                    break;
                }
            }

            for (int i = currentOutsideEnemiesAmount; i < maxEnemiesAtTime; i++)
            {
                int enemyToSpawn = UnityEngine.Random.Range(0, swarmEnemiesIndex.Count / 4);

                Transform spawnPosition = outsideAINodes[UnityEngine.Random.Range(1, outsideAINodes.Length)].transform;

                float randomNum = UnityEngine.Random.Range(0, 1f);

                if (randomNum > 0.8f)
                {
                    enemyToSpawn = swarmEnemiesIndex[3 + enemyToSpawn * 4];
                }
                else
                {
                    enemyToSpawn = swarmEnemiesIndex[1 + enemyToSpawn * 4];
                }

                roundManager.SpawnEnemyOnServer(spawnPosition.position, 0, enemyToSpawn);

                outsideEnemiesToSpawn--;

                if (outsideEnemiesToSpawn == 0)
                {
                    break;
                }
            }
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
}
