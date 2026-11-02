using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using IdeologyReformation.Reform;

namespace IdeologyReformation.Patches;

/// <summary>Counts mechanical edits in the precept edit dialog (ritual timing and reward, apparel, relic material) as the reform's object.</summary>
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

    // The dialog sets a relic's material directly, so undo it when the edit is rejected.
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
