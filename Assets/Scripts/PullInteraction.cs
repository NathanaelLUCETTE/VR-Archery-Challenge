using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PullInteraction : XRBaseInteractable
{
    public static event Action<float> PullActionReleased;

    public Transform start, end;
    public GameObject notch;

    public float pullAmount { get; private set; } = 0.0f;

    private LineRenderer _lineRenderer;
    private IXRSelectInteractor pullingInteractor = null;

    [SerializeField] private AudioSource releaseAudio;
    [SerializeField] private AudioSource tensionAudio;

    private float previousPullAmount;
    private float tensionSoundUntil;

    protected override void Awake()
    {
        base.Awake();
        _lineRenderer = GetComponent<LineRenderer>();
    }

    public void SetPullInteractor(SelectEnterEventArgs args)
    {
        pullingInteractor = args.interactorObject;
    }

    public void Release()
    {
        if (tensionAudio != null)
            tensionAudio.Stop();

        tensionSoundUntil = 0f;
        previousPullAmount = 0f;
        PullActionReleased?.Invoke(pullAmount);
        pullingInteractor = null;
        pullAmount = 0f;

        notch.transform.localPosition = new Vector3(
            notch.transform.localPosition.x,
            notch.transform.localPosition.y,
            0f
        );
        PlayReleaseSound();
        UpdateString();

        
    }

    public override void ProcessInteractable(
    XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            if (isSelected)
            {
                Vector3 pullPosition = pullingInteractor.transform.position;
                pullAmount = CalculatePull(pullPosition);

                UpdateString();
                HapticFeedback();
            }
        }
    }

    private float CalculatePull(Vector3 pullPosition)
    {
        Vector3 pullDirection = pullPosition - start.position;
        Vector3 targetDirection = end.position - start.position;
        float maxLength = targetDirection.magnitude;

        targetDirection.Normalize();
        float pullValue =
            Vector3.Dot(pullDirection, targetDirection) / maxLength;

        return Mathf.Clamp(pullValue, 0, 1);
    }

    private void UpdateString()
    {
        Vector3 linePosition =
            Vector3.forward *
            Mathf.Lerp(
                start.transform.localPosition.z,
                end.transform.localPosition.z,
                pullAmount
            );

        notch.transform.localPosition = new Vector3(
            notch.transform.localPosition.x,
            notch.transform.localPosition.y,
            linePosition.z + .2f
        );

        _lineRenderer.SetPosition(1, linePosition);
    }
    private void HapticFeedback()
    {
        if (pullingInteractor == null)
            return;

        // The player usually sits on the controller, a parent of the interactor
        var hapticPlayer = pullingInteractor.transform.GetComponentInParent<HapticImpulsePlayer>();

        if (hapticPlayer != null)
        {
            hapticPlayer.SendHapticImpulse(pullAmount, 0.1f);
        }
    }
    private void PlayReleaseSound()
    {
        releaseAudio.Stop();
        releaseAudio.Play();
    }
    private void LateUpdate()
    {
        if (tensionAudio == null)
            return;

        // Joue lorsque la tension augmente.
        bool pulling = isSelected &&
                       pullAmount > previousPullAmount + 0.0005f;

        previousPullAmount = pullAmount;

        if (pulling)
            tensionSoundUntil = Time.time + 0.1f;

        // Le petit délai évite les coupures entre deux mouvements.
        bool shouldPlay = isSelected && Time.time < tensionSoundUntil;

        if (shouldPlay && !tensionAudio.isPlaying)
            tensionAudio.Play();
        else if (!shouldPlay && tensionAudio.isPlaying)
            tensionAudio.Stop();
    }
}