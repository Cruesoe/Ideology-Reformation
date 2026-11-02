using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using IdeologyReformation.Reform;

namespace IdeologyReformation.Patches;

[HarmonyPatch(typeof(Dialog_ReformIdeo))]
internal static class ReformDialogPatches
{
    private static readonly FieldInfo WorkingIdeoField = ReflectionSupport.RequireField(typeof(Dialog_ReformIdeo), "newIdeo");
    private static readonly FieldInfo StageField = ReflectionSupport.RequireField(typeof(Dialog_ReformIdeo), "stage");

    // Vanilla moves its centre button this far left while the incompatible-precepts warning shows.
    private const float IncompatibleWarningShift = 65f;

    [HarmonyPatch(MethodType.Constructor, typeof(Ideo))]
    [HarmonyPostfix]
    private static void ConstructorPostfix(Dialog_ReformIdeo __instance, Ideo ideo)
    {
        if (!ideo.Fluid)
        {
            return;
        }

        Ideo working = (Ideo)WorkingIdeoField.GetValue(__instance)!;
        ReformSessions.Begin(__instance, ideo, working);
    }

    [HarmonyPatch(nameof(Dialog_ReformIdeo.DoWindowContents))]
    [HarmonyPostfix]
    private static void DrawPostfix(Dialog_ReformIdeo __instance, Rect inRect)
    {
        if (!ReformSessions.TryGet(__instance, out ReformSession session))
        {
            return;
        }

        // Below vanilla's description label, which starts at y + 40.
        Text.Font = GameFont.Small;
        float statusY = inRect.y + 40f + Text.CalcHeight("ReformIdeoligionDesc".Translate(), inRect.width);
        GUI.color = Color.gray;
        string status = session.SelectedObject == null
            ? "IR_NoObjectSelected".Translate()
            : "IR_SelectedObject".Translate(session.SelectedObject.Label);
        Widgets.Label(new Rect(inRect.x, statusY, inRect.width, Text.LineHeight), status);
        GUI.color = Color.white;

        if ((IdeoReformStage)StageField.GetValue(__instance)! == IdeoReformStage.PreceptsNarrativeAndDeities)
        {
            float shift = session.Working.FirstIncompatiblePreceptPair() != default ? IncompatibleWarningShift : 0f;
            Rect resetRect = new Rect(
                inRect.x + (inRect.width - Window.CloseButSize.x) / 2f - shift,
                inRect.height - Window.CloseButSize.y,
                Window.CloseButSize.x,
                Window.CloseButSize.y);
            if (Widgets.ButtonText(resetRect, "ReformIdeoResetChanges".Translate()))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                session.Reset();
                Messages.Message("IR_ResetComplete".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
            }
        }
    }

    [HarmonyPatch("ResetAllChooseOneChanges")]
    [HarmonyPrefix]
    private static bool ResetChangesPrefix(Dialog_ReformIdeo __instance)
    {
        if (!ReformSessions.TryGet(__instance, out ReformSession session))
        {
            return true;
        }

        // Vanilla's Reset changes button restores the whole opening snapshot, including precepts and cosmetic edits.
        session.Reset();
        Messages.Message("IR_ResetComplete".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
        return false;
    }

    [HarmonyPatch(nameof(Dialog_ReformIdeo.AnyChooseOneChanges), MethodType.Getter)]
    [HarmonyPostfix]
    private static void MechanicalChoicePostfix(Dialog_ReformIdeo __instance, ref bool __result)
    {
        if (ReformSessions.TryGet(__instance, out ReformSession session))
        {
            // Styles don't count as a change; a selected object greys the other mechanical choices.
            __result = session.SelectedObject != null
                || __instance.StructureMemeChanged
                || __instance.NormalMemesChanged;
        }
    }

    [HarmonyPatch(nameof(Dialog_ReformIdeo.StylesChanged), MethodType.Getter)]
    [HarmonyPostfix]
    private static void KeepCosmeticStylesAvailable(Dialog_ReformIdeo __instance, ref bool __result)
    {
        if (ReformSessions.TryGet(__instance, out ReformSession session)
            && session.SelectedObject != null)
        {
            // The dialog reads this only to grey the styles panel, so styles stay editable.
            __result = true;
        }
    }
}
