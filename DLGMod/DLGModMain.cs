using BepInEx;
using HarmonyLib;

namespace DLGMod
{
    [BepInPlugin(GUID, "Deep Lethal Company", "0.0.0.1")]
    public class DLGModMain : BaseUnityPlugin
    {
        internal const string GUID = "XRTG074TheDeveloper.DeepLethalGalactic";

        internal readonly Harmony harmonyInstance = new Harmony(GUID);
    }
}
