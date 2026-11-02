namespace IdeologyReformation.Reform;

/// <summary>Debug-build logging; calls are compiled out of Release builds.</summary>
internal static class Diagnostics
{
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Message(string message)
    {
        Verse.Log.Message("[Ideology Reformation] " + message);
    }
}
