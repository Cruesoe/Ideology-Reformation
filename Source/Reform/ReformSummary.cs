using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace IdeologyReformation.Reform;

internal sealed class ReformSummary
{
    public List<MemeDef> PrimaryMemeCards { get; } = new();
    public List<Precept> PrimaryPreceptCards { get; } = new();
    public List<Precept> ConsequencePreceptCards { get; } = new();
    public List<Precept> PreceptCards { get; } = new();
    public HashSet<int> RemovedPreceptIds { get; } = new();
    public List<string> CosmeticChanges { get; } = new();
    public bool HasMechanicalObject { get; private set; }

    public string CosmeticText => BulletLines(CosmeticChanges);

    public static ReformSummary Build(ReformSession session)
    {
        ReformSummary summary = new ReformSummary
        {
            HasMechanicalObject = session.SelectedObject != null
        };

        List<Precept> originalPrecepts = session.Original.PreceptsListForReading;
        List<Precept> workingPrecepts = session.Working.PreceptsListForReading;
        HashSet<int> originalIds = originalPrecepts.Select(p => p.Id).ToHashSet();
        HashSet<int> workingIds = workingPrecepts.Select(p => p.Id).ToHashSet();
        List<Precept> added = workingPrecepts.Where(p => !originalIds.Contains(p.Id)).ToList();
        List<Precept> removed = originalPrecepts.Where(p => !workingIds.Contains(p.Id)).ToList();

        AddPrimaryMemeCard(summary, session);
        AddPrimaryPreceptCards(summary, session.SelectedObject, originalPrecepts, workingPrecepts, added, removed);

        foreach (Precept precept in added.Where(p => !IsPrimaryPrecept(session.SelectedObject, p)))
        {
            summary.ConsequencePreceptCards.Add(precept);
        }

        foreach (Precept precept in removed.Where(p => !IsPrimaryPrecept(session.SelectedObject, p)))
        {
            summary.ConsequencePreceptCards.Add(precept);
        }

        BuildPreceptCards(summary, workingPrecepts);
        summary.RemovedPreceptIds.UnionWith(summary.PreceptCards.Where(p => !workingIds.Contains(p.Id)).Select(p => p.Id));
        AddCosmeticDetails(summary, session.Original, session.Working);
        return summary;
    }

    private static void BuildPreceptCards(ReformSummary summary, List<Precept> workingPrecepts)
    {
        HashSet<string> seen = new();
        foreach (Precept candidate in summary.PrimaryPreceptCards.Concat(summary.ConsequencePreceptCards))
        {
            IssueDef? issue = candidate.def.issue;
            string key = issue != null && !issue.allowMultiplePrecepts
                ? $"issue:{issue.defName}"
                : $"precept:{candidate.Id}";
            if (!seen.Add(key))
            {
                continue;
            }

            // A replacement produces both an added and removed card. For a
            // single-value issue, display only the value that survives the reform.
            Precept card = issue != null && !issue.allowMultiplePrecepts
                ? workingPrecepts.LastOrDefault(precept => precept.def.issue == issue) ?? candidate
                : candidate;
            summary.PreceptCards.Add(card);
        }
    }

    private static void AddPrimaryMemeCard(ReformSummary summary, ReformSession session)
    {
        ReformObject? selected = session.SelectedObject;
        if (selected?.Kind == ReformObjectKind.Structure)
        {
            MemeDef? structure = session.Working.StructureMeme ?? session.Original.StructureMeme;
            if (structure != null)
            {
                summary.PrimaryMemeCards.Add(structure);
            }
            return;
        }

        if (selected?.Kind != ReformObjectKind.Meme)
        {
            return;
        }

        MemeDef? meme = session.Working.memes
            .Concat(session.Original.memes)
            .FirstOrDefault(candidate => selected.Key == $"meme:{candidate.defName}");
        if (meme != null)
        {
            summary.PrimaryMemeCards.Add(meme);
        }
    }

    private static void AddPrimaryPreceptCards(
        ReformSummary summary,
        ReformObject? selected,
        List<Precept> originalPrecepts,
        List<Precept> workingPrecepts,
        List<Precept> added,
        List<Precept> removed)
    {
        if (selected == null || selected.Kind is ReformObjectKind.Meme or ReformObjectKind.Structure)
        {
            return;
        }

        // Prefer the post-change card. Fall back to the opening card when the
        // deliberate action removed an object without replacing it.
        summary.PrimaryPreceptCards.AddRange(added.Where(p => IsPrimaryPrecept(selected, p)));
        if (summary.PrimaryPreceptCards.Count == 0)
        {
            foreach (Precept before in originalPrecepts.Where(p => IsPrimaryPrecept(selected, p)))
            {
                Precept? after = workingPrecepts.FirstOrDefault(p => p.Id == before.Id);
                summary.PrimaryPreceptCards.Add(after ?? before);
            }
        }

        if (summary.PrimaryPreceptCards.Count == 0)
        {
            summary.PrimaryPreceptCards.AddRange(removed.Where(p => IsPrimaryPrecept(selected, p)));
        }
    }

    private static void AddCosmeticDetails(ReformSummary summary, Ideo original, Ideo working)
    {
        if (original.name != working.name)
        {
            summary.CosmeticChanges.Add("IR_NameChanged".Translate(original.name, working.name));
        }
        if (original.description != working.description)
        {
            summary.CosmeticChanges.Add("IR_DescriptionChanged".Translate());
        }
        if (original.adjective != working.adjective || original.memberName != working.memberName)
        {
            summary.CosmeticChanges.Add("IR_NarrativeTermsChanged".Translate());
        }
        if (original.iconDef != working.iconDef || original.colorDef != working.colorDef)
        {
            summary.CosmeticChanges.Add("IR_IconColorChanged".Translate());
        }
        if (original.leaderTitleMale != working.leaderTitleMale || original.leaderTitleFemale != working.leaderTitleFemale)
        {
            summary.CosmeticChanges.Add("IR_LeaderTitlesChanged".Translate());
        }
        if (!original.thingStyleCategories.SetsEqual(working.thingStyleCategories))
        {
            summary.CosmeticChanges.Add("IR_StylesChanged".Translate());
        }

        Dictionary<int, Precept> byId = working.PreceptsListForReading.ToDictionary(p => p.Id);
        foreach (Precept before in original.PreceptsListForReading)
        {
            if (byId.TryGetValue(before.Id, out Precept? after) && before.Label != after.Label)
            {
                summary.CosmeticChanges.Add("IR_PreceptNameChanged".Translate(before.Label, after.Label));
            }
        }
    }

    private static bool IsPrimaryPrecept(ReformObject? selected, Precept precept)
    {
        return selected != null && selected.Equals(ReformObject.ForPrecept(precept));
    }

    private static string BulletLines(IEnumerable<string> lines)
    {
        return string.Join("\n", lines.Select(line => "  - " + line));
    }
}
