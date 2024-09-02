using DLGMod;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DLGEnemyAI : MonoBehaviour
{
    public enum DLGEnemyType
    {
        SwarmEnemy,
        StrongSwarmEnemy,
        CuteUWULootbug
    }

    public DLGEnemyType dLGEnemyType;

    private SwarmAllocation swarmAllocation;
    public EnemyAI enemyAI;

    float timer = 0;

    public int targetPlayerIndex = -1;
    int prevTargetPlayerIndex = -1;

    GameObject[] allAINodes;

    Vector3 targetPlayerPosition = Vector3.zero;

    void Start()
    {
        if (gameObject.name.Contains("Swarm"))
        {
            if (gameObject.name.Contains("Strong"))
            {
                dLGEnemyType = DLGEnemyType.StrongSwarmEnemy;
            }
            else
            {
                dLGEnemyType = DLGEnemyType.SwarmEnemy;
            }
        }
        else
        {
            dLGEnemyType = DLGEnemyType.CuteUWULootbug;
        }

        swarmAllocation = GameObject.FindObjectOfType<SwarmAllocation>();
        enemyAI = GetComponent<EnemyAI>();
        allAINodes = enemyAI.allAINodes;

        if (dLGEnemyType == DLGEnemyType.StrongSwarmEnemy)
        {
            enemyAI.enemyHP = 7;
            enemyAI.gameObject.AddComponent<Light>();
            enemyAI.gameObject.GetComponent<Light>().color = Color.red;
            enemyAI.gameObject.GetComponent<Light>().intensity = 15;
            enemyAI.gameObject.GetComponent<Light>().range = 15;
        }
    }

    void Update()
    {
        if (enemyAI.currentBehaviourStateIndex == 1 || dLGEnemyType == DLGEnemyType.CuteUWULootbug) return;

        timer += Time.deltaTime;

        if (timer < 10) return;

        if (targetPlayerIndex == -1 || enemyAI.currentSearch.unsearchedNodes.Count == 0)
        {
            targetPlayerIndex = CalculateTargetPlayer(targetPlayerIndex);
        }

        if (targetPlayerIndex != -1)
        {
            if (!enemyAI.GetPathDistance(swarmAllocation.players[targetPlayerIndex].transform.position, gameObject.transform.position))
            {
                DLGModMain.logger.LogInfo($"Swarm Enemy cannot get to the player: {swarmAllocation.players[targetPlayerIndex].playerUsername}");
                swarmAllocation.enemiesTargeting[targetPlayerIndex]--;
                targetPlayerIndex = -1;
                timer = 0f;
                return;
            }

            DLGModMain.logger.LogInfo($"Swarm Enemy is moving towards the player: {swarmAllocation.players[targetPlayerIndex].playerUsername}");

            List<GameObject> AINodes = new List<GameObject>();
            AINodes.Add(swarmAllocation.players[targetPlayerIndex].gameObject);

            enemyAI.allAINodes = AINodes.ToArray();
            enemyAI.currentSearch.inProgress = false;
        }
        else if ((enemyAI.currentSearch.unsearchedNodes.Count == 0 && prevTargetPlayerIndex != -1) ||
            (prevTargetPlayerIndex != -1 && !swarmAllocation.players[prevTargetPlayerIndex].isInsideFactory))
        {
            DLGModMain.logger.LogInfo("Swarm Enemy doesnt see any player inside anymore. Switching to default roaming...");

            swarmAllocation.enemiesTargeting[prevTargetPlayerIndex]--;

            enemyAI.allAINodes = allAINodes;
            enemyAI.currentSearch.inProgress = false;
        }
        else
        {
            DLGModMain.logger.LogInfo("...but nobody came");
        }

        prevTargetPlayerIndex = targetPlayerIndex;

        timer = 0;
    }

    private int CalculateTargetPlayer(int currentTargetPlayer)
    {
        int result = -1;

        DLGModMain.logger.LogInfo("Swarm Enemy is calculating new target player...");

        for (int i = 0; i < swarmAllocation.players.Length; i++)
        {
            PlayerControllerB player = swarmAllocation.players[i];

            if (player.isPlayerControlled && player.isInsideFactory != enemyAI.enemyType.isOutsideEnemy)
            {
                if (swarmAllocation.enemiesTargeting[i] <= swarmAllocation.maxEnemiesPerPlayer)
                {
                    if (enemyAI.GetPathDistance(swarmAllocation.players[i].transform.position, gameObject.transform.position))
                    {
                        result = i;

                        if (i != currentTargetPlayer)
                        {
                            swarmAllocation.enemiesTargeting[i]++;
                        }

                        break;
                    }
                }
            }
            else
            {
                swarmAllocation.enemiesTargeting[i] = 0;
            }
        }

        return result;
    }
}

[HarmonyPatch(typeof(CrawlerAI))]
internal class SwarmCrawlerDamagePatch
{
    [HarmonyPatch("OnCollideWithPlayer")]
    [HarmonyPrefix]
    public static void SetSwarmCrawlerDamage(CrawlerAI __instance, ref Collider other)
    {
        float timeSinceHittingPlayer = Traverse.Create(__instance).Field("timeSinceHittingPlayer").GetValue<float>();

        EnemyAI enemyAI = __instance.GetComponent<EnemyAI>();

        if (!(timeSinceHittingPlayer < 0.65f))
        {
            int damageAmount = 5;

            if (__instance.GetComponent<DLGEnemyAI>() != null)
            {
                switch (__instance.GetComponent<DLGEnemyAI>().dLGEnemyType)
                {
                    case DLGEnemyAI.DLGEnemyType.SwarmEnemy:
                        damageAmount = 5;
                        break;
                    case DLGEnemyAI.DLGEnemyType.StrongSwarmEnemy:
                        damageAmount = 20;
                        break;
                }
            }

            PlayerControllerB playerControllerB = __instance.MeetsStandardPlayerCollisionConditions(other);
            if (playerControllerB != null)
            {
                timeSinceHittingPlayer = 0f;
                playerControllerB.DamagePlayer(damageAmount, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling);
                enemyAI.agent.speed = 0f;
                __instance.HitPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
                GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);
            }
        }

        Traverse.Create(__instance).Field("timeSinceHittingPlayer").SetValue(timeSinceHittingPlayer);
    }
}

[HarmonyPatch(typeof(EnemyAI))]
internal class DLGEnemyAIPatch
{
    [HarmonyPatch("KillEnemy")]
    [HarmonyPostfix]
    public static void OnEnemyDied(EnemyAI __instance)
    {
        if (__instance.gameObject.GetComponent<DLGEnemyAI>() != null)
        {
            if (__instance.gameObject.GetComponent<DLGEnemyAI>().dLGEnemyType == DLGEnemyAI.DLGEnemyType.CuteUWULootbug)
            {
                DLGModMain.logger.LogError("WHAT HAVE YOU DONE? DID YOU JUST KILL THAT REALLY CUTE LOOTBUG?! YOU SOULLESS BEAST!");

                List<Item> allItemsList = Resources.FindObjectsOfTypeAll<AllItemsList>()[0].itemsList;

                int GoldIndex = allItemsList.FindIndex(item => item.itemName == "Gold bar");

                GameObject GoldBar =
                    GameObject.Instantiate(allItemsList[GoldIndex].spawnPrefab, position: __instance.transform.position, rotation: new Quaternion());
                GoldBar.GetComponent<GrabbableObject>().fallTime = 0f;
                GoldBar.GetComponent<GrabbableObject>().SetScrapValue(UnityEngine.Random.Range(100, 200));
                GoldBar.GetComponent<NetworkObject>().Spawn();
            }
            else
            {
                GameObject.FindObjectOfType<SwarmAllocation>()
                .enemiesTargeting[__instance.gameObject.GetComponent<DLGEnemyAI>().targetPlayerIndex]--;
            }
        }
    }
}