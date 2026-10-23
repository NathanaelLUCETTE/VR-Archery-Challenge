using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

// Player hit points for the Dragon mode, with a red flash and controller rumble on damage.
[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    public const int Max = 100;

    public int Current { get; private set; } = Max;
    public bool IsDead => Current <= 0;
    public event Action Changed;

    private Image _flash;
    private float _flashAlpha;

    public void ResetHealth()
    {
        Current = Max;
        _flashAlpha = 0f;
        Changed?.Invoke();
    }

    public void Damage(int amount)
    {
        if (IsDead || amount <= 0) return;
        Current = Mathf.Max(0, Current - amount);
        _flashAlpha = 0.55f;
        foreach (HapticImpulsePlayer haptics in FindObjectsByType<HapticImpulsePlayer>())
            haptics.SendHapticImpulse(0.9f, 0.3f);
        Changed?.Invoke();
    }

    private void Update()
    {
        if (_flashAlpha <= 0f && _flash == null) return;
        EnsureFlash();
        if (_flash == null) return;
        _flashAlpha = Mathf.MoveTowards(_flashAlpha, 0f, Time.deltaTime * 0.9f);
        _flash.color = new Color(0.85f, 0.05f, 0f, _flashAlpha);
        _flash.enabled = _flashAlpha > 0.001f;
    }

    // A red sheet just in front of the eyes, parented to the headset camera.
    private void EnsureFlash()
    {
        if (_flash != null) return;
        Camera eyes = Camera.main;
        if (eyes == null) return;
        var canvasObject = new GameObject("Flash degats", typeof(RectTransform));
        canvasObject.transform.SetParent(eyes.transform, false);
        canvasObject.transform.localPosition = new Vector3(0f, 0f, 0.3f);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        var rect = (RectTransform)canvasObject.transform;
        rect.sizeDelta = new Vector2(2f, 2f);
        var image = new GameObject("Rouge", typeof(RectTransform)).AddComponent<Image>();
        image.transform.SetParent(canvasObject.transform, false);
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.sizeDelta = Vector2.zero;
        image.raycastTarget = false;
        _flash = image;
    }
}
