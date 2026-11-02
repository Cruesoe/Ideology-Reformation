using HarmonyLib;
using RimWorld;
using Verse;
using IdeologyReformation.Reform;

namespace IdeologyReformation.Patches;

/// <summary>Ends the reform session when its dialog closes.</summary>
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
