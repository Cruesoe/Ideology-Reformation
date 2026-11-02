using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeologyReformation.Patches;

/// <summary>Prefixes meme links in the research panel's unlock list with "Meme:".</summary>
[HarmonyPatch(typeof(Dialog_InfoCard.Hyperlink), nameof(Dialog_InfoCard.Hyperlink.Label), MethodType.Getter)]
internal static class ResearchMemeUnlockLabelPatch
{
    [HarmonyPostfix]
    private static void Postfix(Dialog_InfoCard.Hyperlink __instance, ref string __result)
    {
        if (ResearchUnlockLabelScopePatch.IsDrawing && __instance.def is MemeDef)
        {
            __result = "IR_ResearchUnlockMeme".Translate(__result);
        }
    }
}
