using System.Collections;
using TMPro;
using UnityEngine;

public class PhasePopupUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI phaseText;

    [SerializeField] private float popScale = 1.25f;
    [SerializeField] private float popDuration = 0.2f;
    [SerializeField] private float holdDuration = 1.2f;
    [SerializeField] private float fadeDuration = 1.5f;

    private Vector3 originalScale;
    private Coroutine routine;

    private void Awake()
    {
        if (phaseText == null)
            phaseText = GetComponent<TextMeshProUGUI>();

        originalScale = transform.localScale;
        SetAlpha(0f);
    }

    public void ShowPhasePopup(string message)
    {
        if (routine != null)
            StopCoroutine(routine);

        phaseText.text = message;
        routine = StartCoroutine(PopupRoutine());
    }

    private IEnumerator PopupRoutine()
    {
        transform.localScale = originalScale;
        SetAlpha(1f);

        Vector3 big = originalScale * popScale;

        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, big, t / popDuration);
            yield return null;
        }

        t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(big, originalScale, t / popDuration);
            yield return null;
        }

        yield return new WaitForSeconds(holdDuration);

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(1f, 0f, t / fadeDuration));
            yield return null;
        }

        SetAlpha(0f);
    }

    private void SetAlpha(float a)
    {
        Color c = phaseText.color;
        c.a = a;
        phaseText.color = c;
    }
}