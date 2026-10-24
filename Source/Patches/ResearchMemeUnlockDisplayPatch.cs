using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using IdeologyReformation.Restrictions;

namespace IdeologyReformation.Patches;

/// <summary>
/// Includes research-gated memes in the research project's ordinary "Unlocks" list.
/// This is derived from the research fields on <see cref="MemeAvailabilityExtension"/> so
/// compatibility XML remains the single source of truth for both enforcement and UI.
/// </summary>
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

/// <summary>
/// Marks the narrow UI scope in which an unlocked def is being drawn in the
/// research details panel. Meme hyperlinks can then be labelled by type without
/// changing their names in ideology screens or other info-card links.
/// </summary>
[HarmonyPatch(typeof(MainTabWindow_Research), "DrawUnlockableHyperlinks")]
internal static class ResearchUnlockLabelScopePatch
{
    internal static bool IsDrawing { get; private set; }

    [HarmonyPrefix]
    private static void Prefix(out bool __state)
    {
        __state = IsDrawing;
        IsDrawing = true;
    }

    [HarmonyPostfix]
    private static void Postfix(bool __state)
    {
        IsDrawing = __state;
    }

    [HarmonyFinalizer]
    private static Exception? Finalizer(Exception? __exception, bool __state)
    {
        IsDrawing = __state;
        return __exception;
    }
}

[HarmonyPatch(typeof(Dialog_InfoCard.Hyperlink), nameof(Dialog_InfoCard.Hyperlink.Label), MethodType.Getter)]
internal static class ResearchMemeUnlockLabelPatch
{
    [HarmonyPostfix]
    private static void Postfix(Dialog_InfoCard.Hyperlink __instance, ref string __result)
    {
        if (ResearchUnlockLabelScopePatch.IsDrawing && __instance.def is MemeDef)
        {
            __result = "IR_ResearchUnlockMeme".Translate(__result);
        }
    }
}
