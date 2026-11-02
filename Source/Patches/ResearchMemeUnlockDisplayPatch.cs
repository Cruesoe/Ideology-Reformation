using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using IdeologyReformation.Restrictions;

namespace IdeologyReformation.Patches;

/// <summary>Adds research-gated memes to the research project's Unlocks list, read from their <see cref="MemeAvailabilityExtension"/>.</summary>
[HarmonyPatch(typeof(ResearchProjectDef), nameof(ResearchProjectDef.UnlockedDefs), MethodType.Getter)]
internal static class ResearchMemeUnlockDisplayPatch
{
    private static Dictionary<string, List<MemeDef>>? memesByProject;

    [HarmonyPostfix]
    private static void Postfix(ResearchProjectDef __instance, List<Def> __result)
    {
        if (!MemesByProject().TryGetValue(__instance.defName, out List<MemeDef>? unlockedMemes))
        {
            return;
        }

        foreach (MemeDef meme in unlockedMemes)
        {
            if (!__result.Contains(meme))
            {
                __result.Add(meme);
            }
        }
    }

    // Built once, keyed by project defName so generated projects (Node Research) match too.
    private static Dictionary<string, List<MemeDef>> MemesByProject()
    {
        if (memesByProject != null)
        {
            return memesByProject;
        }

        memesByProject = new Dictionary<string, List<MemeDef>>();
        foreach (MemeDef meme in DefDatabase<MemeDef>.AllDefsListForReading.OrderBy(meme => meme.label))
        {
            MemeAvailabilityExtension? restriction = meme.GetModExtension<MemeAvailabilityExtension>();
            if (restriction == null)
            {
                continue;
            }

            IEnumerable<string> projects = (restriction.requiredResearch?.Select(project => project.defName) ?? Enumerable.Empty<string>())
                .Concat(restriction.requiredResearchDefNames ?? Enumerable.Empty<string>());
            foreach (string project in projects.Distinct())
            {
                if (!memesByProject.TryGetValue(project, out List<MemeDef>? memes))
                {
                    memes = new List<MemeDef>();
                    memesByProject[project] = memes;
                }

                memes.Add(meme);
            }
        }

        return memesByProject;
    }
}
