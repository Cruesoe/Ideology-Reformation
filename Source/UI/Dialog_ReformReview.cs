using System;
using System.Collections.Generic;
using IdeologyReformation.Reform;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeologyReformation.UI;

internal sealed class Dialog_ReformReview : Window
{
    private const float SectionGap = 18f;
    private readonly Action confirm;
    private readonly ReformSummary summary;
    private Vector2 scrollPosition;

    public override Vector2 InitialSize => new Vector2(720f, Mathf.Min(720f, Verse.UI.screenHeight));

    public Dialog_ReformReview(ReformSession session, Action confirm)
    {
        this.confirm = confirm;
        summary = ReformSummary.Build(session);
        forcePause = true;
        absorbInputAroundWindow = true;
        doCloseX = false;
        closeOnCancel = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), "IR_ReviewTitle".Translate());
        Text.Font = GameFont.Small;

        Rect outRect = new Rect(inRect.x, inRect.y + 45f, inRect.width, inRect.height - 100f);
        float viewWidth = outRect.width - 16f;
        float contentHeight = CalculateContentHeight(viewWidth);
        Rect viewRect = new Rect(0f, 0f, viewWidth, Mathf.Max(outRect.height, contentHeight));

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        float curY = 0f;
        DrawSection(ref curY, viewWidth, "IR_PrimaryChange".Translate(),
            EmptyStateText(summary.PrimaryMemeCards, summary.HasMechanicalObject ? "None".Translate() : "IR_CosmeticOnly".Translate()),
            memeCards: summary.PrimaryMemeCards);
        DrawSection(ref curY, viewWidth, "IR_Consequences".Translate(),
            EmptyStateText(summary.PreceptCards, "IR_NoConsequences".Translate()),
            preceptCards: summary.PreceptCards,
            removedPreceptIds: summary.RemovedPreceptIds);
        if (summary.CosmeticChanges.Count > 0)
        {
            DrawSection(ref curY, viewWidth, "IR_CosmeticChanges".Translate(), summary.CosmeticText);
        }

        Widgets.EndScrollView();

        Rect backRect = new Rect(inRect.x, inRect.yMax - 40f, 180f, 40f);
        Rect confirmRect = new Rect(inRect.xMax - 180f, inRect.yMax - 40f, 180f, 40f);
        if (Widgets.ButtonText(backRect, "IR_GoBack".Translate()))
        {
            Close();
        }
        if (Widgets.ButtonText(confirmRect, "IR_ConfirmReform".Translate()))
        {
            Close();
            confirm();
        }
    }

    private float CalculateContentHeight(float width)
    {
        float height = SectionHeight(
                width,
                EmptyStateText(summary.PrimaryMemeCards, summary.HasMechanicalObject ? "None".Translate() : "IR_CosmeticOnly".Translate()),
                memeCards: summary.PrimaryMemeCards)
            + SectionHeight(
                width,
                EmptyStateText(summary.PreceptCards, "IR_NoConsequences".Translate()),
                preceptCards: summary.PreceptCards);
        if (summary.CosmeticChanges.Count > 0)
        {
            height += SectionHeight(width, summary.CosmeticText);
        }

        return height;
    }

    // Cards already show what the primary/consequence objects are; only fall back to
    // a text line when a section has nothing to show a card for.
    private static string EmptyStateText<T>(IReadOnlyCollection<T> cards, string emptyFallback)
    {
        return cards.Count == 0 ? emptyFallback : string.Empty;
    }

    private static float SectionHeight(
        float width,
        string body,
        IReadOnlyCollection<Precept>? preceptCards = null,
        IReadOnlyCollection<MemeDef>? memeCards = null)
    {
        Text.Font = GameFont.Medium;
        float headingHeight = Text.CalcHeight("Ag", width);
        Text.Font = GameFont.Small;
        float bodyHeight = body.NullOrEmpty() ? 0f : Text.CalcHeight(body, width);
        return headingHeight + 6f
            + PreceptCardsHeight(width, preceptCards)
            + MemeCardsHeight(width, memeCards)
            + bodyHeight + SectionGap;
    }

    private static void DrawSection(
        ref float curY,
        float width,
        string heading,
        string body,
        IReadOnlyList<Precept>? preceptCards = null,
        IReadOnlyList<MemeDef>? memeCards = null,
        HashSet<int>? removedPreceptIds = null)
    {
        Text.Font = GameFont.Medium;
        float headingHeight = Text.CalcHeight(heading, width);
        Widgets.Label(new Rect(0f, curY, width, headingHeight), heading);
        curY += headingHeight + 6f;
        DrawPreceptCards(ref curY, width, preceptCards, removedPreceptIds);
        DrawMemeCards(ref curY, width, memeCards);
        if (!body.NullOrEmpty())
        {
            DrawBody(ref curY, width, body);
        }
        curY += SectionGap;
    }

    private static float PreceptCardsHeight(float width, IReadOnlyCollection<Precept>? cards)
    {
        if (cards == null || cards.Count == 0)
        {
            return 0f;
        }

        int columns = Mathf.Max(1, Mathf.FloorToInt((width + 8f) / (IdeoUIUtility.PreceptBoxSize.x + 8f)));
        int rows = Mathf.CeilToInt((float)cards.Count / columns);
        return rows * IdeoUIUtility.PreceptBoxSize.y + (rows - 1) * 8f + 10f;
    }

    private static void DrawPreceptCards(ref float curY, float width, IReadOnlyList<Precept>? cards, HashSet<int>? removedIds)
    {
        if (cards == null || cards.Count == 0)
        {
            return;
        }

        Text.Font = GameFont.Small;
        int columns = Mathf.Max(1, Mathf.FloorToInt((width + 8f) / (IdeoUIUtility.PreceptBoxSize.x + 8f)));
        float rowWidth = Mathf.Min(columns, cards.Count) * IdeoUIUtility.PreceptBoxSize.x
            + (Mathf.Min(columns, cards.Count) - 1) * 8f;
        float startX = (width - rowWidth) / 2f;
        for (int i = 0; i < cards.Count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            Rect cardRect = new Rect(
                startX + column * (IdeoUIUtility.PreceptBoxSize.x + 8f),
                curY + row * (IdeoUIUtility.PreceptBoxSize.y + 8f),
                IdeoUIUtility.PreceptBoxSize.x,
                IdeoUIUtility.PreceptBoxSize.y);
            cards[i].DrawPreceptBox(cardRect, IdeoEditMode.None);
            if (removedIds != null && removedIds.Contains(cards[i].Id))
            {
                DrawRemovedOverlay(cardRect);
            }
        }

        curY += PreceptCardsHeight(width, cards);
    }

    // Dims a removed precept's card and labels it across the middle.
    private static void DrawRemovedOverlay(Rect cardRect)
    {
        Widgets.DrawRectFast(cardRect, new Color(0.10f, 0.10f, 0.10f, 0.55f));
        Text.Font = GameFont.Small;
        Rect bandRect = new Rect(cardRect.x, cardRect.center.y - Text.LineHeight / 2f, cardRect.width, Text.LineHeight);
        Widgets.DrawRectFast(bandRect, new Color(0.45f, 0.12f, 0.10f, 0.9f));
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(bandRect, "IR_PreceptRemoved".Translate());
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private static float MemeCardsHeight(float width, IReadOnlyCollection<MemeDef>? cards)
    {
        if (cards == null || cards.Count == 0)
        {
            return 0f;
        }

        int columns = Mathf.Max(1, Mathf.FloorToInt((width + 8f) / (IdeoUIUtility.MemeBoxSize.x + 8f)));
        int rows = Mathf.CeilToInt((float)cards.Count / columns);
        return rows * IdeoUIUtility.MemeBoxSize.y + (rows - 1) * 8f + 10f;
    }

    private static void DrawMemeCards(ref float curY, float width, IReadOnlyList<MemeDef>? cards)
    {
        if (cards == null || cards.Count == 0)
        {
            return;
        }

        Text.Font = GameFont.Small;
        int columns = Mathf.Max(1, Mathf.FloorToInt((width + 8f) / (IdeoUIUtility.MemeBoxSize.x + 8f)));
        float rowWidth = Mathf.Min(columns, cards.Count) * IdeoUIUtility.MemeBoxSize.x
            + (Mathf.Min(columns, cards.Count) - 1) * 8f;
        float startX = (width - rowWidth) / 2f;
        for (int i = 0; i < cards.Count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            Rect cardRect = new Rect(
                startX + column * (IdeoUIUtility.MemeBoxSize.x + 8f),
                curY + row * (IdeoUIUtility.MemeBoxSize.y + 8f),
                IdeoUIUtility.MemeBoxSize.x,
                IdeoUIUtility.MemeBoxSize.y);
            IdeoUIUtility.DoMeme(cardRect, cards[i]);
        }

        curY += MemeCardsHeight(width, cards);
    }

    private static void DrawBody(ref float curY, float width, string body)
    {
        Text.Font = GameFont.Small;
        float height = Text.CalcHeight(body, width);
        Widgets.Label(new Rect(0f, curY, width, height), body);
        curY += height;
    }
}
