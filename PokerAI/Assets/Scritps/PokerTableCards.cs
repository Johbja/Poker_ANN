using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokerTableCards : MonoBehaviour {

   [SerializeField] private CardLocation[] cardLocations;

    public void AssingCard(int index, Card card) {
        if(index < cardLocations.Length)
            cardLocations[index].card = new Card(card);
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

    public void ResetTable() {
        foreach(CardLocation card in cardLocations) {
            card.card = null;
        }
    } 

}
