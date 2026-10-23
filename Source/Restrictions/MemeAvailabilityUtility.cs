using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace IdeologyReformation.Restrictions;

public static class MemeAvailabilityUtility
{
    public static bool IsMemeAvailable(MemeDef meme)
    {
        return LockedReason(meme) == null;
    }

    /// <summary>Player-facing explanation of why this meme cannot be taken yet, or null when it can.</summary>
    public static string? LockedReason(MemeDef meme)
    {
        MemeAvailabilityExtension? restriction = meme.GetModExtension<MemeAvailabilityExtension>();
        if (restriction == null || TitleScreenEditorSupport.IsEditingFromTitleScreen())
        {
            return null;
        }

        List<string> missing = restriction.requiredResearch?
            .Where(project => !project.IsFinished)
            .Select(project => project.LabelCap.ToString())
            .ToList() ?? new List<string>();

        if (restriction.requiredResearchDefNames is { Count: > 0 })
        {
            missing.AddRange(restriction.requiredResearchDefNames
                .Select(defName => new
                {
                    DefName = defName,
                    Project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName)
                })
                .Where(requirement => requirement.Project == null || !requirement.Project.IsFinished)
                .Select(requirement => requirement.Project?.LabelCap.ToString() ?? requirement.DefName));
        }

        if (missing.Count > 0)
        {
            return "FIO_MemeLockedResearch".Translate(missing.Distinct().ToList().ToCommaList(useAnd: true));
        }

        return null;
    }
}
