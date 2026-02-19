using TMPro;
using UnityEngine;
using System.Collections;
using System.Xml;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
public class ObjectiveUpdate : MonoBehaviour
{
    public List<TextMeshProUGUI> objectiveTexts;
    public Image objectiveIcon;

    public float blinkDuration = 1.5f;     // Total blink time
    public float blinkInterval = 0.2f;     // How fast it blinks
    public float fadeDuration = 6f;        // How long to fade out

    public void TextFade(string objectiveDescription)
    {
        objectiveTexts[0].text = $"[{objectiveDescription}]";
        StartCoroutine(Fade());
    }
    public IEnumerator Fade()
    {
        float timer = 0f;
        bool visible = true;

        // Blinking loop
        while (timer < blinkDuration)
        {
            visible = !visible;
            SetAlpha(visible ? 1f : 0f);
            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }

        // Ensure fully visible before fading
        SetAlpha(1f);

        // Fading loop
        float fadeTimer = 0f;
        while (fadeTimer < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeDuration);
            SetAlpha(alpha);
            fadeTimer += Time.deltaTime;
            yield return null;
        }

        // Ensure fully transparent at end
        SetAlpha(0f);
    }

    void SetAlpha(float alpha)
    {
        foreach (TextMeshProUGUI tmp in objectiveTexts)
        {
            if (tmp == null) continue;
            Color c = tmp.color;
            c.a = alpha;
            tmp.color = c;
        }

        if (objectiveIcon != null)
        {
            Color c = objectiveIcon.color;
            c.a = alpha;
            objectiveIcon.color = c;
        }
    }
}
