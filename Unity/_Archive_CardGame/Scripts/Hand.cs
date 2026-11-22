using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    public List<CardData> hand = new List<CardData>();
    public Transform cardSpawnPoint; // Point where cards will be displayed
    public GameObject cardPrefab; // Prefab for displaying cards
    public int currentTotal;

    public void AddCard(CardData data)
    {
        if (data == null)
        {
            Debug.LogError("AddCard received null data! Deck might be empty.");
            return;
        }

        hand.Add(data);

        // Instantiate as a child of the spawn point
        GameObject newCard = Instantiate(cardPrefab, cardSpawnPoint);

        // Standard UI reset to ensure the Layout Group can take control
        newCard.transform.localScale = Vector3.one;
        newCard.transform.localPosition = Vector3.zero;

        CardDisplay display = newCard.GetComponent<CardDisplay>();
        if (display != null)
        {
            display.Setup(data);
        }
    }

    public void CalculateScore()
    {
        int total = 0;
        int aceCount = 0;

        foreach (CardData card in hand)
        {
            if (card.value >= 10) total += 10;
            else if (card.value == 1)
            {
                aceCount++;
                total += 11;
            }
            else total += card.value;
        } // <--- End of foreach

        // NOW adjust for Aces and set the final total
        while (total > 21 && aceCount > 0)
        {
            total -= 10;
            aceCount--;
        }

        currentTotal = total; // This is now the definitive score
    }

    public void ClearHand()
    {
        hand.Clear();
        // Optionally, destroy card GameObjects if they are still in the scene
        foreach (Transform child in cardSpawnPoint)
        {
            Destroy(child.gameObject);
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
