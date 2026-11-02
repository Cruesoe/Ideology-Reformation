using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using IdeologyReformation.Reform;

namespace IdeologyReformation.Patches;

/// <summary>Once a reform object is chosen, dims every other precept box, makes it inert and explains why.</summary>
[HarmonyPatch]
internal static class ReformPreceptLockVisualPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        return typeof(Precept).Assembly.GetTypes()
            .Where(type => typeof(Precept).IsAssignableFrom(type))
            .Select(type => AccessTools.DeclaredMethod(
                type,
                nameof(Precept.DrawPreceptBox),
                new[] { typeof(Rect), typeof(IdeoEditMode), typeof(bool) }))
            .Where(method => method != null)
            .Cast<MethodBase>();
    }

    [HarmonyPrefix]
    private static void Prefix(
        Precept __instance,
        ref IdeoEditMode editMode,
        out ReformSession? __state)
    {
        __state = null;
        if (editMode != IdeoEditMode.Reform
            || !ReformSessions.TryGet(__instance.ideo, out ReformSession session)
            || session.SelectedObject == null
            || session.CanConfigureMemeConsequence(__instance)
            || session.SelectedObject.Equals(ReformObject.ForPrecept(__instance)))
        {
            return;
        }

        // Cosmetic edits to a precept are blocked too while another object holds the reform.
        editMode = IdeoEditMode.None;
        __state = session;
    }

    [HarmonyPostfix]
    private static void Postfix(Rect preceptBox, ReformSession? __state)
    {
        if (__state == null || __state.SelectedObject == null)
        {
            return;
        }

        Widgets.DrawRectFast(preceptBox, new Color(0.10f, 0.10f, 0.10f, 0.62f));
        TooltipHandler.TipRegion(preceptBox, "IR_LockedObjectTip".Translate(__state.SelectedObject.Label));
        if (Widgets.ButtonInvisible(preceptBox))
        {
            Messages.Message(
                "IR_AnotherObjectLocked".Translate(__state.SelectedObject.Label),
                MessageTypeDefOf.RejectInput,
                historical: false);
        }
    }
}
