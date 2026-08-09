using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialCanvases : MonoBehaviour
{
    //this is the default position for tutorial prompts on screen. Reference this when Fading in your tutorialPanel
    public Vector3 tutorialCanvasesPos = new Vector3(0, -175, 0);

    public float fadeDuration = 1f;

    /// <summary>
    /// Fade in the UI element, instatiate and make it visible
    /// </summary>
    /// <param name="canvasGroupPrefab"></param>
    /// <param name="canvasGroupObj"></param>
    /// <param name="canvasGroup"></param>
    /// <param name="pos"></param>
    public void FadeIn(GameObject canvasGroupPrefab, ref GameObject canvasGroupObj, ref CanvasGroup canvasGroup, Vector3 pos)
    {
        InstantiateCanvasGroup(canvasGroupPrefab, ref canvasGroupObj, ref canvasGroup, pos);
        StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f));
    }

    /// <summary>
    /// Fade out the UI element, make it invisible and destroy it
    /// </summary>
    /// <param name="canvasGroupObj"></param>
    /// <param name="canvasGroup"></param>
    /// <param name="onDestroyed"></param>
    public void FadeOut(GameObject canvasGroupObj, CanvasGroup canvasGroup, Action onDestroyed = null)
    {
        StartCoroutine(FadeOutThenDestroyCanvasGoup(canvasGroupObj, canvasGroup, onDestroyed));
    }

    /// <summary>
    /// Delay for inputed time, then fadeout and destroy it
    /// </summary>
    /// <param name="delayTime"></param>
    /// <param name="canvasGroupObj"></param>
    /// <param name="canvasGroup"></param>
    /// <param name="onDestroyed"></param>
    /// <returns></returns>
    public IEnumerator DelayFadeOut(float delayTime, GameObject canvasGroupObj, CanvasGroup canvasGroup, Action onDestroyed = null)
    {
        yield return new WaitForSeconds(delayTime); // Wait for the specified time
        FadeOutThenDestroyCanvasGoup(canvasGroupObj, canvasGroup, onDestroyed);
    }

    /// <summary>
    /// This helper method finds a slider by name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Slider FindSliderByName(string name)
    {
        Transform found = FindDeepChild(this.transform, name);
        if (found != null)
        {
            return found.GetComponent<Slider>();
        }
        return null;
    }

    /// <summary>
    /// This method instantiates a canvas group and assigns it to your event script
    /// it takes three objects from the script, a reference to the canvas prefab, the gameobject it is, and a reference to th canvas group itself
    /// </summary>
    /// <param name="canvasGroupPrefab"></param>
    /// <param name="canvasGroupObj"></param>
    /// <param name="canvasGroup"></param>
    /// <param name="pos"></param>
    private void InstantiateCanvasGroup(GameObject canvasGroupPrefab, ref GameObject canvasGroupObj, ref CanvasGroup canvasGroup, Vector3 pos)
    {
        if (canvasGroup == null)
        {
            canvasGroupObj = Instantiate(canvasGroupPrefab);
            canvasGroupObj.transform.SetParent(this.transform, false);
            canvasGroupObj.transform.localScale = Vector3.one;
            canvasGroupObj.GetComponent<RectTransform>().anchoredPosition3D = pos;
            canvasGroup = canvasGroupObj.GetComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// This method is used in FadeOut(). it controls logic to fade out then destroy the canvas group
    ///  It takes 3 arguements, the canvas GameObject, the Canvasgroup component of that object, and a action reference to establish what to do OnDestroyed
    ///  Action example: () => { canvasObj = null; canvasGroup = null }
/// </summary>
/// <param name="canvasGroupObj"></param>
/// <param name="canvasGroup"></param>
/// <param name="onDestroyed"></param>
/// <returns></returns>
private IEnumerator FadeOutThenDestroyCanvasGoup(GameObject canvasGroupObj, CanvasGroup canvasGroup, Action onDestroyed)
    {
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f));

        if (canvasGroupObj != null)
            Destroy(canvasGroupObj);

        onDestroyed?.Invoke();
    }

    /// <summary>
    /// Coroutine to fade the CanvasGroup over time
    /// </summary>
    /// <param name="canvasGroup"></param>
    /// <param name="startAlpha"></param>
    /// <param name="endAlpha"></param>
    /// <returns></returns>
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha)
    {
        float timeElapsed = 0f;

        while (timeElapsed < fadeDuration)
        {
            if (canvasGroup == null) yield break; // Exit if the canvasGroup is destroyed

            // Lerp alpha from start to end
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timeElapsed / fadeDuration);
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        if (canvasGroup != null)
            canvasGroup.alpha = endAlpha; // Ensure it's set to the final alpha
    }

    /// <summary>
    /// this helper method finds the gameobject child of a parent gameobject by name
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

}
