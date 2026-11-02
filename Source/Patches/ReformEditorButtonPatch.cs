using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using IdeologyReformation.Reform;

namespace IdeologyReformation.Patches;

/// <summary>In the reform dialog, hides the Randomize buttons and disables the Add precept buttons once a reform object is chosen.</summary>
[HarmonyPatch(typeof(Widgets), nameof(Widgets.ButtonText),
    new[] { typeof(Rect), typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(TextAnchor?) })]
internal static class ReformEditorButtonPatch
{
    private static readonly HashSet<string> randomizeLabels = new();
    private static readonly HashSet<string> addPreceptLabels = new();
    private static LoadedLanguage? labelsLanguage;

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
        // An inactive button still draws as pressable, so dim it.
        if (__state == null || __state.SelectedObject == null)
        {
            return;
        }

        Widgets.DrawRectFast(rect, new Color(0.22f, 0.22f, 0.22f, 0.78f));
        TooltipHandler.TipRegion(rect, "IR_LockedObjectTip".Translate(__state.SelectedObject.Label));
    }

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
