using UnityEngine;
using UnityEngine.UI;

public class TutorialCanvases : MonoBehaviour
{
    public Vector3 tutorialCanvasesPos = new Vector3(0, -175, 0);

    public void InstantiateCanvasGroup(GameObject canvasGroupPrefab, ref GameObject canvasGroupObj, ref CanvasGroup canvasGroup, Vector3 pos)
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

    public void DestroyCanvasGroup(ref GameObject canvasGroupObj, ref CanvasGroup canvasGroup)
    {
        canvasGroup = null;
        Destroy(canvasGroupObj);
    }

    public Slider FindSliderByName(string name)
    {
        Transform found = FindDeepChild(this.transform, name);
        if (found != null)
        {
            return found.GetComponent<Slider>();
        }
        return null;
    }

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
