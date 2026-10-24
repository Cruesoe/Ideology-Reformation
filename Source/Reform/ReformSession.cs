using RimWorld;
using System.Linq;
using Verse;

namespace IdeologyReformation.Reform;

internal sealed class ReformSession
{
    public Dialog_ReformIdeo Dialog { get; }
    public Ideo Original { get; }
    public Ideo Working { get; }
    public ReformObject? SelectedObject { get; private set; }
    public bool ReviewApproved { get; set; }
    public bool CommitStarted { get; private set; }

    public ReformSession(Dialog_ReformIdeo dialog, Ideo original, Ideo working)
    {
        Dialog = dialog;
        Original = original;
        Working = working;
    }

    public bool TrySelect(ReformObject reformObject, bool notify = true)
    {
        if (SelectedObject == null)
        {
            SelectedObject = reformObject;
            Diagnostics.Message($"Selected reform object: {reformObject.Key}");
            return true;
        }

        if (SelectedObject.Equals(reformObject))
        {
            return true;
        }

        if (notify)
        {
            Messages.Message(
                "IR_AnotherObjectLocked".Translate(SelectedObject.Label),
                MessageTypeDefOf.RejectInput,
                historical: false);
        }
        Diagnostics.Message($"Rejected second reform object {reformObject.Key}; locked to {SelectedObject.Key}");
        return false;
    }

    public void RefreshSelection(Precept precept)
    {
        if (SelectedObject == null || SelectedObject.Kind is not (ReformObjectKind.Ritual or ReformObjectKind.Relic or ReformObjectKind.Building))
        {
            return;
        }

        ReformObject current = ReformObject.ForPrecept(precept);
        if (SelectedObject.Kind == current.Kind && SelectedObject.Key.EndsWith(":-1"))
        {
            SelectedObject = current;
            Diagnostics.Message($"Re-keyed reform object to {current.Key}");
        }
    }

    public bool CanConfigureMemeConsequence(Precept precept)
    {
        if (SelectedObject?.Kind != ReformObjectKind.Meme || precept.def.issue == null)
        {
            return false;
        }

        // A meme may introduce an issue or replace an existing issue's value to meet
        // its requirements. Choosing the value for that changed issue configures the
        // meme consequence rather than selecting a second deliberate reform object.
        var before = Original.PreceptsListForReading.Where(p => p.def.issue == precept.def.issue).ToList();
        var after = Working.PreceptsListForReading.Where(p => p.def.issue == precept.def.issue).ToList();
        return before.Count != after.Count
            || before.Any(old => !after.Any(current => current.Id == old.Id && current.def == old.def));
    }

    public void Reset()
    {
        Original.CopyTo(Working);
        SelectedObject = null;
        ReviewApproved = false;
        CommitStarted = false;
        Diagnostics.Message("Reform session reset to opening snapshot.");
    }

    public bool TryBeginCommit()
    {
        if (CommitStarted)
        {
            Diagnostics.Message("Ignored duplicate reform commit callback.");
            return false;
        }

        CommitStarted = true;
        return true;
    }
}
