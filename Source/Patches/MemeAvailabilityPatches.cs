using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using IdeologyReformation.Restrictions;

namespace IdeologyReformation.Patches;

/// <summary>
/// Draws memes gated by <see cref="MemeAvailabilityExtension"/> as locked in the meme picker
/// (initial ideo creation, fluid ideo setup, and reform): dimmed, inert to clicks, and carrying a
/// tooltip naming the requirement. The tile stays in the grid so the requirement is discoverable
/// rather than the meme silently not existing.
/// </summary>
[HarmonyPatch(typeof(Dialog_ChooseMemes), "DrawMeme")]
internal static class MemeLockVisualPatch
{
    private static readonly AccessTools.FieldRef<Dialog_ChooseMemes, Ideo> IdeoRef =
        AccessTools.FieldRefAccess<Dialog_ChooseMemes, Ideo>("ideo");

    [HarmonyPrefix]
    private static void Prefix(Dialog_ChooseMemes __instance, MemeDef meme, Rect memeBox, out string? __state)
    {
        // A meme the ideoligion already has stays selectable so it can be removed.
        __state = IdeoRef(__instance)?.memes.Contains(meme) == true ? null : MemeAvailabilityUtility.LockedReason(meme);
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
}

/// <summary>
/// Applies the same gate to <see cref="IdeoUtility"/>'s random meme generation for the player's
/// ideoligion (starting scenarios and "Randomize"). Other factions' ideoligions are not gated.
/// </summary>
[HarmonyPatch(typeof(IdeoUtility), "CanAdd")]
internal static class MemeAvailabilityRandomizationPatch
{
    [HarmonyPostfix]
    private static void Postfix(MemeDef meme, FactionDef? forFaction, ref bool __result)
    {
        if (__result && (forFaction == null || forFaction.isPlayer) && !MemeAvailabilityUtility.IsMemeAvailable(meme))
        {
            __result = false;
        }
    }
}
