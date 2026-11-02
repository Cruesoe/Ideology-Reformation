using HarmonyLib;
using RimWorld;
using IdeologyReformation.Restrictions;

namespace IdeologyReformation.Patches;

/// <summary>Applies the meme research gates to random meme generation for the player's ideoligion; other factions are not gated.</summary>
[HarmonyPatch(typeof(IdeoUtility), "CanAdd")]
internal static class MemeAvailabilityRandomizationPatch
{
    [HarmonyPostfix]
    private static void Postfix(MemeDef meme, FactionDef? forFaction, ref bool __result)
    {
        if (__result && (forFaction == null || forFaction.isPlayer) && !MemeAvailabilityUtility.IsMemeAvailable(meme))
        {
            __result = false;
        }
    }
}
