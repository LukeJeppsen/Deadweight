using System.Collections.Generic;
using UnityEngine;

public class Deck : ScriptableObject
{
    public List<CardData> allCards; // The permanent master list (52 cards)
    private List<CardData> drawPile = new List<CardData>(); // The actual pile we draw from

    public void InitializeDeck()
    {
        drawPile = new List<CardData>(allCards); // Copy master to working list
        Shuffle();
    }

    public void Shuffle()
    {
        // Simple Fisher-Yates shuffle logic
        for (int i = 0; i < drawPile.Count; i++)
        {
            CardData temp = drawPile[i];
            int randomIndex = Random.Range(i, drawPile.Count);
            drawPile[i] = drawPile[randomIndex];
            drawPile[randomIndex] = temp;
        }
    }

    public CardData DrawCard()
    {
        // If the list itself is null or empty, force an initialization
        if (drawPile == null || drawPile.Count == 0)
        {
            InitializeDeck();
        }

        CardData card = drawPile[0];
        drawPile.RemoveAt(0);
        return card;
    }

    public float GetPercentRemaining()
    {
        if (allCards.Count == 0) return 0;
        return (float)drawPile.Count / allCards.Count;
    }
}