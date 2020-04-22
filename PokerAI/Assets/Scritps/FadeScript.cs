using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeScript : MonoBehaviour {

    [SerializeField] private float fadeSetp;

    private Text text;

    void Start() {
        text = GetComponent<Text>();
        text.text = "";
    }

    public void StartFade(string textToShow) {
        text.text = textToShow;
        SetAlpha(1);
        StartCoroutine(Fade() as IEnumerator);
    }

    public void SetPermanent(string textToShow) {
        text.text = textToShow;
        SetAlpha(1);
    }

    public void Hide() {
        SetAlpha(0);
    }

    private void SetAlpha(float alpha) {
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }

    private IEnumerator Fade() {

        while(text.color.a > 0) {

            Color c = text.color;
            c.a -= fadeSetp * Time.deltaTime;
            text.color = c;

            yield return null;
        }

    }

}
