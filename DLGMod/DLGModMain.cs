﻿using BepInEx;
using BepInEx.Logging;
using DLGMod.Patches;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DLGMod
{
    [BepInPlugin(GUID, "Deep Lethal Galactic", "0.5.5")]
    public class DLGModMain : BaseUnityPlugin
    {
        internal const string GUID = "XRTG074TheDeveloper.DeepLethalGalactic";

        internal readonly Harmony harmonyInstance = new Harmony(GUID);

        internal static ManualLogSource logger;

        internal static string filesPath;



        internal static List<AudioClip> swarmSFX;

        internal static int playersAmount;

        void Awake()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource(GUID);

            filesPath = this.Info.Location;
            filesPath = filesPath.TrimEnd("DLGMod.dll".ToCharArray());

            AssetBundle assetBundle = AssetBundle.LoadFromFile(filesPath + "\\swarmmusic");

            if (assetBundle != null)
            {
                swarmSFX = assetBundle.LoadAllAssets<AudioClip>().ToList();
            }

            harmonyInstance.PatchAll(typeof(GameControllerPatch));
            harmonyInstance.PatchAll(typeof(AmmunitionPatch));
            harmonyInstance.PatchAll(typeof(ChatCommandsPatch));
            harmonyInstance.PatchAll(typeof(SwarmPatch));
            harmonyInstance.PatchAll(typeof(DLGTipsPatch));
            harmonyInstance.PatchAll(typeof(SpawnAmmunitionPatch));
            harmonyInstance.PatchAll(typeof(PlayerControllerPatch));
            harmonyInstance.PatchAll(typeof(MissionControllerPatch));
            harmonyInstance.PatchAll(typeof(DLGNetStuffSync));
            harmonyInstance.PatchAll(typeof(DLGEnemyAIPatch));
            harmonyInstance.PatchAll(typeof(SwarmCrawlerPatch));
            harmonyInstance.PatchAll(typeof(NetworkManagerPatch));
        }

        internal static void SendAmmunition(int _playersAmount)
        {
            playersAmount = _playersAmount;

            AmmunitionPatch.shouldBeSent = true;
        }
    }
}
