using DLGMod;
using GameNetcodeStuff;
using JetBrains.Annotations;
using System;
using UnityEngine;

public class SwarmAllocation : MonoBehaviour
{
    public int[] enemiesTargeting;

    public int maxEnemiesAtTime;
    public Tuple<int, int> currentMaxEnemiesPerPlayer;

    public PlayerControllerB[] players;

    float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer < 10f) return;

        int insideFactoryCount = 0;
        int outsideFactoryCount = 0;

        foreach (PlayerControllerB player in players)
        {
            if (player.isInsideFactory)
            {
                insideFactoryCount++;
            }
            else
            {
                outsideFactoryCount++;
            }
        }

        int insideTarget = 0;
        if (insideFactoryCount != 0)
            insideTarget = maxEnemiesAtTime / insideFactoryCount;

        int outsideTarget = 0;
        if (outsideFactoryCount != 0)
            outsideTarget = maxEnemiesAtTime / outsideFactoryCount;

        currentMaxEnemiesPerPlayer = new Tuple<int, int>(insideTarget, outsideTarget);

        timer = 0f;
    }
}
