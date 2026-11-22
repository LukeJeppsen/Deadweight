using UnityEngine;
using UnityEditor;
using System.IO;

public class CardAutoGenerator : EditorWindow
{
    // These strings must match your Enum in CardData
    private static string[] suitNames = { "Spades", "Clubs", "Diamonds", "Hearts" };
    private static string[] rankNames = { "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King" };

    [MenuItem("Tools/Generate Deck")]
    public static void GenerateDeck()
    {
        // 1. Path to your sliced texture
        string path = "Assets/Sprites/PlayingCardsSpriteSheet.ase";
        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(path);

        if (sprites.Length <= 1)
        {
            Debug.LogError("No sprites found! Did you slice the sheet in the Sprite Editor?");
            return;
        }

        // Create a folder to save the cards
        if (!AssetDatabase.IsValidFolder("Assets/Cards"))
            AssetDatabase.CreateFolder("Assets", "Cards");

        int cardCount = 0;

        // 2. Loop through the sprites (Index 0 is usually the texture itself, so we skip it)
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] is Sprite sprite)
            {
                // Create a new instance of your CardData ScriptableObject
                CardData asset = ScriptableObject.CreateInstance<CardData>();

                // 3. Logic to determine Suit and Value based on index
                // Row (Suit) is index / 13 | Column (Value) is index % 13
                int suitIndex = cardCount / 13;
                int rankIndex = cardCount % 13;

                asset.cardVisual = sprite;
                asset.value = rankIndex + 1; // Ace=1, 2=2...
                asset.suit = (Suit)suitIndex;
                asset.cardName = rankNames[rankIndex] + " of " + suitNames[suitIndex];

                // 4. Save the asset to your project
                string assetPath = $"Assets/Cards/{suitNames[suitIndex]}_{rankIndex + 1}.asset";
                AssetDatabase.CreateAsset(asset, assetPath);

                cardCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Generated {cardCount} cards successfully!");
    }
}