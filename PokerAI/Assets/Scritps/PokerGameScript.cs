using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public enum CardSuit { Heart, Dimond, Spades, Clubs }
public enum CardFace { Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }
public enum HandRank { HighCard, Pair, TwoPair, ThreeKind, Straight, Flush, FullHouse, FourKind, StraightFlush, RoyalFlush }

public class Card {
    public CardSuit suit;
    public CardFace face;

    public Card(CardSuit _suit, CardFace _face) {
        suit = _suit;
        face = _face;
    }

    public Card() {
        suit = CardSuit.Heart;
        face = CardFace.Ace;
    }

    public Card(Card _card) {
        suit = _card.suit;
        face = _card.face;
    }
}

public class CardCopare : IComparer<Card> {
    public int Compare(Card cardA, Card cardB) {

        if(cardA.face == cardB.face)
            return 0;

        return cardA.face.CompareTo(cardB.face);
    }
}

[System.Serializable]
public class CardLocation {
    public Transform location;
    public Card card;

    public CardLocation(Transform t, Card c) {
        location = t;
        card = c;
    }
}

public class PokerGameScript : MonoBehaviour{

    [Header("Game settings")]
    [SerializeField, Range(1, 4)] private int decksToUse;
    [SerializeField] private int totalRounds;
    [SerializeField] private float dealDeley;
    [SerializeField] private float roundResetDeley;
    [SerializeField] private bool hideCards;

    [Header("Ai settings")]
    [SerializeField] private bool isTraning;

    [Header("References")]
    [SerializeField] private Transform DealPosition;
    [SerializeField] private Text totalPotText;
    [SerializeField] private Text currentBetText;
    [SerializeField] private Text RoundText;
    [SerializeField] private Text WinnerText;
    [SerializeField] private GameObject cardObjectPrefab;

    private Stack<Card> shuffeldDeck;
    private List<Card> totalCards;
    private PokerTableCards tableCards;
    public PokerPlayer[] players { get; private set; }
    private float totalPot;
    private float currenBet;
    private int roundCount;
    private int cardsDelt;
    public bool gameDone { get; set; }

    private void Start() {
        tableCards = GetComponentInChildren<PokerTableCards>();
        players = GetComponentsInChildren<PokerPlayer>();

        foreach(PokerPlayer p in players) {
            p.resetCurrency();
            p.UpdateCurrencyText();
        }

        totalPot = 0;
        totalPotText.text = totalPot.ToString();

        roundCount = 1;
        RoundText.text = roundCount.ToString();

        currenBet = 1;
        currentBetText.text = currenBet.ToString();

        ShuffleDeck();

        cardsDelt = 0;

        StartCoroutine(DealPocketCards() as IEnumerator);
    }

    public IEnumerator DealPocketCards() {

        gameDone = false;

        int dealIndex = 0;
        WaitForSeconds dealTime = new WaitForSeconds(dealDeley);

        while(dealIndex < players.Length * 2) {
            yield return dealTime;
            
            //get next card in deck
            Card dealCard = shuffeldDeck.Pop();
            int index = 0;

            //assing the card
            if(dealIndex < players.Length) {
                players[dealIndex % players.Length].AssingCard(0, dealCard);
                index = 0;
            } else {
                players[dealIndex % players.Length].AssingCard(1, dealCard);
                index = 1;
            }

            //add count to player AI
            players[dealIndex % players.Length].cardCount.AddCardToCount(dealCard.suit, dealCard.face);

            //instansiate a new card object assosiated with the card dealt
            GameObject cardObj = Instantiate(cardObjectPrefab, DealPosition);

            string cardString = System.Enum.GetName(typeof(CardSuit), dealCard.suit) + System.Enum.GetName(typeof(CardFace), dealCard.face);

            if(dealIndex % players.Length == 0 && hideCards)    //show cards for player 1 no matter what
                cardObj.GetComponent<CardObject>().InizializeObject(cardString, players[dealIndex % players.Length].GetCardLocation(index), false);
            else
                cardObj.GetComponent<CardObject>().InizializeObject(cardString, players[dealIndex % players.Length].GetCardLocation(index), hideCards);

            cardObj.transform.rotation = players[dealIndex % players.Length].GetCardLocation(index).rotation;

            dealIndex++;
            cardsDelt++;
        }

        StartCoroutine(GetBets(1) as IEnumerator);

    }

    private IEnumerator GetBets(int betCount) {

        if(betCount == 1) {

            int playerIndex = 0;

            while(playerIndex < players.Length) {

                if(players[playerIndex].hasFold) {
                    playerIndex++;
                    continue;
                }

                players[playerIndex].StartCoroutine(players[playerIndex].CalculateBet(currenBet, cardsDelt, GetBestPlayer().GetFitness(), tableCards, decksToUse, betCount) as IEnumerator);        //start neurla network or wait for player to place bet
    
                while(players[playerIndex].isBetting()) {                                                   // wait for calculation
                    yield return null;
                }

                float betFromPlayer = players[playerIndex].GetBet();

                if(currenBet < betFromPlayer) {
                    currenBet = betFromPlayer;
                    currentBetText.text = currenBet.ToString();
                }

                totalPot += betFromPlayer;                                                  //get the bet and add it to total pot aswell as subtract it from player totlat currency
                totalPotText.text = totalPot.ToString();

                playerIndex++;
            }

            StartCoroutine(DealFlop() as IEnumerator);

        } else if(betCount == 2) {

            int playerIndex = 0;

            while(playerIndex < players.Length) {

                if(players[playerIndex].hasFold) {
                    playerIndex++;
                    continue;
                }

                players[playerIndex].StartCoroutine(players[playerIndex].CalculateBet(currenBet, cardsDelt, GetBestPlayer().GetFitness(), tableCards, decksToUse, betCount) as IEnumerator);        //start neurla network or wait for player to place bet

                while(players[playerIndex].isBetting()) {                                                   // wait for calculation
                    yield return null;
                }

                float betFromPlayer = players[playerIndex].GetBet();

                if(currenBet < betFromPlayer) {
                    currenBet = betFromPlayer;
                    currentBetText.text = currenBet.ToString();
                }

                totalPot += betFromPlayer;                                                  //get the bet and add it to total pot aswell as subtract it from player totlat currency
                totalPotText.text = totalPot.ToString();

                playerIndex++;
            }

            yield return new WaitForSeconds(dealDeley);

            AddCardToFlop(1);
            StartCoroutine(GetBets(3) as IEnumerator);

        } else if(betCount == 3) {

            int playerIndex = 0;

            while(playerIndex < players.Length) {

                if(players[playerIndex].hasFold) {
                    playerIndex++;
                    continue;
                }

                players[playerIndex].StartCoroutine(players[playerIndex].CalculateBet(currenBet, cardsDelt, GetBestPlayer().GetFitness(), tableCards, decksToUse, betCount) as IEnumerator);        //start neurla network or wait for player to place bet

                while(players[playerIndex].isBetting()) {                                                   // wait for calculation
                    yield return null;
                }

                float betFromPlayer = players[playerIndex].GetBet();

                if(currenBet < betFromPlayer) {
                    currenBet = betFromPlayer;
                    currentBetText.text = currenBet.ToString();
                }

                totalPot += betFromPlayer;                                                  //get the bet and add it to total pot aswell as subtract it from player totlat currency
                totalPotText.text = totalPot.ToString();

                playerIndex++;
            }

            yield return new WaitForSeconds(dealDeley);

            AddCardToFlop(2);

            yield return new WaitForSeconds(1);

            StartCoroutine(EvaluateWinner() as IEnumerator);
        }

    }

    private IEnumerator DealFlop() {
        int dealIndex = 0;
        WaitForSeconds dealTime = new WaitForSeconds(dealDeley);

        while(dealIndex < 3) {
            yield return dealTime;

            //get and assing next card in deck
            Card dealCard = shuffeldDeck.Pop();
            tableCards.AssingCard(dealIndex, dealCard);

            //instansiate a new card object assosiated with the card dealt
            GameObject cardObj = Instantiate(cardObjectPrefab, DealPosition);
            string cardString = System.Enum.GetName(typeof(CardSuit), dealCard.suit) + System.Enum.GetName(typeof(CardFace), dealCard.face);
            cardObj.GetComponent<CardObject>().InizializeObject(cardString, tableCards.GetCardLocation(dealIndex), false);

            dealIndex++;
            cardsDelt++;

            //add table card to running count in each player
            foreach(var player in players)
                player.cardCount.AddCardToCount(dealCard.suit, dealCard.face);

        }

        StartCoroutine(GetBets(2) as IEnumerator);
    }

    private void AddCardToFlop(int index) {
        //get and assing next card in deck
        Card dealCard = shuffeldDeck.Pop();
        tableCards.AssingCard(2 + index, dealCard);

        //instansiate a new card object assosiated with the card dealt
        GameObject cardObj = Instantiate(cardObjectPrefab, DealPosition);
        string cardString = System.Enum.GetName(typeof(CardSuit), dealCard.suit) + System.Enum.GetName(typeof(CardFace), dealCard.face);
        cardObj.GetComponent<CardObject>().InizializeObject(cardString, tableCards.GetCardLocation(2 + index), false);

        cardsDelt++;

        //add table card to running count for each player
        foreach(var player in players) 
            player.cardCount.AddCardToCount(dealCard.suit, dealCard.face);

    }

    private IEnumerator EvaluateWinner() {

        //evaulate Cards
        HandRank bestRank = 0;

        int bestIndex = 0;

        for(int i = 0; i < players.Length; i++) {

            if(players[i].hasFold) {
                continue;
            }

            HandRank evRank = EvaluateHand(players[i].GetCard(0), players[i].GetCard(1));

            if((int)bestRank <= (int)evRank) {
                bestRank = evRank;
                bestIndex = i;
            }
        }

        WinnerText.text = "Winner is player " + (bestIndex + 1);

        players[bestIndex].AddCurrency(totalPot);

        totalPot = 0;
        totalPotText.text = totalPot.ToString();

        CardObject[] cards = DealPosition.gameObject.GetComponentsInChildren<CardObject>();

        foreach(CardObject card in cards) {
            card.ShowCard();
        }

        yield return new WaitForSeconds(roundResetDeley);

        ResetRound();
    }

    private HandRank EvaluateHand(Card c1, Card c2) {
        List<Card> cards = new List<Card>();
        cards.Add(c1);
        cards.Add(c2);
        cards.Add(tableCards.GetCard(0));
        cards.Add(tableCards.GetCard(1));
        cards.Add(tableCards.GetCard(2));
        cards.Add(tableCards.GetCard(3));
        cards.Add(tableCards.GetCard(4));

        cards.Sort(new CardCopare());

        Dictionary<CardSuit, int> suitsInHand = new Dictionary<CardSuit, int>();
        Dictionary<CardFace, int> faceInHand = new Dictionary<CardFace, int>();

        foreach(int suit in System.Enum.GetValues(typeof(CardSuit))) {
            suitsInHand.Add((CardSuit)suit, 0);
        }

        foreach(int face in System.Enum.GetValues(typeof(CardFace))) {
            faceInHand.Add((CardFace)face, 0);
        }

        for(int i = 0; i < cards.Count; i++) {
            suitsInHand[cards[i].suit] += 1;
            faceInHand[cards[i].face] += 1;
        }

        //get a sorted list of all face values
        List<int> allFace = new List<int>();

        foreach(Card c in cards) {
            allFace.Add((int)c.face);
        }

        //royal flush and straigth flush

        if(suitsInHand.ContainsValue(5)) {

            //create a range of values from ten to ace, create a remaning list with all values that are not in the new range
            List<int> _rangeList = Enumerable.Range((int)CardFace.Ten, (int)CardFace.Ace).ToList();
            List<int> _Remaing = allFace.Except(allFace).ToList();

            //if there is no remaning numbers that means that there is a sequence of a royal flush
            if(_Remaing.Count <= 0) {
                return HandRank.RoyalFlush;
            }

            //if there is not royal flush check for a straigth flush

            //get last and first face value
            int _first = allFace.First();
            int _last = allFace.Last();

            //create a range of values from first to last, create a remaning list with all values that are not in the list of face values 
            _rangeList = Enumerable.Range(_first, _last - _first + 1).ToList();
            _Remaing = _rangeList.Except(allFace).ToList();

            //if there is no remaning numbers that means that there is a sequence
            if(_Remaing.Count <= 0) {
                return HandRank.StraightFlush;
            }
        }

        //four of a kind
        if(faceInHand.ContainsValue(4)) {
            return HandRank.FourKind;
        }

        // full house
        if(faceInHand.ContainsValue(3) && faceInHand.ContainsValue(2)) {
            return HandRank.FullHouse;
        }

        //flush
        if(suitsInHand.ContainsValue(5)) {
            return HandRank.Flush;
        }

        //straigth
        //get last and first face value
        int first = allFace.First();
        int last = allFace.Last();

        //create a range of values from first to last, create a remaning list with all values that are not in the list of face values 
        List<int> rangeList = Enumerable.Range(first, last - first + 1).ToList();
        List<int> remaing = rangeList.Except(allFace).ToList();

        //if there is no remaning numbers that means that there is a sequence
        if(remaing.Count <= 0) {
            return HandRank.Straight;
        }


        //three of a kind
        if(faceInHand.ContainsValue(3)) {
            return HandRank.ThreeKind;
        }

        //two pair
        int count = 0;
        foreach(var input in faceInHand) {
            if(input.Value == 2) {
                count++;
            }
        }

        if(count >= 2) {
            return HandRank.TwoPair;
        }

        if(faceInHand.ContainsValue(2)) {
            return HandRank.Pair;
        }

        return HandRank.HighCard;
    }

    private void ResetRound() {

        WinnerText.text = "";

        roundCount++;
        RoundText.text = roundCount.ToString();

        currenBet = 1;
        currentBetText.text = currenBet.ToString();

        foreach(PokerPlayer p in players) {
            p.ResetHand();
        }

        tableCards.ResetTable();

        foreach(Transform t in DealPosition) {
            Destroy(t.gameObject);
        }

        if(shuffeldDeck.Count < 15) {
            ShuffleDeck();
            cardsDelt = 0;

            //reset running count for each player
            foreach(var player in players)
                player.cardCount.ResetCounter();

        }

        if(isTraning) {
            //if traning tell AI that this table is done or start a new round
            if(roundCount > totalRounds)
                gameDone = true;
            else
                StartCoroutine(DealPocketCards() as IEnumerator);

        } else {

            //if player is playing, reset game or start a new round
            if(roundCount > totalRounds)
                ResetGame();
            else
                StartCoroutine(DealPocketCards() as IEnumerator);
        }   
    }

    public void ResetGame() {

        foreach(PokerPlayer p in players) {
            p.resetCurrency();
        }

        roundCount = 1;
        RoundText.text = roundCount.ToString();

        ShuffleDeck();
        cardsDelt = 0;

        //reset running count for each player
        foreach(var player in players)
            player.cardCount.ResetCounter();

        StartCoroutine(DealPocketCards() as IEnumerator);

    }

    public void ResetPlayers() {
        for(int i = 0; i < players.Length; i++) {
            players[i].resetCurrency();
            players[i].ResetHand();
        }
    }

    private void ShuffleDeck() {
        shuffeldDeck = new Stack<Card>();
        totalCards = new List<Card>();

        //add all cards including duplicates to a list that can be chosen from
        foreach(int suit in System.Enum.GetValues(typeof(CardSuit))) {
            foreach(int face in System.Enum.GetValues(typeof(CardFace))) {
                for(int i = 0; i < decksToUse; i++) {
                    totalCards.Add(new Card((CardSuit)suit, (CardFace)face));
                }
            }
        }

        //add cards from total cards at radom to another list to suffle decs
        while(totalCards.Count > 0) {
            Card selectedCard = totalCards[Random.Range(0, totalCards.Count - 1)];
            totalCards.Remove(selectedCard);
            shuffeldDeck.Push(selectedCard);
        }
    }

    public PokerPlayer GetBestPlayer() {
        PokerPlayer bestPlayer = null;
        float bestFitness = 0;

        foreach(var player in players) {
            if(bestFitness < player.GetFitness()) {
                bestFitness = player.GetFitness();
                bestPlayer = player;
            }
        }

        return bestPlayer;
    }
}
