using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using IdeologyReformation.Reform;

namespace IdeologyReformation.Patches;

[HarmonyPatch(typeof(Dialog_ChooseMemes), "DoAcceptChanges")]
internal static class MemePreservationPatches
{
    private static readonly FieldInfo IdeoField = ReflectionSupport.RequireField(typeof(Dialog_ChooseMemes), "ideo");
    private static readonly FieldInfo NewMemesField = ReflectionSupport.RequireField(typeof(Dialog_ChooseMemes), "newMemes");
    private static readonly FieldInfo ReformingField = ReflectionSupport.RequireField(typeof(Dialog_ChooseMemes), "reformingIdeo");

    [HarmonyPrefix]
    private static bool Prefix(Dialog_ChooseMemes __instance, out CascadeState? __state)
    {
        __state = null;
        if (!(bool)ReformingField.GetValue(__instance)!)
        {
            return true;
        }

        Ideo working = (Ideo)IdeoField.GetValue(__instance)!;
        if (!ReformSessions.TryGet(working, out ReformSession session))
        {
            return true;
        }

        List<MemeDef> proposed = (List<MemeDef>)NewMemesField.GetValue(__instance)!;
        ReformObject? reformObject = ReformObject.ForMemeChange(session.Original, proposed);
        if (reformObject == null)
        {
            Messages.Message("IR_OneMemeAtATime".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }

        if (!session.TrySelect(reformObject))
        {
            return false;
        }

        Ideo snapshot = IdeoGenerator.MakeIdeo(working.foundation.def);
        working.CopyTo(snapshot);
        __state = new CascadeState(session, snapshot);
        MutationScope.EnterConsequences();
        return true;
    }

    [HarmonyPostfix]
    private static void Postfix(CascadeState? __state)
    {
        if (__state == null)
        {
            return;
        }

        try
        {
            RestoreCompatiblePrecepts(__state.Snapshot, __state.Session.Working);
        }
        finally
        {
            __state.ExitScope();
        }
    }

    [HarmonyFinalizer]
    private static System.Exception? Finalizer(System.Exception? __exception, CascadeState? __state)
    {
        __state?.ExitScope();
        return __exception;
    }

    private static void RestoreCompatiblePrecepts(Ideo snapshot, Ideo working)
    {
        HashSet<int> currentIds = working.PreceptsListForReading.Select(p => p.Id).ToHashSet();
        foreach (Precept previous in snapshot.PreceptsListForReading)
        {
            if (currentIds.Contains(previous.Id) || !working.CanAddPreceptAllFactions(previous.def).Accepted)
            {
                continue;
            }

            IssueDef issue = previous.def.issue;
            if (issue != null && !issue.allowMultiplePrecepts)
            {
                Precept? replacement = working.PreceptsListForReading.FirstOrDefault(p => p.def.issue == issue);
                if (replacement != null && !snapshot.PreceptsListForReading.Any(p => p.Id == replacement.Id))
                {
                    working.RemovePrecept(replacement, replacing: true);
                }
            }

            if (issue != null && working.HasMaxPreceptsForIssue(issue))
            {
                continue;
            }

            Precept restored = PreceptMaker.MakePrecept(previous.def);
            previous.CopyTo(restored);
            working.AddPrecept(restored);
            currentIds.Add(restored.Id);
            Diagnostics.Message($"Preserved precept {previous.def.defName} after meme cascade.");
        }
    }

    internal sealed class CascadeState
    {
        public ReformSession Session { get; }
        public Ideo Snapshot { get; }
        private bool scopeExited;

        public CascadeState(ReformSession session, Ideo snapshot)
        {
            Session = session;
            Snapshot = snapshot;
        }

        public void ExitScope()
        {
            if (scopeExited)
            {
                return;
            }

            scopeExited = true;
            MutationScope.ExitConsequences();
        }
    }
}
