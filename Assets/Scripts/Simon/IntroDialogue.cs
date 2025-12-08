using UnityEngine;
using System.Collections;

public class IntroDialogue : MonoBehaviour
{
    public CanvasGroup introGroup;
    public CanvasGroup dialogueGroup;
    public float delay = 10f;
    public float fadeDuration = 1.5f;

    void Start()
    {
        // États initiaux
        introGroup.alpha = 1f;
        dialogueGroup.alpha = 0f;

        introGroup.gameObject.SetActive(true);
        dialogueGroup.gameObject.SetActive(true);

        // Lancement après delay
        Invoke(nameof(StartFade), delay);
    }

    void StartFade()
    {
        StartCoroutine(FadePanels());
    }

    IEnumerator FadePanels()
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = t / fadeDuration;

            introGroup.alpha = 1f - a;     // fade out
            dialogueGroup.alpha = a;       // fade in

            yield return null;
        }

        introGroup.alpha = 0f;
        dialogueGroup.alpha = 1f;

        introGroup.gameObject.SetActive(false);
    }
}
