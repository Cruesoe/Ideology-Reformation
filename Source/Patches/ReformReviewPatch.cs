using System;
using HarmonyLib;
using RimWorld;
using Verse;
using IdeologyReformation.Reform;
using IdeologyReformation.UI;

namespace IdeologyReformation.Patches;

[HarmonyPatch(typeof(IdeoDevelopmentUtility), nameof(IdeoDevelopmentUtility.ConfirmChangesToIdeo))]
internal static class ReformReviewPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Ideo ideo, Ideo newIdeo, Action confirmCallback)
    {
        if (!ReformSessions.TryGet(newIdeo, out ReformSession session))
        {
            return true;
        }

        if (session.ReviewApproved)
        {
            // Approval skips the review once; a cancelled vanilla warning gets a fresh review next time.
            session.ReviewApproved = false;
            return true;
        }

        Find.WindowStack.Add(new Dialog_ReformReview(session, delegate
        {
            session.ReviewApproved = true;
            IdeoDevelopmentUtility.ConfirmChangesToIdeo(ideo, newIdeo, delegate
            {
                if (!session.TryBeginCommit())
                {
                    return;
                }

                confirmCallback();
            });
        }));
        return false;
    }
}
