using System.Collections.Generic;
using RimWorld;
using Verse;

namespace IdeologyReformation.Restrictions;

/// <summary>
/// Optional <see cref="DefModExtension"/> for <see cref="MemeDef"/>. Any mod can attach this
/// (via its own def, or an XML patch onto a vanilla/other mod's MemeDef) to gate that meme
/// behind finished research, without needing a reference to this mod's assembly.
/// Absence of the extension means the meme is unrestricted.
/// </summary>
public class MemeAvailabilityExtension : DefModExtension
{
    /// <summary>Meme requires all of these research projects to be finished. Leave null/empty for none.</summary>
    public List<ResearchProjectDef>? requiredResearch;

    /// <summary>
    /// Meme requires all research projects with these def names to be finished. This supports
    /// projects generated after XML cross-reference resolution, such as Node Research's
    /// Emergence projects.
    /// </summary>
    public List<string>? requiredResearchDefNames;
}
