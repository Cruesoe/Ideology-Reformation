using System;
using System.Collections.Generic;
using System.Linq;
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
            Rect resetRect = new Rect(
                inRect.x + (inRect.width - Window.CloseButSize.x) / 2f,
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

        // Take over RimWorld's existing centred Reset changes button. This restores
        // the complete opening snapshot, including precepts and cosmetic edits.
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
            // Style categories are presentation-only in this mod. Keep vanilla's mutual
            // exclusion for meme/structure changes. Once any object is selected, this also
            // activates RimWorld's existing grey overlays on the other mechanical choices.
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
            // Dialog_ReformIdeo uses this value only to decide whether to grey the styles
            // panel. Styles remain presentation-only even while a mechanical object is locked.
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(Widgets), nameof(Widgets.ButtonText),
    new[] { typeof(Rect), typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(TextAnchor?) })]
internal static class ReformEditorButtonPatch
{
    [HarmonyPrefix]
    private static bool Prefix(string label, ref bool active, ref bool __result, out ReformSession? __state)
    {
        __state = null;

        if (Find.WindowStack.currentlyDrawnWindow is not Dialog_ReformIdeo dialog
            || !ReformSessions.TryGet(dialog, out ReformSession session))
        {
            return true;
        }

        RefreshLabels();
        if (randomizeLabels.Contains(label))
        {
            // Randomizing memes or precepts wholesale is incompatible with the
            // one-deliberate-object rule at any lock state, so remove the button
            // outright instead of just disabling it.
            __result = false;
            return false;
        }

        if (session.SelectedObject != null && addPreceptLabels.Contains(label))
        {
            active = false;
            __state = session;
        }

        return true;
    }

    [HarmonyPostfix]
    private static void Postfix(Rect rect, ReformSession? __state)
    {
        // Widgets.ButtonText's "active" flag only blocks the click; it never dims the
        // button's own draw call, so a disabled button still looks pressable. Overlay
        // the same greyed-out treatment used elsewhere in this dialog (see
        // ReformChoiceVisualPatches) to make the locked state visible.
        if (__state == null || __state.SelectedObject == null)
        {
            return;
        }

        Widgets.DrawRectFast(rect, new Color(0.22f, 0.22f, 0.22f, 0.78f));
        TooltipHandler.TipRegion(rect, "IR_LockedMemeTip".Translate(__state.SelectedObject.Label));
    }

    private static readonly HashSet<string> randomizeLabels = new();
    private static readonly HashSet<string> addPreceptLabels = new();
    private static LoadedLanguage? labelsLanguage;

    // Rebuilds the translated button labels when the active language changes.
    private static void RefreshLabels()
    {
        if (labelsLanguage == LanguageDatabase.activeLanguage && randomizeLabels.Count > 0)
        {
            return;
        }

        labelsLanguage = LanguageDatabase.activeLanguage;
        randomizeLabels.Clear();
        randomizeLabels.Add("Randomize".Translate());
        randomizeLabels.Add("RandomizePrecepts".Translate());

        addPreceptLabels.Clear();
        foreach (string objectLabel in new[]
        {
            "Precept".Translate().ToString(),
            "Role".Translate().ToString(),
            "Ritual".Translate().ToString(),
            "IdeoBuilding".Translate().ToString(),
            "IdeoRelic".Translate().ToString(),
            "IdeoWeapon".Translate().ToString(),
            "Animal".Translate().ToString(),
            "Xenotype".Translate().ToString().UncapitalizeFirst(),
            "IdeoApparelDesire".Translate().ToString()
        })
        {
            addPreceptLabels.Add("AddPrecept".Translate(objectLabel).CapitalizeFirst() + "...");
        }
    }
}

/// <summary>
/// A precept float menu can remain open while another reform object becomes locked.
/// Stop its vanilla callback before it constructs a detached precept and attempts to
/// configure it, which is unsafe after Ideo.AddPrecept has rejected the mutation.
/// </summary>
[HarmonyPatch(typeof(FloatMenuOption), nameof(FloatMenuOption.Chosen))]
internal static class LockedPreceptMenuOptionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(FloatMenuOption __instance)
    {
        if (__instance.action == null || !IsIdeoAddPreceptAction(__instance.action.Method))
        {
            return true;
        }

        Dialog_ReformIdeo? dialog = Find.WindowStack.Windows.OfType<Dialog_ReformIdeo>().LastOrDefault();
        if (dialog == null
            || !ReformSessions.TryGet(dialog, out ReformSession session)
            || session.SelectedObject == null)
        {
            return true;
        }

        Messages.Message(
            "IR_AnotherObjectLocked".Translate(session.SelectedObject.Label),
            MessageTypeDefOf.RejectInput,
            historical: false);
        return false;
    }

    private static bool IsIdeoAddPreceptAction(MethodInfo method)
    {
        Type? type = method.DeclaringType;
        while (type != null && type != typeof(IdeoUIUtility))
        {
            type = type.DeclaringType;
        }

        return type == typeof(IdeoUIUtility) && method.Name.Contains("AddPrecept");
    }
}

[HarmonyPatch(typeof(Window), nameof(Window.PostClose))]
internal static class ReformWindowClosePatch
{
    [HarmonyPostfix]
    private static void Postfix(Window __instance)
    {
        if (__instance is Dialog_ReformIdeo reformDialog)
        {
            ReformSessions.End(reformDialog);
        }
    }
}
