using System;
using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; } // Singleton instance

    // UI elements for dialogue display
    [Header("Dialogue Data")]
    public DialogueSequence[] dialogueSequences;
    public DialogueSequence failureDialogues;

    [Header("UI")]
    public Canvas dialogueCanvas;
    private string dialogueCanvasObj = "DialogueCanvas";
    public CanvasGroup dialogueCanvasGroup;
    public TextMeshProUGUI nameTextUI;
    private string nameTextObj = "NameTextUI";
    public TextMeshProUGUI dialogueTextUI;
    private string dialogueTextObj = "DialogueTextUI";

    [Header("Audio / Typewriter")]
    public AudioSource defaultAudioSource;
    public List<AudioSource> sourceList;
    public AudioClip fillerLineBeep;
    public float defaultTextSpeed = 0.08f;
    public float textSpeedMultiplier = 1.1f;
    public DialogueAudio dialogueAudio;

    [Header("Fade / Timings")]
    public float fadeDuration = 0.5f;
    public float skipPauseDuration = 0.3f;

    [Header("Tutorial / External")]
    public TutorialManager tutorialManager;
    //need to restore these references when the player is reloaded from a save, since the player object is destroyed and recreated
    public PersistantManager persistantManager;
    public GameObject player;
    private string playerObjectName = "ZeroGPlayer";
    public PlayerController playerController;
    public ZeroGravity playerManager;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip skipSfxClip;

    public bool IsDialogueActive => currentState == DialogueState.Playing || currentState == DialogueState.Paused;
    public bool IsDialogueSpeaking => isLineTyping;
    public bool IsFailureSpeaking => isPlayingFailureDialogue;

    public readonly Queue<int> sequenceQueue = new Queue<int>();

    private Coroutine displayCoroutine = null;
    private Coroutine fadeCoroutine = null;
    private Coroutine typewriterCoroutine = null;

    public enum DialogueState { Idle, Playing, Paused, Fading }
    public DialogueState currentState = DialogueState.Idle;

    private bool isLineTyping = false;
    public bool isPlayingFailureDialogue = false;
    private bool playFillerBeep = false;
    private bool justBeeped = false;

    // Single flag for dialogue skip
    public bool skipDialogueRequested = false;

    // Track if last dialogue was skipped (used instead of out parameter)
    private bool lastDialogueWasSkipped = false;

    public int currentSequenceIndex = -1;
    public int currentDialogueIndex = 0;
    private int currentFailureIndex = -1;

    public int numDialoguesQueued { get; private set; } = 0;

    //_______________________________________________________________________________________________________________
    // Events for external listeners (e.g. tutorial manager) - can be expanded as needed
    public event Action<int> OnDialogueLineEndBrokenDoor;
    public event Action<int> OnDialogueEndTutorial;

    private void Awake()
    {
        //if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        //Instance = this;
        //DontDestroyOnLoad(gameObject);

        if (dialogueCanvasGroup == null && dialogueCanvas != null)
            dialogueCanvasGroup = dialogueCanvas.GetComponent<CanvasGroup>();

        if (dialogueCanvas != null) dialogueCanvas.enabled = false;
        if (dialogueCanvasGroup != null) dialogueCanvasGroup.alpha = 0f;
        playerController = new PlayerController();
        ClearTextsImmediate();
    }

    private void OnEnable() => playerController.Dialogue.ContinueDialogue.Enable();
    private void OnDisable() => playerController.Dialogue.ContinueDialogue.Disable();

    private void Start()
    {
        if (player != null) playerManager = player.GetComponent<ZeroGravity>();
        sourceList.Add(defaultAudioSource);
    }

    private void Update()
    {
        // Restore references if player is null (e.g., after scene reload)
        if (persistantManager == null)
        {
            persistantManager = FindFirstObjectByType<PersistantManager>();
            player = persistantManager.GetComponentInChildren<ZeroGravity>()?.gameObject;
            if (player != null) playerManager = player.GetComponent<ZeroGravity>();

            dialogueCanvas = GameObject.Find(dialogueCanvasObj)?.GetComponent<Canvas>();
            if (dialogueCanvas != null)
            {
                dialogueCanvasGroup = dialogueCanvas.GetComponent<CanvasGroup>();
                nameTextUI = GameObject.Find(nameTextObj).GetComponent<TextMeshProUGUI>();
                dialogueTextUI = GameObject.Find(dialogueTextObj).GetComponent<TextMeshProUGUI>();
            }

            if (dialogueCanvasGroup == null && dialogueCanvas != null)
                dialogueCanvasGroup = dialogueCanvas.GetComponent<CanvasGroup>();
        }
        if (playerController.Dialogue.ContinueDialogue.triggered)
        {
            // Skip requests the current Dialogue entry to end immediately
            if (IsDialogueActive)
            {
                skipDialogueRequested = true;
            }
        }
        if(tutorialManager == null)
        {
            tutorialManager = FindFirstObjectByType<TutorialManager>();
            //Debug.Log("found tutorial manager");
        }
    }

    #region Public API

    public void StartDialogueSequence(int sequenceIndex, float delayTime = 0f)
    {
        if (sequenceIndex < 0 || sequenceIndex >= dialogueSequences.Length) return;

        //Debug.Log("Queueing dialog at sequence " + sequenceIndex);
        sequenceQueue.Enqueue(sequenceIndex);
        numDialoguesQueued++;

        if (currentState == DialogueState.Idle)
        {
            displayCoroutine = StartCoroutine(ProcessDialogueQueue(delayTime));
        }
    }

    public void StartFailureDialogue(int failureIndex)
    {
        StartCoroutine(PlayFailureDialogueRoutine(failureIndex));
    }

    public void ForceStopAll(bool clearQueue = true)
    {
        if (displayCoroutine != null) { StopCoroutine(displayCoroutine); displayCoroutine = null; }
        if (typewriterCoroutine != null) { StopCoroutine(typewriterCoroutine); typewriterCoroutine = null; }
        if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }

        currentState = DialogueState.Idle;
        isLineTyping = false;
        isPlayingFailureDialogue = false;
        skipDialogueRequested = false;

        sequenceQueue.Clear();
        if (clearQueue) numDialoguesQueued = 0;

        //stop all potential audio sources
        foreach(AudioSource source in sourceList)
        {
            if (source != null && source.isPlaying) source.Stop();
        }
        
        ClearTextsImmediate();
        HideCanvasImmediate();
    }

    public void RestartCurrentDialogue(float delayTime = 0f)
    {
        if (displayCoroutine != null) { StopCoroutine(displayCoroutine); displayCoroutine = null; }
        if (typewriterCoroutine != null) { StopCoroutine(typewriterCoroutine); typewriterCoroutine = null; }

        isLineTyping = false;
        isPlayingFailureDialogue = false;
        skipDialogueRequested = false;

        currentDialogueIndex = 0;

        if (currentSequenceIndex >= 0 && currentSequenceIndex < dialogueSequences.Length)
        {
            sequenceQueue.Enqueue(currentSequenceIndex);
            numDialoguesQueued++;
            if (currentState == DialogueState.Idle)
                displayCoroutine = StartCoroutine(ProcessDialogueQueue(delayTime));
        }
    }

    public void SkipTutorial()
    {
        if (sfxSource && skipSfxClip) sfxSource.PlayOneShot(skipSfxClip);

        // Stop the typewriter if currently typing
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        // Stop audio
        foreach (AudioSource source in sourceList)
        {
            if (source != null && source.isPlaying) source.Stop();
        }

        // Clear any skip requests
        skipDialogueRequested = false;
        isLineTyping = false;

        // Clear the current sequence queue (tutorial sequences)
        sequenceQueue.Clear();
        numDialoguesQueued = 0;

        // Stop the display coroutine cleanly so the next sequence can start
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }

        // Set state to idle so new sequences can be queued immediately
        currentState = DialogueState.Idle;

        // Clear text but keep canvas visible and faded in for next dialogue
        ClearTextsImmediate();

        // Don't hide or fade out the canvas - leave it ready for the next sequence
    }


    #endregion

    #region Queue Processing & Display Flow

    private IEnumerator ProcessDialogueQueue(float startDelay = 0f)
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        while (sequenceQueue.Count > 0)
        {
            //Debug.Log("Starting next dialogue in the queue");
            int seqIndex = sequenceQueue.Dequeue();
            currentSequenceIndex = seqIndex;
            numDialoguesQueued = Mathf.Max(0, numDialoguesQueued - 1);

            yield return StartCoroutine(PlaySequenceRoutine(seqIndex));
            //Debug.Log("Dialogue at index " + seqIndex + " has ended.");
            yield return null;
        }

        displayCoroutine = null;
        currentState = DialogueState.Idle;
    }

    private IEnumerator PlaySequenceRoutine(int sequenceIndex)
    {
        if (sequenceIndex < 0 || sequenceIndex >= dialogueSequences.Length) yield break;
        DialogueSequence sequence = dialogueSequences[sequenceIndex];

        //keep track of the source being used for this sequence
        AudioSource sequenceSource;
        if (sequence.audioSource)
        {
            sequenceSource = sequence.audioSource;
        }
        else
        {
            sequenceSource = defaultAudioSource;
        }

        if(!sourceList.Contains(sequenceSource))
        {
            sourceList.Add(sequenceSource);
        }

        currentDialogueIndex = 0;

        ClearDialogueTextBeforeShow();
        ShowCanvasImmediate();
        yield return StartCoroutine(FadeCanvas(true));

        currentState = DialogueState.Playing;
        //Debug.Log("Dialogue sequence is starting with length " + sequence.dialogues.Length);
        while (currentDialogueIndex < sequence.dialogues.Length)
        {
            //Debug.Log("Dialogue at index " + currentDialogueIndex + " waiting while paused");
            yield return new WaitWhile(() => currentState == DialogueState.Paused);

            Dialogue d = sequence.dialogues[currentDialogueIndex];

            
            // Skip if tutorial global skip and this dialogue wanted to be skipped
            if (tutorialManager != null && tutorialManager.IsTutorialSkipped && d.skipWithTutorial && sequenceIndex == 0)
            {
                currentDialogueIndex++;
                continue;
            }

            //Debug.Log("Dialogue at index " + currentDialogueIndex + " about to play the dialogue");
            // Play the dialogue and check if it was skipped
            yield return StartCoroutine(PlaySingleDialogue(d, sequenceSource));
            bool wasSkipped = lastDialogueWasSkipped;

            // check for the last line of the dining room sequence before alan breaks the door, and fire an event to trigger the door break
            bool isLastEntryBeforeDoorBreak = currentDialogueIndex == sequence.dialogues.Length - 2;
            if (isLastEntryBeforeDoorBreak) OnDialogueLineEndBrokenDoorSafe(currentSequenceIndex);

            // Handle tutorial advancement
            if (d.advancesTutorial && tutorialManager != null)
            {
                //Debug.Log("Dialogue at index " + currentDialogueIndex + " is progressing tutorial and awaiting completion");
                tutorialManager.ProgressTutorial();
                yield return new WaitUntil(() => tutorialManager.TutorialStepCompleted());
                //Debug.Log("Dialogue at index " + currentDialogueIndex + " detects tutorial step completion");
            }

            // Wait for audio to finish if not skipped
            if (!wasSkipped && sequenceSource != null && sequenceSource.clip != null)
            {
                
                yield return new WaitWhile(() => sequenceSource.isPlaying);
                //Debug.Log("Dialogue at index " + currentDialogueIndex + " successfully waiting for the audio source to stop playing");
            }

            // Post-dialogue delay
            if (!wasSkipped)
            {
                yield return new WaitForSeconds(d.delayBetweenDialogues);
            }

            //Debug.Log("Dialogue at index " + currentDialogueIndex + " is now increasing index");
            currentDialogueIndex++;

            //check to ensure that we aren't skipping the next dialogue before it starts
            if(skipDialogueRequested)
            {
                skipDialogueRequested = false;
            }
        }

        //Debug.Log("Dialogue at sequence " + sequenceIndex + " is has completed");
        OnDialogueEndTutorialSafe(currentSequenceIndex);
        
        if (sequenceQueue.Count <= 0)
        {
            yield return StartCoroutine(FadeCanvas(false));
            HideCanvasImmediate();
        }
    }

    /// <summary>
    /// Plays a single dialogue entry with clean skip handling.
    /// Used by both normal sequences and failure dialogues.
    /// Sets lastDialogueWasSkipped to indicate if it was skipped.
    /// </summary>
    private IEnumerator PlaySingleDialogue(Dialogue d, AudioSource audioSource)
    {
        lastDialogueWasSkipped = false;

        // Set name
        if (nameTextUI != null) nameTextUI.text = d.characterName ?? "";

        // Setup audio
        if (d.audioClip != null && audioSource != null)
        {
            audioSource.clip = d.audioClip;
            if (audioSource.isActiveAndEnabled)
                audioSource.Play();
            playFillerBeep = false;
        }
        else
        {
            playFillerBeep = true;
        }

        // Calculate typewriter speed
        float typeSpeed = CalculateTypeSpeed(d);
        //Debug.Log("Typespeed: " + typeSpeed);

        // Play through all lines
        for (int lineIndex = 0; lineIndex < d.dialogueLines.Length; lineIndex++)
        {
            yield return new WaitWhile(() => currentState == DialogueState.Paused);

            // Check for skip BEFORE starting the line
            if (skipDialogueRequested)
            {
                lastDialogueWasSkipped = true;
                HandleDialogueSkip(d);
                yield break; // Exit immediately
            }

            string line = d.dialogueLines[lineIndex];

            // Type the line
            if (typewriterCoroutine != null) { StopCoroutine(typewriterCoroutine); typewriterCoroutine = null; }
            typewriterCoroutine = StartCoroutine(TypewriterEffect(line, typeSpeed, audioSource));
            yield return typewriterCoroutine;
            typewriterCoroutine = null;

            // Check for skip AFTER line completes
            if (skipDialogueRequested)
            {
                lastDialogueWasSkipped = true;
                HandleDialogueSkip(d);
                yield break; // Exit immediately
            }

            // Small buffer between lines
            if (lineIndex < d.dialogueLines.Length - 1)
            {
                yield return new WaitForSeconds(d.delayBetweenLines);
            }
        }

        if(audioSource.isPlaying)
        {
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
    }

    /// <summary>
    /// Handles what happens when a dialogue is skipped.
    /// Stops audio, plays SFX, and shows final line if tutorial is coming or if it's the last dialogue.
    /// </summary>
    private void HandleDialogueSkip(Dialogue d)
    {
        // Stop audio and play skip SFX
        //stop all potential audio sources
        foreach (AudioSource source in sourceList)
        {
            if (source != null && source.isPlaying) source.Stop();
        }

        if (sfxSource && skipSfxClip)
            if (sfxSource.isActiveAndEnabled)
                sfxSource.PlayOneShot(skipSfxClip);

        // Check if this is the last dialogue in the sequence
        bool isLastDialogue = false;
        if (currentSequenceIndex >= 0 && currentSequenceIndex < dialogueSequences.Length)
        {
            isLastDialogue = currentDialogueIndex >= dialogueSequences[currentSequenceIndex].dialogues.Length - 1;
        }

        // Show final line if tutorial task is coming OR if it's the last dialogue, otherwise clear
        if ((d.advancesTutorial || isLastDialogue) && d.dialogueLines.Length > 0)
        {
            // Keep the last line visible for tutorial context or natural fade-out
            dialogueTextUI.text = d.dialogueLines[d.dialogueLines.Length - 1];
        }
        else
        {
            // Clear text for mid-sequence non-tutorial dialogues
            dialogueTextUI.text = "";
        }

        // Clear the skip flag
        skipDialogueRequested = false;
    }

    private float CalculateTypeSpeed(Dialogue d)
    {
        float typeSpeed = defaultTextSpeed;

        if (d.audioClip != null && d.dialogueLines != null)
        {
            int totalLength = 0;
            foreach (var l in d.dialogueLines) totalLength += l.Length;
            if (totalLength > 0)
                typeSpeed = Mathf.Max(0.001f, d.audioClip.length / ((float)totalLength * textSpeedMultiplier));
        }

        return typeSpeed;
    }

    #endregion

    #region Failure Dialogue Handling

    /// <summary>
    /// Plays a failure dialogue that interjects into the current sequence.
    /// Waits for current line to finish, pauses main sequence, plays failure, then resumes.
    /// </summary>
    public IEnumerator PlayFailureDialogueRoutine(int index)
    {
        if (failureDialogues == null || index < 0 || index >= failureDialogues.dialogues.Length) yield break;

        // Wait until any currently typing line finishes (don't interrupt mid-line)
        yield return new WaitUntil(() => !isLineTyping);

        // Wait if another failure is already playing
        yield return new WaitUntil(() => !isPlayingFailureDialogue);


        // Pause the main sequence
        DialogueState prevState = currentState;
        currentState = DialogueState.Paused;
        isPlayingFailureDialogue = true;
        currentFailureIndex = index;

        // Ensure canvas is visible
        ClearDialogueTextBeforeShow();
        ShowCanvasImmediate();
        yield return StartCoroutine(FadeCanvas(true));

        //keep track of the source being used for this sequence
        AudioSource sequenceSource;
        if (failureDialogues.audioSource)
        {
            sequenceSource = failureDialogues.audioSource;
        }
        else
        {
            sequenceSource = defaultAudioSource;
        }

        if (!sourceList.Contains(sequenceSource))
        {
            sourceList.Add(sequenceSource);
        }

        Dialogue d = failureDialogues.dialogues[index];

        // If this failure should skip the success dialogue in the main sequence
        if (d.incrementsDialogue)
        {
            int maxIndex = (currentSequenceIndex >= 0 && currentSequenceIndex < dialogueSequences.Length)
                ? dialogueSequences[currentSequenceIndex].dialogues.Length
                : currentDialogueIndex;
            //Debug.Log("Max index calculated as " + maxIndex);
            currentDialogueIndex = Mathf.Min(currentDialogueIndex + 1, maxIndex);
            //Debug.Log("Current dialogue index now " + currentDialogueIndex);
        }

        // Play the failure dialogue using the same PlaySingleDialogue method
        yield return StartCoroutine(PlaySingleDialogue(d, sequenceSource));
        bool wasSkipped = lastDialogueWasSkipped;

        // Handle tutorial advancement if this failure dialogue advances the tutorial
        if (d.advancesTutorial && tutorialManager != null)
        {
            //Debug.Log("Failure dialogue progresses the tutorial");
            tutorialManager.CompleteStep();
            tutorialManager.ProgressTutorial();
        }

        // Post-dialogue delay
        if (!wasSkipped)
        {
            //Debug.Log("Waiting on delay between dialogues. Delay: " + d.delayBetweenDialogues);
            yield return new WaitForSeconds(d.delayBetweenDialogues);
        }


        //Debug.Log("Failure Dialogue now ending and resetting states");
        // Resume main sequence
        isPlayingFailureDialogue = false;
        currentFailureIndex = -1;
        currentState = prevState == DialogueState.Playing ? DialogueState.Playing : DialogueState.Idle;

        // If nothing is playing after the failure, fade out and hide
        if (currentState == DialogueState.Idle && sequenceQueue.Count == 0)
        {
            yield return new WaitForSeconds(0.3f); // Brief pause before fading
            yield return StartCoroutine(FadeCanvas(false));
            HideCanvasImmediate();
        }
    }

    #endregion

    #region Typewriter & Helpers

    private IEnumerator TypewriterEffect(string fullText, float charDelay, AudioSource audioSource)
    {
        isLineTyping = true;
        dialogueTextUI.text = "";
        justBeeped = false;

        for (int i = 0; i < fullText.Length; i++)
        {
            if (currentState == DialogueState.Paused) break;

            // Check for skip during typing - stop immediately at current character
            if (skipDialogueRequested)
            {
                isLineTyping = false;
                yield break; // Exit typewriter, skip will be handled in PlaySingleDialogue
            }

            dialogueTextUI.text += fullText[i];

            if (playFillerBeep && fillerLineBeep != null && audioSource != null)
            {
                if (!justBeeped)
                {
                    audioSource.PlayOneShot(fillerLineBeep);
                    justBeeped = true;
                }
                else justBeeped = false;
            }

            yield return new WaitForSeconds(charDelay);
        }

        isLineTyping = false;
    }

    private IEnumerator FadeCanvas(bool fadeIn)
    {
        if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }
        fadeCoroutine = StartCoroutine(FadeCanvasGroupRoutine(dialogueCanvasGroup, fadeIn ? 0f : dialogueCanvasGroup.alpha, fadeIn ? 1f : 0f, fadeDuration));
        yield return fadeCoroutine;
        fadeCoroutine = null;
    }

    private IEnumerator FadeCanvasGroupRoutine(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        currentState = DialogueState.Fading;

        float t = 0f;
        startAlpha = cg != null ? cg.alpha : startAlpha;

        while (t < duration)
        {
            if (cg != null) cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t / duration);
            t += Time.deltaTime;
            yield return null;
        }

        if (cg != null) cg.alpha = endAlpha;

        if (Mathf.Approximately(endAlpha, 0f))
        {
            ClearTextsImmediate();
            HideCanvasImmediate();
        }

        currentState = sequenceQueue.Count > 0 || displayCoroutine != null ? DialogueState.Playing : DialogueState.Idle;
    }

    private void ClearDialogueTextBeforeShow()
    {
        if (dialogueTextUI != null) dialogueTextUI.text = "";
        if (nameTextUI != null) nameTextUI.text = "";
    }

    private void ClearTextsImmediate()
    {
        if (dialogueTextUI != null) dialogueTextUI.text = "";
        if (nameTextUI != null) nameTextUI.text = "";
    }

    private void ShowCanvasImmediate()
    {
        if (dialogueCanvas != null) dialogueCanvas.enabled = true;
        if (dialogueCanvasGroup != null) dialogueCanvasGroup.alpha = 0f;
    }

    private void HideCanvasImmediate()
    {
        if (dialogueCanvas != null) dialogueCanvas.enabled = false;
        if (dialogueCanvasGroup != null) dialogueCanvasGroup.alpha = 0f;
    }

    private void OnDialogueEndTutorialSafe(int sequenceIndex)
    {
        try
        {
            OnDialogueEndTutorial?.Invoke(sequenceIndex);
        }
        catch (Exception e)
        {
            Debug.LogWarning("OnDialogueEnd handler threw: " + e);
        }
    }

    private void OnDialogueLineEndBrokenDoorSafe(int sequenceIndex)
    {
        try
        {
            OnDialogueLineEndBrokenDoor?.Invoke(sequenceIndex);
        }
        catch (Exception e)
        {
            Debug.LogWarning("OnDialogueLineEndBrokenDoor handler threw: " + e);
        }
    }

    #endregion
}