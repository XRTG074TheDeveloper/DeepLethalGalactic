using HarmonyLib;

namespace DLGMod.StartPatches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StarterSoundPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void SetUpStarterSound(StartOfRound __instance)
        {
            __instance.shipIntroSpeechSFX = DLGModMain.MissionControlQuotesSFX[0];
        }
    }
}
