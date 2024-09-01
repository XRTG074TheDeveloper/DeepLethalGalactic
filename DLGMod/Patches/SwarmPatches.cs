using GameNetcodeStuff;
using HarmonyLib;
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

        internal static List<int> swarmEnemiesIndex = new List<int>();

        internal static float chance = 0;
        internal static int enemiesToSpawn = 0;

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
            SpawnableEnemyWithRarity swarmCrawlerEnemy = new SpawnableEnemyWithRarity();
            swarmCrawlerEnemy.enemyType = roundManager.currentLevel.Enemies.Find(enemy => enemy.enemyType.enemyName == "Crawler").enemyType;
            swarmCrawlerEnemy.rarity = 0;

            GameObject dlgCrawler = GameObject.Instantiate(swarmCrawlerEnemy.enemyType.enemyPrefab);
            dlgCrawler.AddComponent<DLGEnemyAI>();

            swarmCrawlerEnemy.enemyType = ScriptableObject.CreateInstance<EnemyType>();

            swarmCrawlerEnemy.enemyType.enemyPrefab = dlgCrawler;
            swarmCrawlerEnemy.enemyType.enemyPrefab.name = "DLGSwarmCrawler";
            swarmCrawlerEnemy.enemyType.name = "DLGSwarmCrawler";
            swarmCrawlerEnemy.enemyType.enemyName = "DLGSwarmCrawler";

            roundManager.currentLevel.Enemies.Add(swarmCrawlerEnemy);

            SpawnableEnemyWithRarity swarmCrawlerEnemy2 = new SpawnableEnemyWithRarity();
            swarmCrawlerEnemy2.enemyType = roundManager.currentLevel.Enemies.Find(enemy => enemy.enemyType.enemyName == "Crawler").enemyType;
            swarmCrawlerEnemy2.rarity = 0;

            GameObject dlgCrawler2 = GameObject.Instantiate(swarmCrawlerEnemy2.enemyType.enemyPrefab);
            dlgCrawler2.AddComponent<DLGEnemyAI>();

            swarmCrawlerEnemy2.enemyType = ScriptableObject.CreateInstance<EnemyType>();

            swarmCrawlerEnemy2.enemyType.enemyPrefab = dlgCrawler2;
            swarmCrawlerEnemy2.enemyType.enemyPrefab.name = "DLGStrongSwarmCrawler";
            swarmCrawlerEnemy2.enemyType.name = "DLGStrongSwarmCrawler";
            swarmCrawlerEnemy2.enemyType.enemyName = "DLGStrongSwarmCrawler";

            roundManager.currentLevel.Enemies.Add(swarmCrawlerEnemy2);

            maxEnemiesAtTime = hazardLevel * Mathf.CeilToInt(dangerLevel / 10) * DLGModMain.playersAmount;

            swarmAllocation = new GameObject();
            swarmAllocation.AddComponent<SwarmAllocation>();

            swarmAllocation.GetComponent<SwarmAllocation>().maxEnemiesPerPlayer = Mathf.CeilToInt(maxEnemiesAtTime / DLGModMain.playersAmount);

            swarmAllocation.GetComponent<SwarmAllocation>().players = allPlayersScripts;

            swarmAllocation.GetComponent<SwarmAllocation>().enemiesTargeting = new int[allPlayersScripts.Length];

            hasStarted = true;
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

            RoundManager roundManager = GameObject.FindObjectOfType<RoundManager>();

            if (swarmEnemiesIndex.Count == 0)
            {
                swarmEnemiesIndex.Add(((roundManager.currentLevel.Enemies.FindIndex(enemy => enemy.enemyType.enemyName == "DLGSwarmCrawler"))));
                swarmEnemiesIndex.Add(((roundManager.currentLevel.Enemies.FindIndex(enemy => enemy.enemyType.enemyName == "DLGStrongSwarmCrawler"))));
            }

            int rollDice = UnityEngine.Random.Range(30, 100);

            DLGModMain.logger.LogInfo($"Current swarm chance: {chance}\n" +
                $"Randomly chosen swarm value: {rollDice}");

            int enemiesAmount = 0;

            foreach (DLGEnemyAI enemy in GameObject.FindObjectsOfType<DLGEnemyAI>())
            {
                if (!enemy.enemyAI.isEnemyDead && enemy.dLGEnemyType.ToString().Contains("Swarm")) enemiesAmount++;
            }

            if (rollDice < chance && enemiesToSpawn == 0)
            {
                enemiesToSpawn = DLGModMain.playersAmount * 5 * hazardLevel * Mathf.CeilToInt(dangerLevel / 10);

                SpawnSwarmEnemies(roundManager, enemiesAmount);

                isSwarm = true;

                hudManager.AddTextToChatOnServer("SWARM!");

                chance = 0f;
            }
            else if (enemiesToSpawn != 0)
            {
                if (isSwarm && enemiesAmount < maxEnemiesAtTime)
                {
                    SpawnSwarmEnemies(roundManager, enemiesAmount);
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

                chance += dangerLevel * __instance.normalizedTimeOfDay * Random.Range(0.5f, 1f) * hazardLevel;
            }

            hasBeenCalled = true;
        }

        private static void SpawnSwarmEnemies(RoundManager roundManager, int currentEnemiesAmount)
        {
            for (int i = currentEnemiesAmount; i < maxEnemiesAtTime; i++)
            {
                int enemyToSpawn = swarmEnemiesIndex[0];
                EnemyVent vent = roundManager.allEnemyVents[UnityEngine.Random.Range(1, roundManager.allEnemyVents.Length)];

                float randomNum = UnityEngine.Random.Range(0, 1f);
                
                if (randomNum > 0.8f)
                {
                    enemyToSpawn = swarmEnemiesIndex[1];
                }

                roundManager.SpawnEnemyOnServer(vent.transform.position, vent.transform.eulerAngles.y, enemyToSpawn);

                enemiesToSpawn--;

                if (enemiesToSpawn == 0)
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
