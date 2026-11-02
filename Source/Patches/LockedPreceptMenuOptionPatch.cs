using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using IdeologyReformation.Reform;

namespace IdeologyReformation.Patches;

/// <summary>Blocks an Add precept menu option left open after another reform object was locked in.</summary>
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
