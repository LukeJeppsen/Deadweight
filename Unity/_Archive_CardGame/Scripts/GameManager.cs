using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Deck deck;
    public Hand playerHand;
    public Hand dealerHand;
    public GameState currentState;

    public int currentDebt = 10000;
    public int currentBid = 100;
    public int money = 1000;
    public int currentFloorBid = 100; // Minimum starting bid for floor 1
    public int FloorDebtMultiplier = 2; // Debt multiplier per floor
    public int FloorDebtThreshold = 10000; // Debt threshold to advance to next floor

    [Header("UI References")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI playerTotalText;
    public TextMeshProUGUI dealerTotalText;
    public TextMeshProUGUI debtText;
    public TextMeshProUGUI upBid;
    public TextMeshProUGUI downBid;
    public TextMeshProUGUI bidText;
    public TextMeshProUGUI currentMoney;

    public Button hit;
    public Button stand;
    public Button start;
    public Button raise;
    public Button lower;

    public Slider debtSlider;

    // Call this whenever debt changes (e.g., in DetermineWinner)
    public void UpdateEconomicUI()
    {
        // Sliders usually take a value between 0 and 1 if using 'whole numbers' is off
        debtSlider.maxValue = FloorDebtThreshold;
        debtSlider.value = money;

        // Optional: Change color to red if debt is over 75%
        Image fillImage = debtSlider.fillRect.GetComponent<Image>();
        if ((float)currentDebt / FloorDebtThreshold > 0.75f)
        {
            fillImage.color = Color.red;
        }
    }


    public void showButtonsGameStart()
    {
        hit.gameObject.GetComponent<Button>().interactable = true;
        stand.gameObject.GetComponent<Button>().interactable = true;
        raise.gameObject.GetComponent<Button>().interactable = true;
        lower.gameObject.GetComponent<Button>().interactable = true;
        start.gameObject.GetComponent<Button>().interactable = false;
    }

    public void hideButtonsGameEnded()
    {
        hit.gameObject.GetComponent<Button>().interactable = false;
        stand.gameObject.GetComponent<Button>().interactable = false;
        raise.gameObject.GetComponent<Button>().interactable = false;
        lower.gameObject.GetComponent<Button>().interactable = false;
        start.gameObject.GetComponent<Button>().interactable = true;
    }
    public void UpdateUI(string message)
    {
        statusText.text = message;
        playerTotalText.text = $"{playerHand.currentTotal}";
        dealerTotalText.text = $"{dealerHand.currentTotal}";
        debtText.text = $"Debt: ${currentDebt - money}";
        bidText.text = $"Bid: ${currentBid}";
        currentMoney.text = "Total Cash: $" + money;

    }

    public void RaiseBid()
    {
        if (currentBid == money)
        {
            UpdateUI("Not enough money to raise the bid! Current bid: $" + currentBid);
            return;
        }
        currentBid += currentFloorBid;
        UpdateUI("Bid raised! Current bid: $" + currentBid);
    }

    public void LowerBid()
    {
        if (currentBid > currentFloorBid)
        {
            currentBid -= currentFloorBid;
            UpdateUI("Bid lowered! Current bid: $" + currentBid);
        }
        else
        {
            UpdateUI("Cannot lower bid below the floor! Current bid: $" + currentBid);
        }
    }
    public void StartGame()
    {
        showButtonsGameStart();
        deck.InitializeDeck();
        playerHand.ClearHand();
        dealerHand.ClearHand();
 
        playerHand.AddCard(deck.DrawCard());
        dealerHand.AddCard(deck.DrawCard());
        playerHand.AddCard(deck.DrawCard());
        dealerHand.AddCard(deck.DrawCard());

        // 3. Set the state
        currentState = GameState.playerTurn;

        // 4. Initial Score Check
        playerHand.CalculateScore();
        dealerHand.CalculateScore();
        UpdateUI("Game Start");
    }

    public void Hit()
    {
        // Draw the card and add it in one move
        playerHand.AddCard(deck.DrawCard());

        playerHand.CalculateScore();
        UpdateUI("");
        if (playerHand.currentTotal > 21)
        {
            DetermineWinner();
            // Trigger your "Bust" UI here
        }
    }

    public void Stand()
    {
        // Transition to dealer's turn
        currentState = GameState.dealerTurn;
        dealerHand.CalculateScore();

        // The Dealer's AI loop
        while (dealerHand.currentTotal < 17)
        {
            Debug.Log("Dealer drawing. Current score: " + dealerHand.currentTotal);
            dealerHand.AddCard(deck.DrawCard());
            dealerHand.CalculateScore();
            UpdateUI("Dealer draws a card...");
        }

        // Determine Result
        DetermineWinner();
    }

    private void DetermineWinner()
    {
        hideButtonsGameEnded();
        currentState = GameState.gameOver;

        int pScore = playerHand.currentTotal;
        int dScore = dealerHand.currentTotal;

        if (pScore > 21)
        {
            money -= currentBid;
            UpdateUI("BUST! YOU LOSE");
        }
        else if (dScore > 21 || pScore > dScore)
        {
            money += currentBid;
            UpdateUI("YOU WIN");
            
        }
        else if (pScore < dScore)
        {
            money -= currentBid;
            UpdateUI("DEALER WINS");
        }
        else
        {
            UpdateUI("TIE");
        }
        UpdateEconomicUI();
    }
    public void ResetRound()
    {
        playerHand.ClearHand();
        dealerHand.ClearHand();

        if (deck.GetPercentRemaining() <= 0.25f)
        {
            Debug.Log("Deck low. Initializing fresh deck for the new round.");
            deck.InitializeDeck();
        }
        currentState = GameState.start;
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

public enum GameState { start, playerTurn, dealerTurn, gameOver }
