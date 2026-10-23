using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using IdeologyReformation.Reform;

namespace IdeologyReformation.Patches;

[HarmonyPatch]
internal static class PreceptMutationPatches
{
    [HarmonyPatch(typeof(Ideo), nameof(Ideo.AddPrecept))]
    [HarmonyPrefix]
    private static bool AddPreceptPrefix(Ideo __instance, Precept precept, out bool __state)
    {
        return BeginMutation(__instance, precept, out __state);
    }

    [HarmonyPatch(typeof(Ideo), nameof(Ideo.AddPrecept))]
    [HarmonyPostfix]
    private static void AddPreceptPostfix(Ideo __instance, Precept precept, bool __state)
    {
        // A new ritual, relic or building is keyed by ID, which Init assigns during the add.
        if (__state && ReformSessions.TryGet(__instance, out ReformSession session))
        {
            session.RefreshSelection(precept);
        }

        EndMutation(__state);
    }

    [HarmonyPatch(typeof(Ideo), nameof(Ideo.AddPrecept))]
    [HarmonyFinalizer]
    private static System.Exception? AddPreceptFinalizer(System.Exception? __exception, bool __state)
    {
        EndMutation(__state);
        return __exception;
    }

    [HarmonyPatch(typeof(Ideo), nameof(Ideo.RemovePrecept))]
    [HarmonyPrefix]
    private static bool RemovePreceptPrefix(Ideo __instance, Precept precept, out bool __state)
    {
        return BeginMutation(__instance, precept, out __state);
    }

    [HarmonyPatch(typeof(Ideo), nameof(Ideo.RemovePrecept))]
    [HarmonyPostfix]
    private static void RemovePreceptPostfix(bool __state) => EndMutation(__state);

    [HarmonyPatch(typeof(Ideo), nameof(Ideo.RemovePrecept))]
    [HarmonyFinalizer]
    private static System.Exception? RemovePreceptFinalizer(System.Exception? __exception, bool __state)
    {
        EndMutation(__state);
        return __exception;
    }

    private static bool BeginMutation(Ideo ideo, Precept precept, out bool enteredScope)
    {
        enteredScope = false;
        if (MutationScope.IsResolvingConsequences || MutationScope.IsNestedPreceptMutation || !ReformSessions.TryGet(ideo, out ReformSession session))
        {
            return true;
        }

        if (session.CanConfigureMemeConsequence(precept))
        {
            MutationScope.EnterPreceptMutation();
            enteredScope = true;
            Diagnostics.Message($"Allowed configuration of meme consequence {precept.def.defName}.");
            return true;
        }

        if (!session.TrySelect(ReformObject.ForPrecept(precept)))
        {
            return false;
        }

        MutationScope.EnterPreceptMutation();
        enteredScope = true;
        return true;
    }

    private static void EndMutation(bool enteredScope)
    {
        if (enteredScope)
        {
            MutationScope.ExitPreceptMutation();
        }
    }
}

[HarmonyPatch(typeof(Dialog_EditPrecept), "ApplyChanges")]
internal static class DetailedPreceptEditPatch
{
    private static readonly FieldInfo PreceptField = ReflectionSupport.RequireField(typeof(Dialog_EditPrecept), "precept");
    private static readonly FieldInfo NewAnytimeField = ReflectionSupport.RequireField(typeof(Dialog_EditPrecept), "newCanStartAnytime");
    private static readonly FieldInfo NewDateField = ReflectionSupport.RequireField(typeof(Dialog_EditPrecept), "newTriggerDaysSinceStartOfYear");
    private static readonly FieldInfo RewardField = ReflectionSupport.RequireField(typeof(Dialog_EditPrecept), "selectedReward");
    private static readonly FieldInfo ApparelField = ReflectionSupport.RequireField(typeof(Dialog_EditPrecept), "apparelRequirements");

    [HarmonyPrefix]
    private static bool Prefix(Dialog_EditPrecept __instance)
    {
        Precept precept = (Precept)PreceptField.GetValue(__instance)!;
        if (!ReformSessions.TryGet(precept.ideo, out ReformSession session) || !HasMechanicalChanges(__instance, precept, session))
        {
            return true;
        }

        ReformObject reformObject = ReformObject.ForPrecept(precept);
        if (session.TrySelect(reformObject))
        {
            return true;
        }

        RestoreDirectRelicMutation(precept, session);
        return false;
    }

    private static bool HasMechanicalChanges(Dialog_EditPrecept dialog, Precept precept, ReformSession session)
    {
        if (precept is Precept_Ritual ritual)
        {
            bool newAnytime = (bool)NewAnytimeField.GetValue(dialog)!;
            int newDate = (int)NewDateField.GetValue(dialog)!;
            RitualAttachableOutcomeEffectDef? reward = (RitualAttachableOutcomeEffectDef?)RewardField.GetValue(dialog);
            RitualObligationTrigger_Date? trigger = ritual.obligationTriggers.OfType<RitualObligationTrigger_Date>().FirstOrDefault();
            if (newAnytime != ritual.isAnytime || reward != ritual.attachableOutcomeEffect || (trigger != null && newDate != trigger.triggerDaysSinceStartOfYear))
            {
                return true;
            }
        }

        List<PreceptApparelRequirement>? edited = (List<PreceptApparelRequirement>?)ApparelField.GetValue(dialog);
        if (edited != null && !(precept.ApparelRequirements ?? Enumerable.Empty<PreceptApparelRequirement>()).SequenceEqual(edited))
        {
            return true;
        }

        if (precept is Precept_Relic relic)
        {
            Precept_Relic? original = session.Original.PreceptsListForReading.OfType<Precept_Relic>().FirstOrDefault(p => p.Id == relic.Id);
            return original != null && original.stuff != relic.stuff;
        }

        return false;
    }

    private static void RestoreDirectRelicMutation(Precept precept, ReformSession session)
    {
        if (precept is not Precept_Relic relic)
        {
            return;
        }

        Precept_Relic? original = session.Original.PreceptsListForReading.OfType<Precept_Relic>().FirstOrDefault(p => p.Id == relic.Id);
        if (original != null)
        {
            relic.stuff = original.stuff;
        }
    }
}
