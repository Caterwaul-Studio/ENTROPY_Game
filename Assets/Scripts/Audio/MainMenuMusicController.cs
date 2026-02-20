using UnityEngine;

public class MainMenuMusicController : MonoBehaviour
{
    [SerializeField] private Looper looper;
    [SerializeField] private AudioClip musicClip;

    private double nextLoopTime;

    private void Start()
    {
        nextLoopTime = AudioSettings.dspTime + 0.1;
        looper.Enqueue(musicClip, true, nextLoopTime);
        nextLoopTime += musicClip.length;
    }

    private void Update()
    {
        if (AudioSettings.dspTime >= nextLoopTime - 1.0)
        {
            looper.Enqueue(musicClip, true, nextLoopTime);
            nextLoopTime += musicClip.length;
        }
    }

    public void FadeOut()
    {
        looper.FadeOut(Looper.mediumFadeDuration);
    }
}