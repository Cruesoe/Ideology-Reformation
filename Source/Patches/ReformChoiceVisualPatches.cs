using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using IdeologyReformation.Reform;

namespace IdeologyReformation.Patches;

/// <summary>
/// Vanilla greys whole meme sections when another category is selected, but keeps
/// the normal-meme row active after a meme change. Dim every tile except the meme
/// that owns the current reform lock so a second choice no longer looks available.
/// </summary>
[HarmonyPatch(typeof(IdeoUIUtility), nameof(IdeoUIUtility.DoMeme))]
internal static class ReformMemeLockVisualPatch
{
    [HarmonyPrefix]
    private static void Prefix(
        MemeDef meme,
        Ideo? ideo,
        ref IdeoEditMode editMode,
        out ReformSession? __state)
    {
        __state = null;
        if (editMode != IdeoEditMode.Reform
            || ideo == null
            || meme.category != MemeCategory.Normal
            || !ReformSessions.TryGet(ideo, out ReformSession session)
            || session.SelectedObject == null
            || session.SelectedObject.Kind != ReformObjectKind.Meme
            || session.SelectedObject.Key == $"meme:{meme.defName}")
        {
            return;
        }

        editMode = IdeoEditMode.None;
        __state = session;
    }

    [HarmonyPostfix]
    private static void Postfix(Rect memeBox, ReformSession? __state)
    {
        if (__state == null || __state.SelectedObject == null)
        {
            return;
        }

        Widgets.DrawRectFast(memeBox, new Color(0.22f, 0.22f, 0.22f, 0.78f));
        TooltipHandler.TipRegion(
            memeBox,
            "IR_LockedMemeTip".Translate(__state.SelectedObject.Label));

        if (Widgets.ButtonInvisible(memeBox))
        {
            Messages.Message(
                "IR_AnotherObjectLocked".Translate(__state.SelectedObject.Label),
                MessageTypeDefOf.RejectInput,
                historical: false);
        }
    }
}

/// <summary>
/// Makes the one-object rule visible in the precept list. RimWorld already draws
/// disabled overlays over the meme and structure panels through AnyChooseOneChanges;
/// precepts need equivalent treatment because their boxes are drawn independently.
/// </summary>
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

        // Suppress every interaction with a different precept. Cosmetic changes are
        // free only while their owning object is not locked by another reform choice.
        editMode = IdeoEditMode.None;
        __state = session;
    }

    [HarmonyPostfix]
    private static void Postfix(Precept __instance, Rect preceptBox, ReformSession? __state)
    {
        if (__state == null || __state.SelectedObject == null)
        {
            return;
        }

        Widgets.DrawRectFast(preceptBox, new Color(0.10f, 0.10f, 0.10f, 0.62f));

    }
}
