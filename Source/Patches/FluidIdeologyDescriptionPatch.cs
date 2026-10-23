using RimWorld;
using Verse;

namespace IdeologyReformation.Patches;

internal static class FluidIdeologyDescriptionPatch
{
    public static void ApplyAfterLoading()
    {
        LongEventHandler.ExecuteWhenFinished(Apply);
    }

    private static void Apply()
    {
        IdeoPresetCategoryDef? fluid = DefDatabase<IdeoPresetCategoryDef>.GetNamedSilentFail("Fluid");
        if (fluid != null)
        {
            fluid.description = "IR_FluidModeDescription".Translate();
        }
    }
}
