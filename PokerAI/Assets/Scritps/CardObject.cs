using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardObject : MonoBehaviour {

    [SerializeField] private float moveSpeed;
    [SerializeField] private float tolorance;
    [SerializeField] private Texture2D allSprties;
   
    private Transform target;
    private Sprite[] sprites;
    private Sprite actuallSprite;
    private Sprite backSprite;


    public void InizializeObject(string sprite, Transform _target, bool hide) {
        target = _target;

        sprites = Resources.LoadAll<Sprite>(allSprties.name);

        for(int i = 0; i < sprites.Length; i++) {
            if(sprites[i].name == sprite) {
                actuallSprite = sprites[i];
            }

            if(sprites[i].name == "Back") {
                backSprite = sprites[i];
            }

        }

        if(hide)
            GetComponent<SpriteRenderer>().sprite = backSprite;
        else
            ShowCard();

        StartCoroutine(moveToTarget() as IEnumerator);
    }

    public void ShowCard() {
        GetComponent<SpriteRenderer>().sprite = actuallSprite;
    }

    private IEnumerator moveToTarget() {
        while(Vector2.Distance(target.position, transform.position) > tolorance) {
            transform.position = Vector2.MoveTowards(transform.position, target.position, moveSpeed);
            yield return new WaitForEndOfFrame();
        }
    }

}
