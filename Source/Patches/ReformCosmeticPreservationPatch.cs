using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using IdeologyReformation.Reform;

namespace IdeologyReformation.Patches;

/// <summary>
/// The reform dialog copies the original ideoligion over the working one before opening the meme picker.
/// Keeps the cosmetic edits made so far through that copy; the mod's own Reset still discards them.
/// </summary>
[HarmonyPatch(typeof(Ideo), nameof(Ideo.CopyTo))]
internal static class ReformCosmeticPreservationPatch
{
    private static readonly AccessTools.FieldRef<Precept, string> PreceptNameRef = AccessTools.FieldRefAccess<Precept, string>("name");
    private static readonly AccessTools.FieldRef<Ideo, string> WorshipRoomLabelRef = AccessTools.FieldRefAccess<Ideo, string>("overriddenWorshipRoomLabel");

    [HarmonyPrefix]
    private static void Prefix(Ideo __instance, Ideo ideo, out Ideo? __state)
    {
        __state = null;
        if (!ReformSessions.TryGet(ideo, out ReformSession session) || __instance != session.Original || session.IsResetting)
        {
            return;
        }

        __state = IdeoGenerator.MakeIdeo(ideo.foundation.def);
        session.Working.CopyTo(__state);
    }

    [HarmonyPostfix]
    private static void Postfix(Ideo ideo, Ideo? __state)
    {
        if (__state == null)
        {
            return;
        }

        __state.foundation.CopyTo(ideo.foundation);
        ideo.foundation.ideo = ideo;
        ideo.name = __state.name;
        ideo.adjective = __state.adjective;
        ideo.memberName = __state.memberName;
        ideo.iconDef = __state.iconDef;
        ideo.colorDef = __state.colorDef;
        ideo.primaryFactionColor = __state.primaryFactionColor;
        ideo.leaderTitleMale = __state.leaderTitleMale;
        ideo.leaderTitleFemale = __state.leaderTitleFemale;
        ideo.usedSymbolPacks.Clear();
        ideo.usedSymbolPacks.AddRange(__state.usedSymbolPacks);
        ideo.thingStyleCategories.Clear();
        ideo.thingStyleCategories.AddRange(__state.thingStyleCategories);
        __state.style.CopyTo(ideo.style);
        ideo.description = __state.description;
        ideo.descriptionTemplate = __state.descriptionTemplate;
        ideo.descriptionLocked = __state.descriptionLocked;
        WorshipRoomLabelRef(ideo) = WorshipRoomLabelRef(__state);

        // Precept names, matched by ID.
        Dictionary<int, Precept> edited = __state.PreceptsListForReading.ToDictionary(p => p.Id);
        foreach (Precept precept in ideo.PreceptsListForReading)
        {
            if (edited.TryGetValue(precept.Id, out Precept? before) && PreceptNameRef(before) != PreceptNameRef(precept))
            {
                precept.SetName(PreceptNameRef(before));
                precept.usesDefiniteArticle = before.usesDefiniteArticle;
                precept.nameLocked = before.nameLocked;
            }
        }
    }
}
