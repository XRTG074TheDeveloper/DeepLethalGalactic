using DLGMod;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;

public class DLGEnemyAI : MonoBehaviour
{
    public enum DLGEnemyType
    {
        EnemyPrefab,
        SwarmEnemy,
        StrongSwarmEnemy,
        CuteUWULootbug
    }

    public DLGEnemyType dLGEnemyType;

    private SwarmAllocation swarmAllocation;
    public EnemyAI enemyAI;

    float timer = 0;

    public int targetPlayerIndex = -1;

    GameObject[] allAINodes;

    void Start()
    {
        SetUpEnemy();
    }

    void OnEnable()
    {
        SetUpEnemy();
    }

    void SetUpEnemy()
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

        if (!gameObject.name.Contains("Clone"))
        {
            dLGEnemyType = DLGEnemyType.EnemyPrefab;
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
        if (dLGEnemyType == DLGEnemyType.EnemyPrefab &&
            ((enemyAI.IsHost && swarmAllocation == null) || (!enemyAI.IsHost && GameObject.FindObjectOfType<Terminal>() == null))) gameObject.SetActive(false);
        else if (dLGEnemyType != DLGEnemyType.EnemyPrefab &&
            ((enemyAI.IsHost && swarmAllocation == null) || (!enemyAI.IsHost && GameObject.FindObjectOfType<Terminal>() == null))) Destroy(gameObject);

        if (enemyAI.isEnemyDead ||
            dLGEnemyType == DLGEnemyType.EnemyPrefab || enemyAI.currentBehaviourStateIndex == 1 ||
            dLGEnemyType == DLGEnemyType.CuteUWULootbug) return;

        if (!enemyAI.IsHost) gameObject.GetComponent<DLGEnemyAI>().enabled = false;

        timer += Time.deltaTime;

        if (timer < 7) return;

        if (targetPlayerIndex == -1 || !IsValidTarget(targetPlayerIndex))
        {
            targetPlayerIndex = CalculateTargetPlayer();
        }

        if (targetPlayerIndex != -1)
        {
            DLGModMain.logger.LogInfo($"{gameObject.name} is moving towards the player: {swarmAllocation.players[targetPlayerIndex].playerUsername}");

            List<GameObject> AINodes = new List<GameObject> { swarmAllocation.players[targetPlayerIndex].gameObject };

            enemyAI.allAINodes = AINodes.ToArray();
            enemyAI.currentSearch.inProgress = false;
        }
        else
        {
            DLGModMain.logger.LogInfo("...but there is no player that could be targeted");

            enemyAI.allAINodes = allAINodes;
            enemyAI.currentSearch.inProgress = false;
        }

        timer = 0;
    }

    private int CalculateTargetPlayer()
    {
        int result = -1;

        DLGModMain.logger.LogInfo($"{gameObject.name} is calculating new target player...");

        for (int i = 0; i < swarmAllocation.players.Length; i++)
        {
            PlayerControllerB player = swarmAllocation.players[i];

            if (player.isPlayerControlled && player.isInsideFactory != enemyAI.enemyType.isOutsideEnemy)
            {
                int maxEnemiesPerPlayer = enemyAI.enemyType.isOutsideEnemy ? swarmAllocation.currentMaxEnemiesPerPlayer.Item2 : swarmAllocation.currentMaxEnemiesPerPlayer.Item1;

                if (swarmAllocation.enemiesTargeting[i] < maxEnemiesPerPlayer)
                {
                    result = i;
                }
                else {
                    DLGModMain.logger.LogError($"Player: {player.playerUsername} is not valid for targeting cuz it is overtargeted: {swarmAllocation.enemiesTargeting[i]}");
                }
            }
            else {
                DLGModMain.logger.LogError($"Player: {player.playerUsername} is not valid for targeting cuz it is not in the correct area or dead");
            }
        }

        if (result != -1)
        {
            swarmAllocation.enemiesTargeting[result]++;
        }

        return result;
    }

    private bool IsValidTarget(int playerIndex)
    {
        PlayerControllerB player = swarmAllocation.players[playerIndex];
        bool isValid = !player.isPlayerDead && player.isInsideFactory != enemyAI.enemyType.isOutsideEnemy;

        if (!isValid) 
        {
            swarmAllocation.enemiesTargeting[playerIndex]--;
        }

        return isValid;
    }
}

[HarmonyPatch(typeof(CrawlerAI))]
internal class SwarmCrawlerPatch
{
    [HarmonyPatch("MakeScreech")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> SetSwarmCralwerScreechVolume(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codeInstructions = new List<CodeInstruction>(instructions);

        bool transpiledScreechVolume = false;

        for (int i = 0; i < codeInstructions.Count; i++)
        {
            if (i < codeInstructions.Count - 2 &&
                codeInstructions[i].opcode == OpCodes.Ldloc_0 && codeInstructions[i + 1].opcode == OpCodes.Ldelem_Ref &&
                !transpiledScreechVolume)
            {
                codeInstructions.Insert(i + 2,
                                        new CodeInstruction(OpCodes.Ldc_R4, 0.2f));
                codeInstructions[i + 3] = new CodeInstruction(OpCodes.Callvirt,
                    AccessTools.Method(typeof(UnityEngine.AudioSource), "PlayOneShot", new System.Type[]
                    {
                        typeof(UnityEngine.AudioClip), typeof(float)
                    }));

                transpiledScreechVolume = true;
            }
            else if (codeInstructions[i].opcode == OpCodes.Ldc_R4 && codeInstructions[i].operand.ToString() == "0.75")
            {
                codeInstructions[i] = new CodeInstruction(OpCodes.Ldc_R4, 0.2f);
            }
        }

        return codeInstructions.AsEnumerable();
    }

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
            else if (__instance.gameObject.GetComponent<DLGEnemyAI>().targetPlayerIndex != -1)
            {
                GameObject.FindObjectOfType<SwarmAllocation>()
                .enemiesTargeting[__instance.gameObject.GetComponent<DLGEnemyAI>().targetPlayerIndex]--;
                __instance.gameObject.GetComponent<DLGEnemyAI>().targetPlayerIndex = -1;
            }
        }
    }
}
