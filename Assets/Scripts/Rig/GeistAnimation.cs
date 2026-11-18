using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;
using System.Collections.Generic;

public class GeistAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rig ikRig;
    [SerializeField] private GeistAmbientMotion ambientMotion;

    [Header("Animator")]
    [SerializeField] private string animatedStateTag = "Animated";
    [SerializeField] private int animatorLayer = 0;

    [Header("Blending")]
    [SerializeField] private float blendTime = 0.25f;

    [Header("Test Clip")]
    [SerializeField] private AnimationClip testClip;
    [SerializeField] private bool playTestClip = true;

    public bool IsAnimating { get; private set; }

    private GeistStepper[] steppers;
    private StretchIKBones[] stretchers;
    private GeistLookAt[] lookAts;

    // Tips for each IK target (paired tendrils share one)
    private readonly Dictionary<Transform, List<Transform>> tipsByTarget = new Dictionary<Transform, List<Transform>>();

    // 1 = procedural, 0 = animation
    private float proceduralWeight = 1f;
    private Quaternion ambientSettleStart;

    private PlayableGraph testGraph;
    private AnimationClipPlayable testPlayable;
    private AnimationPlayableOutput testOutput;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        ikRig = GetComponentInChildren<Rig>();
        ambientMotion = GetComponentInParent<GeistAmbientMotion>();
    }

    void Awake()
    {
        steppers = ikRig.GetComponentsInChildren<GeistStepper>();
        stretchers = ikRig.GetComponentsInChildren<StretchIKBones>();
        lookAts = ikRig.GetComponentsInChildren<GeistLookAt>();

        foreach (ChainIKConstraint chain in ikRig.GetComponentsInChildren<ChainIKConstraint>())
        {
            Transform target = chain.data.target;
            if (target == null || chain.data.tip == null) continue;

            if (!tipsByTarget.TryGetValue(target, out List<Transform> tips))
                tipsByTarget[target] = tips = new List<Transform>();
            tips.Add(chain.data.tip);
        }
    }

    void OnDisable()
    {
        if (testGraph.IsValid())
            testGraph.Destroy();
    }

    void Update()
    {
        bool testActive = testClip != null && playTestClip;
        if (testActive)
            EnsureTestGraph();

        bool animating = testActive || IsAnimatedStateActive();
        if (animating != IsAnimating)
        {
            if (animating) BeginAnimation();
            else EndAnimation();
        }

        proceduralWeight = Mathf.MoveTowards(proceduralWeight, IsAnimating ? 0f : 1f, Time.deltaTime / blendTime);

        ikRig.weight = proceduralWeight;
        foreach (StretchIKBones stretcher in stretchers)
            stretcher.Weight = proceduralWeight;

        UpdateTestGraph(testActive);

        if (!IsAnimating) return;

        // Ease the body tilt back to rest for the clip
        if (ambientMotion != null)
            ambientMotion.transform.localRotation = Quaternion.Slerp(ambientMotion.RestRotation, ambientSettleStart, proceduralWeight);

        // Stretching would override the clip, so turn it off once it's faded out
        if (proceduralWeight <= 0f)
            SetEnabled(stretchers, false);
    }

    // The state needs the animatedStateTag tag for the rig to hand over
    public void Play(string stateName)
    {
        animator.CrossFadeInFixedTime(stateName, blendTime, animatorLayer);
    }

    private bool IsAnimatedStateActive()
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            return false;

        // Check the state we're going into so the fade starts with the transition
        AnimatorStateInfo state = animator.IsInTransition(animatorLayer)
            ? animator.GetNextAnimatorStateInfo(animatorLayer)
            : animator.GetCurrentAnimatorStateInfo(animatorLayer);

        return state.IsTag(animatedStateTag);
    }

    private void EnsureTestGraph()
    {
        // Restart if the clip was swapped in the Inspector
        if (testGraph.IsValid() && testPlayable.GetAnimationClip() != testClip)
            testGraph.Destroy();
        if (testGraph.IsValid()) return;

        // Own graph so it works without an Animator Controller. The IK still runs on top of it
        testGraph = PlayableGraph.Create("GeistTestClip");
        testGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        testPlayable = AnimationClipPlayable.Create(testGraph, testClip);
        testOutput = AnimationPlayableOutput.Create(testGraph, "GeistTestClip", animator);
        testOutput.SetSourcePlayable(testPlayable);
        testOutput.SetWeight(0f);
        testGraph.Play();
    }

    private void UpdateTestGraph(bool testActive)
    {
        if (!testGraph.IsValid()) return;

        // Clip fades in as the rig fades out
        testOutput.SetWeight(1f - proceduralWeight);

        // Loop even if Loop Time is off on the clip
        double length = testPlayable.GetAnimationClip().length;
        double time = testPlayable.GetTime();
        if (length > 0 && time >= length)
            testPlayable.SetTime(time % length);

        if (!testActive && proceduralWeight >= 1f)
            testGraph.Destroy();
    }

    private void BeginAnimation()
    {
        IsAnimating = true;

        // These only move IK targets, so they can stop right away
        SetEnabled(steppers, false);
        SetEnabled(lookAts, false);

        if (ambientMotion != null)
        {
            ambientMotion.enabled = false;
            ambientSettleStart = ambientMotion.transform.localRotation;
        }
    }

    private void EndAnimation()
    {
        IsAnimating = false;

        // Start the IK from where the clip left the tips
        SnapTargetsToTips();

        SetEnabled(stretchers, true);
        SetEnabled(lookAts, true);

        foreach (GeistStepper stepper in steppers)
        {
            // Old plants are stale after the clip
            stepper.ReleaseAll();
            stepper.enabled = true;
        }

        if (ambientMotion != null)
            ambientMotion.enabled = true;
    }

    private void SnapTargetsToTips()
    {
        foreach (KeyValuePair<Transform, List<Transform>> pair in tipsByTarget)
        {
            Vector3 position = Vector3.zero;
            foreach (Transform tip in pair.Value)
                position += tip.position;

            pair.Key.position = position / pair.Value.Count;
            pair.Key.rotation = pair.Value[0].rotation;
        }
    }

    private static void SetEnabled(Behaviour[] behaviours, bool enabled)
    {
        foreach (Behaviour behaviour in behaviours)
            behaviour.enabled = enabled;
    }
}
