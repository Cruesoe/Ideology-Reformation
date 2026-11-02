using System.Collections.Generic;
using RimWorld;

namespace IdeologyReformation.Reform;

internal static class ReformSessions
{
    private static readonly Dictionary<Ideo, ReformSession> ByWorkingIdeo = new();
    private static readonly Dictionary<Dialog_ReformIdeo, ReformSession> ByDialog = new();

    public static ReformSession Begin(Dialog_ReformIdeo dialog, Ideo original, Ideo working)
    {
        ReformSession session = new ReformSession(dialog, original, working);
        ByWorkingIdeo[working] = session;
        ByDialog[dialog] = session;
        Diagnostics.Message($"Opened reform session for {original.name}.");
        return session;
    }

    public static bool TryGet(Ideo working, out ReformSession session) => ByWorkingIdeo.TryGetValue(working, out session!);
    public static bool TryGet(Dialog_ReformIdeo dialog, out ReformSession session) => ByDialog.TryGetValue(dialog, out session!);

    public static void End(Dialog_ReformIdeo dialog)
    {
        if (!ByDialog.TryGetValue(dialog, out ReformSession? session))
        {
            return;
        }

        ByDialog.Remove(dialog);
        ByWorkingIdeo.Remove(session.Working);
        Diagnostics.Message($"Closed reform session for {session.Original.name}.");
    }
}
