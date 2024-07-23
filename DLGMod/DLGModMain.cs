using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

using DLGMod.StartPatches;
using System.Runtime.CompilerServices;

namespace DLGMod
{
    [BepInPlugin(GUID, "Deep Lethal Company", "0.0.0.1")]
    public class DLGModMain : BaseUnityPlugin
    {
        internal const string GUID = "XRTG074TheDeveloper.DeepLethalGalactic";

        internal readonly Harmony harmonyInstance = new Harmony(GUID);

        internal static ManualLogSource logger;

        internal static string filesPath;


        internal static List<AudioClip> MissionControlQuotesSFX;

        internal static int playersAmount;

        void Awake()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource(GUID);

            MissionControlQuotesSFX = new List<AudioClip>();

            filesPath = this.Info.Location;
            filesPath = filesPath.TrimEnd("DLGMod.dll".ToCharArray());

            AssetBundle assetBundle = AssetBundle.LoadFromFile(filesPath + "SoundBundles\\MissionControlQuotes\\" + "mission_control_quotes");

            if (assetBundle != null)
            {
                MissionControlQuotesSFX = assetBundle.LoadAllAssets<AudioClip>().ToList();
            }

            harmonyInstance.PatchAll(typeof(WelcomeSpeechPatch));
            harmonyInstance.PatchAll(typeof(AmmunitionPatch));
        }

        internal static void SendAmmunition(int _playersAmount)
        {
            playersAmount = _playersAmount;

            AmmunitionPatch.shouldBeSent = true;
        }
    }
}
