using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using IdeologyReformation.Restrictions;

namespace IdeologyReformation.Patches;

/// <summary>
/// Replaces vanilla's "you can reform your ideoligion" letter with this mod's own text, and appends
/// a note listing memes that became available (per <see cref="MemeAvailabilityTracker"/>) since
/// the last such letter for any ideo in this save.
/// </summary>
[HarmonyPatch(typeof(IdeoDevelopmentTracker), nameof(IdeoDevelopmentTracker.TryAddDevelopmentPoints))]
internal static class ReformAvailableLetterPatch
{
    [HarmonyPrefix]
    private static bool Prefix(IdeoDevelopmentTracker __instance, int pointsToAdd, ref bool __result)
    {
        if (pointsToAdd <= 0 || __instance.CanReformNow)
        {
            __result = false;
            return false;
        }

        bool couldReformBefore = __instance.CanReformNow;
        __instance.points = Mathf.Min(__instance.points + pointsToAdd, __instance.NextReformationDevelopmentPoints);

        if (!couldReformBefore && __instance.CanReformNow)
        {
            string newMemesText = BuildNewlyAvailableMemesText();
            Find.LetterStack.ReceiveLetter(
                "LetterLabelReformIdeo".Translate(),
                "IR_LetterTextReformIdeo".Translate(__instance.ideo.name, newMemesText),
                LetterDefOf.PositiveEvent);
        }

        __result = true;
        return false;
    }

    private static string BuildNewlyAvailableMemesText()
    {
        MemeAvailabilityTracker? tracker = Current.Game?.GetComponent<MemeAvailabilityTracker>();
        if (tracker == null)
        {
            return string.Empty;
        }

        List<MemeDef> newlyAvailable = tracker.ComputeNewlyAvailableAndAdvance();
        if (newlyAvailable.Count == 0)
        {
            return string.Empty;
        }

        string memeList = string.Join(", ", newlyAvailable.Select(m => m.LabelCap));
        return "IR_NewlyAvailableMemes".Translate(memeList);
    }
}
