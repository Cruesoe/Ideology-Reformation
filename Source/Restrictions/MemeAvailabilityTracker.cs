using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace IdeologyReformation.Restrictions;

/// <summary>Remembers which memes were available at the last reform letter, so the next letter can list the newly available ones.</summary>
public class MemeAvailabilityTracker : GameComponent
{
    private List<string> knownAvailableMemeDefNames = new();
    private bool initialized;

    public MemeAvailabilityTracker()
    {
    }

    public MemeAvailabilityTracker(Game game)
    {
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref knownAvailableMemeDefNames, "fio_knownAvailableMemes", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            // An empty list means no baseline was recorded; unrestricted memes are always available.
            knownAvailableMemeDefNames ??= new List<string>();
            initialized = knownAvailableMemeDefNames.Count > 0;
        }
    }

    // Baseline at colony start, after the scenario's starting research is applied.
    public override void StartedNewGame()
    {
        ComputeNewlyAvailableAndAdvance();
    }

    // Baseline when the mod is added to an existing save.
    public override void LoadedGame()
    {
        if (!initialized)
        {
            ComputeNewlyAvailableAndAdvance();
        }
    }

    /// <summary>Returns memes available now that weren't at the last call (none on the first call), and records the current set.</summary>
    public List<MemeDef> ComputeNewlyAvailableAndAdvance()
    {
        List<MemeDef> currentlyAvailable = DefDatabase<MemeDef>.AllDefsListForReading
            .Where(MemeAvailabilityUtility.IsMemeAvailable)
            .ToList();

        List<MemeDef> newlyAvailable = initialized
            ? currentlyAvailable.Where(m => !knownAvailableMemeDefNames.Contains(m.defName)).ToList()
            : new List<MemeDef>();

        initialized = true;
        knownAvailableMemeDefNames = currentlyAvailable.Select(m => m.defName).ToList();
        return newlyAvailable;
    }
}
