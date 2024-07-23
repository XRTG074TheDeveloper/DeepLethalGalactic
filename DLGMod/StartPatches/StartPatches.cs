using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace DLGMod.StartPatches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class WelcomeSpeechPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void SetUpStarterSound(StartOfRound __instance)
        {
            __instance.shipIntroSpeechSFX = DLGModMain.MissionControlQuotesSFX[0];
        }

        [HarmonyPatch(nameof(StartOfRound.StartGame))]
        [HarmonyPostfix]
        public static void CheckChosenLevel(StartOfRound __instance)
        {
            if (__instance.currentLevelID != 3)
            {
                DLGModMain.SendAmmunition(__instance.connectedPlayersAmount + 1);
            }
        }
    }

    [HarmonyPatch(typeof(Terminal))]
    internal class AmmunitionPatch
    {
        internal static bool shouldBeSent = false;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void SendAmmunition(Terminal __instance)
        {
            if (!shouldBeSent) return;

            Item shotgunItem = Resources.FindObjectsOfTypeAll<AllItemsList>()[0].itemsList[59];
            Item shotgunShell = Resources.FindObjectsOfTypeAll<AllItemsList>()[0].itemsList[60];

            __instance.buyableItemsList[0] = shotgunItem;
            __instance.buyableItemsList[1] = shotgunShell;

            List<int> ammunitionOrder = new List<int>();

            for (int i = 0; i < DLGModMain.playersAmount; i++)
            {
                ammunitionOrder.Add(0);
            }

            __instance.BuyItemsServerRpc(ammunitionOrder.ToArray(), __instance.groupCredits, 0);

            shouldBeSent = false;
        }
    }
}
