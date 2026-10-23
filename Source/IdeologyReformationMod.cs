using IdeologyReformation.Patches;
using HarmonyLib;
using Verse;

namespace IdeologyReformation;

public sealed class IdeologyReformationMod : Mod
{
    public const string HarmonyId = "cruesoe.ideologyreformation";

    public IdeologyReformationMod(ModContentPack content) : base(content)
    {
        new Harmony(HarmonyId).PatchAll();
        ReflectionSupport.EnsureFieldLookupsResolved();
        FluidIdeologyDescriptionPatch.ApplyAfterLoading();
#if DEBUG
        Log.Message("[Ideology Reformation] Phase 1 loaded.");
#endif
    }
}
