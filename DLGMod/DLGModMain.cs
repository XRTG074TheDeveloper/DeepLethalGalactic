using BepInEx;
using BepInEx.Logging;
using DLGMod.Patches;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DLGMod
{
    [BepInPlugin(GUID, "Deep Lethal Galactic", "1.4.0")]
    public class DLGModMain : BaseUnityPlugin
    {
        internal const string GUID = "XRTG074TheDeveloper.DeepLethalGalactic";

        internal readonly Harmony harmonyInstance = new Harmony(GUID);

        internal static ManualLogSource logger;

        internal static string filesPath;


        internal static List<AudioClip> MissionControlQuotesSFX;

        internal static List<AudioClip> swarmSFX;

        internal static int playersAmount;

        void Awake()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource(GUID);

            //MissionControlQuotesSFX = new List<AudioClip>();

            //filesPath = this.Info.Location;
            //filesPath = filesPath.TrimEnd("DLGMod.dll".ToCharArray());

            //AssetBundle assetBundle = AssetBundle.LoadFromFile(filesPath + "\\mission_control_quotes");

            //if (assetBundle != null)
            //{
            //    MissionControlQuotesSFX = assetBundle.LoadAllAssets<AudioClip>().ToList();
            //}

            AssetBundle assetBundle = AssetBundle.LoadFromFile(filesPath + "\\swarm");

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
        }

        internal static void SendAmmunition(int _playersAmount)
        {
            playersAmount = _playersAmount;

            AmmunitionPatch.shouldBeSent = true;
        }
    }
}
