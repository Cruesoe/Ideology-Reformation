using System.Collections.Generic;
using RimWorld;
using Verse;

namespace IdeologyReformation.Restrictions;

/// <summary>Gates a <see cref="MemeDef"/> behind finished research; any mod can add it by XML patch.</summary>
public class MemeAvailabilityExtension : DefModExtension
{
    /// <summary>Every listed project must be finished.</summary>
    public List<ResearchProjectDef>? requiredResearch;

    /// <summary>Every project with these def names must be finished; for projects generated after cross-references resolve, such as Node Research's.</summary>
    public List<string>? requiredResearchDefNames;
}
