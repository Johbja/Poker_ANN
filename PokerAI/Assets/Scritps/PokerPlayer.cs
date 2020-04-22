using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardCounter {
    private Dictionary<CardFace, int> faceCount = new Dictionary<CardFace, int>();
    private Dictionary<CardSuit, int> suitCount = new Dictionary<CardSuit, int>();

    public CardCounter() {
        foreach(var card in System.Enum.GetValues(typeof(CardFace)))
            faceCount.Add((CardFace)card, 0);

        foreach(var card in System.Enum.GetValues(typeof(CardSuit)))
            suitCount.Add((CardSuit)card, 0);
    }

    public void AddCardToCount(CardSuit suit, CardFace face) {
        faceCount[face]++;
        suitCount[suit]++;
    }

    public int GetSuitCount(CardSuit suit) {
        return suitCount[suit];
    }

    public int GetFaceCount(CardFace face) {
        return faceCount[face];
    }

    public void ResetCounter() {

        foreach(var card in System.Enum.GetValues(typeof(CardFace)))
            faceCount[(CardFace)card] = 0;

        foreach(var card in System.Enum.GetValues(typeof(CardSuit)))
            suitCount[(CardSuit)card] = 0;
    }

}

public class PokerPlayer : MonoBehaviour {

    [Header("Game settings")]
    [SerializeField] private float startCurrency;
    [SerializeField] private float betDeley;
    [SerializeField] private Button[] PlayerButtons;

    [Header("Ai settings")]
    [SerializeField] private bool isAi;
    [SerializeField] private Brain brain;

    [Header("References")]
    [SerializeField] private CardLocation[] cardLocations;
    [SerializeField] private Text currencyText;
    [SerializeField] private FadeScript fade;

    public CardCounter cardCount { get; set; }
    public Network network { get; set; }
    public bool hasFold { get; private set; } 

    private bool betting;

    private float bet;
    private float currenCurrency;
    private float betPlayer;

    private void Awake() {
        network = brain.GetNetwork();
        cardCount = new CardCounter();
    }

    private void Start() {
        if(!isAi) {
            ChangeButtonVisability(false);
        }
    }

    public void AssingCard(int index, Card card) {
        if(index < cardLocations.Length)
            cardLocations[index].card = new Card(card);

        betting = false;
    }

    public void AddCurrency(float c) {
        currenCurrency += c;
        UpdateCurrencyText();
    }

    public Transform GetCardLocation(int index) {
        if(index < cardLocations.Length)
            return cardLocations[index].location;
        else
            return null;
    }

    public Card GetCard(int index) {
        if(index < cardLocations.Length)
            return cardLocations[index].card;
        else
            return null;
    }

    public void ResetHand() {
        foreach(CardLocation card in cardLocations) {
            card.card = null;
        }
        hasFold = false;
    }

    public void UpdateCurrencyText() {
        currencyText.text = currenCurrency.ToString();
    }

    public void resetCurrency() {
        currenCurrency = startCurrency;
        hasFold = false;
        UpdateCurrencyText();
    }

    public float GetBet() {
        currenCurrency -= bet;
        UpdateCurrencyText();
        float returnBet = bet;
        bet = 0;
        return returnBet;
    }

    public bool isBetting() {
        return betting;
    }

    public float GetFitness() {
        return Mathf.Max(0.1f, currenCurrency);
    }

    public void SaveBrain() {
        brain.StoreNetowrk(network);
    }

    public IEnumerator CalculateBet(float currenBet, int cardsDelt, float topCurrency, PokerTableCards table, int deckSize, int turn) {
        //use neural network here
        betting = true;

        if(isAi) {

            //inputs = { totalCount, heartsCount, dimondCount, spadeCount, clubsCount, hand1, hand2, table1, table2, table3, table4, table5, turn, cash } 

            float[] inputs = {
                cardsDelt / (52 * deckSize),                                            //noramlize cards dealt agains size of one deck
                (float)cardCount.GetSuitCount(CardSuit.Heart) / (float)(14 * deckSize), //noramlize suits dealt agains max amount of suits in deck 
                (float)cardCount.GetSuitCount(CardSuit.Dimond) / (float)(14 * deckSize),
                (float)cardCount.GetSuitCount(CardSuit.Spades) / (float)(14 * deckSize),
                (float)cardCount.GetSuitCount(CardSuit.Clubs) / (float)(14 * deckSize),
                (float)((int)cardLocations[0].card.suit + (int)cardLocations[0].card.face) / (float)((int)CardSuit.Clubs * (int)CardFace.Ace),  //normalize card agains all possible cards
                (float)((int)cardLocations[1].card.suit + (int)cardLocations[1].card.face) / (float)((int)CardSuit.Clubs * (int)CardFace.Ace),
                0, 0, 0, 0,                 //set 0 for now and add cards in depeding on turn
                turn / 3,                   //normalize betting turn
                currenCurrency/topCurrency  //normalzie own currenzy against top currency
            };

            if(turn >= 2) {
                inputs[7] = (float)((int)table.GetCard(0).suit + (int)table.GetCard(0).face) / (float)((int)CardSuit.Clubs * (int)CardFace.Ace);    //normalize card agains all possible cards
                inputs[8] = (float)((int)table.GetCard(1).suit + (int)table.GetCard(1).face) / (float)((int)CardSuit.Clubs * (int)CardFace.Ace);
                inputs[9] = (float)((int)table.GetCard(2).suit + (int)table.GetCard(2).face) / (float)((int)CardSuit.Clubs * (int)CardFace.Ace);
            }

            if(turn >= 3) {
                inputs[10] = (float)((int)table.GetCard(3).suit + (int)table.GetCard(3).face) / (float)((int)CardSuit.Clubs * (int)CardFace.Ace);   //normalize card agains all possible cards
            }

            float[] outputs = network.SendInputs(inputs);

            float topOut = 0;
            int outIndex = 0;

            //find larges ouput
            for(int i = 0; i < outputs.Length; i++) {
                if(topOut < outputs[i]) {
                    topOut = outputs[i];
                    outIndex = i;
                }

            }

            //do decision based on largets value
            switch(outIndex) {
                case 0:
                    Raise(currenBet);
                    break;
                case 1:
                    Call(currenBet);
                    break;
                case 2:
                    Fold();
                    break;
            }

        } else {
            
            //show buttons
            ChangeButtonVisability(true);
            betPlayer = currenBet;

            //wait for player to press button
            yield return new WaitWhile(() => betting == true);

            //hide buttons
            ChangeButtonVisability(false);

        }

        

        yield return new WaitForSeconds(betDeley);
        betting = false;
        yield return null;
    }

    public void PlayerHasBet() {
        betting = false;
    }

    public void Raise(float currentBet) {
        fade.StartFade("Raise");
        if(isAi)
            bet = currentBet + 1;
        else
            bet = betPlayer + 1;
    }

    public void Call(float currentBet) {
        fade.StartFade("Call");
        if(isAi)
            bet = currentBet;
        else
            bet = betPlayer;
    }

    public void Fold() {
        fade.SetPermanent("Fold");
        hasFold = true;
    }

    private void ChangeButtonVisability(bool visability) {
        foreach(var button in PlayerButtons) {
            button.gameObject.SetActive(visability);
        }
    }

}
