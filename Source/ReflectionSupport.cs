using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using IdeologyReformation.Patches;
using IdeologyReformation.Restrictions;
using Verse;

namespace IdeologyReformation;

/// <summary>Looks up RimWorld's private fields, logging a clear error at load when one is missing.</summary>
internal static class ReflectionSupport
{
    public static FieldInfo RequireField(Type type, string fieldName)
    {
        FieldInfo? field = AccessTools.Field(type, fieldName);
        if (field == null)
        {
            Log.Error($"[Ideology Reformation] Expected field '{type.FullName}.{fieldName}' was not found. " +
                "A RimWorld update likely renamed or removed it; patches depending on it will fail.");
        }

        return field!;
    }

    // Runs the field lookups at load so a missing field is reported at startup.
    public static void EnsureFieldLookupsResolved()
    {
        RuntimeHelpers.RunClassConstructor(typeof(MemePreservationPatches).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(DetailedPreceptEditPatch).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(ReformDialogPatches).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(StartingResearch).TypeHandle);
    }
}
