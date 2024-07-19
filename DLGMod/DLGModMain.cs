using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;

namespace DLGMod
{
    [BepInPlugin(GUID, "Deep Lethal Company", "0.0.0.1")]
    public class DLGModMain : BaseUnityPlugin
    {
        internal const string GUID = "XRTG074TheDeveloper.DeepLethalGalactic";

        internal readonly Harmony harmonyInstance = new Harmony(GUID);

        internal ManualLogSource log;


        internal static List<AudioClip> MissionControlQuotesSFX;

        void Awake()
        {
            log = BepInEx.Logging.Logger.CreateLogSource(GUID);

            MissionControlQuotesSFX = new List<AudioClip>();

            string filesPath = this.Info.Location;
            filesPath = filesPath.TrimEnd("DeepLethalGalactic.dll".ToCharArray());

            //AssetBundle assetBundle = AssetBundle.LoadFromFile(>> Path here <<);
        }
    }
}
