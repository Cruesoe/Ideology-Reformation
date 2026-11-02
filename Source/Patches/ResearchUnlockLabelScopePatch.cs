using System;
using HarmonyLib;
using RimWorld;

namespace IdeologyReformation.Patches;

/// <summary>Marks when the research panel is drawing its unlock links, so only those meme links get a type prefix.</summary>
[HarmonyPatch(typeof(MainTabWindow_Research), "DrawUnlockableHyperlinks")]
internal static class ResearchUnlockLabelScopePatch
{
    internal static bool IsDrawing { get; private set; }

    [HarmonyPrefix]
    private static void Prefix(out bool __state)
    {
        __state = IsDrawing;
        IsDrawing = true;
    }

    [HarmonyPostfix]
    private static void Postfix(bool __state)
    {
        IsDrawing = __state;
    }

    [HarmonyFinalizer]
    private static Exception? Finalizer(Exception? __exception, bool __state)
    {
        IsDrawing = __state;
        return __exception;
    }
}
