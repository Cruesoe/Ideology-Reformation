using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using IdeologyReformation.Reform;

namespace IdeologyReformation.Patches;

/// <summary>Once a meme change is chosen, dims every other normal meme in the reform dialog and makes it inert.</summary>
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
