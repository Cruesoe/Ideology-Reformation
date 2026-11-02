namespace IdeologyReformation.Reform;

/// <summary>Tracks nested precept changes and meme consequences so they aren't counted as a second reform object.</summary>
internal static class MutationScope
{
    [System.ThreadStatic]
    private static int consequenceDepth;

    [System.ThreadStatic]
    private static int preceptMutationDepth;

    public static bool IsResolvingConsequences => consequenceDepth > 0;
    public static bool IsNestedPreceptMutation => preceptMutationDepth > 0;
    public static void EnterConsequences() => consequenceDepth++;
    public static void ExitConsequences() => consequenceDepth = System.Math.Max(0, consequenceDepth - 1);
    public static void EnterPreceptMutation() => preceptMutationDepth++;
    public static void ExitPreceptMutation() => preceptMutationDepth = System.Math.Max(0, preceptMutationDepth - 1);
}
