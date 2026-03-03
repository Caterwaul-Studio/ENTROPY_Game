using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Win : MonoBehaviour
{
    public bool winCondition = false;
    [SerializeField]
    private DoorScript winDoor;
    public bool canWin = true;
    public CanvasGroup fadeCanvasGroup;
    public CanvasGroup thanksGroup;
    public GameObject _uiCam;

    private void Start()
    {
        if (fadeCanvasGroup == null)
            Debug.LogError("fadeCanvasGroup is NOT assigned in the inspector!");
        winCondition = false;
        //_uiCam = GameObject.FindGameObjectWithTag("UICamera");
        if(_uiCam == null)
        {
            print("NULL UI CAM");
        }
    }
    public bool WinCondition
    {
        get { return winCondition; }
        set { winCondition = value; }
    }

    void Update()
    {
        // Check if the win door (final door of the game) is open and the player can win
        //first ensure we catch if there is no win door assigned in inspector
        if(winDoor == null)
        {
            //Debug.LogError("winDoor, 'final door of level' is NOT assigned in the inspector!");
            return;
        }
        else
        {
            if (winDoor.DoorState == DoorScript.States.Open && canWin == true)
            {
                canWin = false;
                _uiCam.SetActive(true);
                StartCoroutine(ShowWin());
            }
        }
    }

    private IEnumerator ShowWin()
    {
        yield return StartCoroutine(FadeOut(fadeCanvasGroup, 10f));
        //yield return new WaitForSeconds(1f);
        winCondition = true;

        StartCoroutine(FadeOut(thanksGroup, 12f));
    }

    public IEnumerator FadeOut(CanvasGroup group, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            group.alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        group.alpha = 1f;
    }

}
