using HarmonyLib;
using RimWorld;
using IdeologyReformation.Reform;

namespace IdeologyReformation.Patches;

/// <summary>Counts each precept added or removed during a reform as the reform's object, rejecting a second one.</summary>
[HarmonyPatch]
internal static class PreceptMutationPatches
{
    [HarmonyPatch(typeof(Ideo), nameof(Ideo.AddPrecept))]
    [HarmonyPrefix]
    private static bool AddPreceptPrefix(Ideo __instance, Precept precept, out bool __state)
    {
        return BeginMutation(__instance, precept, out __state);
    }

    [HarmonyPatch(typeof(Ideo), nameof(Ideo.AddPrecept))]
    [HarmonyPostfix]
    private static void AddPreceptPostfix(Ideo __instance, Precept precept, bool __state)
    {
        // A new ritual, relic or building is keyed by ID, which Init assigns during the add.
        if (__state && ReformSessions.TryGet(__instance, out ReformSession session))
        {
            session.RefreshSelection(precept);
        }

        EndMutation(__state);
    }

    [HarmonyPatch(typeof(Ideo), nameof(Ideo.AddPrecept))]
    [HarmonyFinalizer]
    private static System.Exception? AddPreceptFinalizer(System.Exception? __exception, bool __state)
    {
        EndMutation(__state);
        return __exception;
    }

    [HarmonyPatch(typeof(Ideo), nameof(Ideo.RemovePrecept))]
    [HarmonyPrefix]
    private static bool RemovePreceptPrefix(Ideo __instance, Precept precept, out bool __state)
    {
        return BeginMutation(__instance, precept, out __state);
    }

    [HarmonyPatch(typeof(Ideo), nameof(Ideo.RemovePrecept))]
    [HarmonyPostfix]
    private static void RemovePreceptPostfix(bool __state) => EndMutation(__state);

    [HarmonyPatch(typeof(Ideo), nameof(Ideo.RemovePrecept))]
    [HarmonyFinalizer]
    private static System.Exception? RemovePreceptFinalizer(System.Exception? __exception, bool __state)
    {
        EndMutation(__state);
        return __exception;
    }

    private static bool BeginMutation(Ideo ideo, Precept precept, out bool enteredScope)
    {
        enteredScope = false;
        if (MutationScope.IsResolvingConsequences || MutationScope.IsNestedPreceptMutation || !ReformSessions.TryGet(ideo, out ReformSession session))
        {
            return true;
        }

        if (session.CanConfigureMemeConsequence(precept))
        {
            MutationScope.EnterPreceptMutation();
            enteredScope = true;
            Diagnostics.Message($"Allowed configuration of meme consequence {precept.def.defName}.");
            return true;
        }

        if (!session.TrySelect(ReformObject.ForPrecept(precept)))
        {
            return false;
        }

        MutationScope.EnterPreceptMutation();
        enteredScope = true;
        return true;
    }

    private static void EndMutation(bool enteredScope)
    {
        if (enteredScope)
        {
            MutationScope.ExitPreceptMutation();
        }
    }
}
