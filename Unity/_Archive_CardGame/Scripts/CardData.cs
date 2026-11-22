using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Card Game/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    public Sprite cardVisual; // card sprite goes here
    public int value;         // 0 for ace, 1 for 2, etc. up to 12 for king
    public string rankName;    // "A", "2", ..., "10", "J", "Q", "K"
    public Suit suit;
}

public enum Suit { Spades, Clubs, Hearts, Diamonds }