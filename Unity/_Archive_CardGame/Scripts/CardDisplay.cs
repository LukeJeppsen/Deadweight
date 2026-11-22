using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [Header("UI References")]
    public Image cardImage;        // The main sprite (from LibreSprite)
    public Image cardBack;         // The "Obsidian" card back sprite

    private CardData data;

    // This is the function the Hand script calls immediately
    public void Setup(CardData cardData, bool faceUp = true)
    {
        data = cardData;

        // Assign the visuals from the ScriptableObject
        if (cardImage != null)
            cardImage.sprite = data.cardVisual;

        SetFaceUp(faceUp);
    }

    // Use this to reveal the dealer's hidden cards
    public void SetFaceUp(bool faceUp)
    {
        if (faceUp)
        {
            cardImage.enabled = true;
            cardBack.enabled = false;
        }
        else
        {
            cardImage.enabled = false;
            cardBack.enabled = true;
        }
    }
}