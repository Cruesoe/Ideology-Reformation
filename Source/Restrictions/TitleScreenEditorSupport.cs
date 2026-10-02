using System.Reflection;
using HarmonyLib;
using Verse;

namespace IdeologyReformation.Restrictions;

/// <summary>
/// Detects the throwaway game that Xenotype And Ideology Buttons TitleScreen creates for its
/// main-menu ideoligion editor. That game has no research, so meme gates are skipped there.
/// </summary>
internal static class TitleScreenEditorSupport
{
    private static readonly FieldInfo? PreviewGameField = FindPreviewGameField();

    public static bool IsEditingFromTitleScreen()
    {
        return PreviewGameField != null
            && Current.ProgramState == ProgramState.Entry
            && Current.Game != null
            && PreviewGameField.GetValue(null) == Current.Game;
    }

    private static FieldInfo? FindPreviewGameField()
    {
        System.Type? type = GenTypes.GetTypeInAnyAssembly("XenotypeAndIdeologyButtonsTitleScreen.MainPatches");
        return type == null ? null : AccessTools.Field(type, "_previewGame");
    }
}
