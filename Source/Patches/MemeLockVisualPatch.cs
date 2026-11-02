using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using IdeologyReformation.Restrictions;

namespace IdeologyReformation.Patches;

/// <summary>Draws research-locked memes in the meme picker dimmed, inert and with a tooltip naming the requirement.</summary>
[HarmonyPatch(typeof(Dialog_ChooseMemes), "DrawMeme")]
internal static class MemeLockVisualPatch
{
    private static readonly AccessTools.FieldRef<Dialog_ChooseMemes, Ideo> IdeoRef =
        AccessTools.FieldRefAccess<Dialog_ChooseMemes, Ideo>("ideo");

    // Lock reasons for the open picker; research can't change while the modal dialog is open.
    private static readonly Dictionary<MemeDef, string?> lockReasons = new();
    private static Dialog_ChooseMemes? lockReasonsDialog;

    [HarmonyPrefix]
    private static void Prefix(Dialog_ChooseMemes __instance, MemeDef meme, Rect memeBox, out string? __state)
    {
        // A meme the ideoligion already has stays selectable so it can be removed.
        __state = IdeoRef(__instance)?.memes.Contains(meme) == true ? null : LockedReason(__instance, meme);
        if (__state == null)
        {
            return;
        }

        // Consume the press before vanilla's own ButtonInvisible can select the meme.
        if (Event.current.type == EventType.MouseDown && Mouse.IsOver(memeBox))
        {
            Messages.Message(__state, MessageTypeDefOf.RejectInput, historical: false);
            Event.current.Use();
        }
    }

    [HarmonyPostfix]
    private static void Postfix(Rect memeBox, string? __state)
    {
        if (__state == null)
        {
            return;
        }

        Widgets.DrawRectFast(memeBox, new Color(0.10f, 0.10f, 0.10f, 0.62f));
        TooltipHandler.TipRegion(memeBox, __state);
    }

    private static string? LockedReason(Dialog_ChooseMemes dialog, MemeDef meme)
    {
        if (lockReasonsDialog != dialog)
        {
            lockReasons.Clear();
            lockReasonsDialog = dialog;
        }

        if (!lockReasons.TryGetValue(meme, out string? reason))
        {
            reason = MemeAvailabilityUtility.LockedReason(meme);
            lockReasons[meme] = reason;
        }

        return reason;
    }
}
