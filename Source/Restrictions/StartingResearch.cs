using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace IdeologyReformation.Restrictions;

/// <summary>Research the scenario grants at game start, which the game applies only after the ideoligion is configured.</summary>
internal static class StartingResearch
{
    private static readonly FieldInfo FactionDefField = ReflectionSupport.RequireField(typeof(ScenPart_PlayerFaction), "factionDef");
    private static readonly FieldInfo ProjectField = ReflectionSupport.RequireField(typeof(ScenPart_StartingResearch), "project");

    /// <summary>True during new-game setup when the scenario's faction tags or starting research parts include the project.</summary>
    public static bool Grants(ResearchProjectDef project)
    {
        if (Current.ProgramState != ProgramState.Entry || Find.Scenario is not Scenario scenario)
        {
            return false;
        }

        foreach (ScenPart part in scenario.AllParts)
        {
            if (part is ScenPart_PlayerFaction playerFaction
                && FactionDefField?.GetValue(playerFaction) is FactionDef factionDef
                && factionDef.startingResearchTags?.Any(project.HasTag) == true)
            {
                return true;
            }

            if (part is ScenPart_StartingResearch startingResearch && ProjectField?.GetValue(startingResearch) == project)
            {
                return true;
            }
        }

        return false;
    }
}
