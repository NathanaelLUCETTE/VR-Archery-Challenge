using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class VolumeMenuUI : MonoBehaviour
{
    private const string SaveKey = "Archery.MasterVolumePercent.v1";
    private int _percent;
    private bool _initialized;
    private readonly List<Controls> _controls = new List<Controls>();
    private sealed class Controls
    {
        public TMP_Text label;
        public Button minus;
        public Button plus;
    }
    private RectTransform _row;
    private TMP_Text _label;
    private Button _minus;
    private Button _plus;

    private void Awake()
    {
        _percent = Mathf.Clamp(PlayerPrefs.GetInt(SaveKey, 100), 0, 100);
        AudioListener.volume = _percent / 100f;
    }

    public void Initialize(GameObject menu, GameObject gameplay)
    {
        if (_initialized) return;
        _initialized = true;
        CreateControls(menu, new Vector2(-5f, 3.5f));
        // Bottom-right corner during play, away from the line of sight to the targets.
        CreateControls(gameplay, new Vector2(6.5f, 0.3f));
        Refresh();
    }

    private void CreateControls(GameObject menu, Vector2 anchor)
    {
        if (menu == null) return;
        Button reference = menu.GetComponentInChildren<Button>(true);
        _row = Rect("Volume general", menu.transform, anchor.x, anchor.y, 10f, 1.2f);
        if (reference != null)
        {
            Vector3 position = _row.localPosition;
            position.z = menu.transform.InverseTransformPoint(reference.transform.position).z;
            _row.localPosition = position;
        }
        _label = Label(_row, "", 5.5f);
        _minus = MakeButton("-", -4f, () => ChangeVolume(-10));
        _plus = MakeButton("+", 4f, () => ChangeVolume(10));
        _controls.Add(new Controls { label = _label, minus = _minus, plus = _plus });
    }

    public void ResetSavedVolume()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        _percent = 100;
        AudioListener.volume = 1f;
        Refresh();
    }

    private void ChangeVolume(int delta)
    {
        _percent = Mathf.Clamp(_percent + delta, 0, 100);
        AudioListener.volume = _percent / 100f;
        Refresh();
        try
        {
            PlayerPrefs.SetInt(SaveKey, _percent);
            PlayerPrefs.Save();
        }
        catch (Exception exception)
        {
            foreach (Controls controls in _controls) controls.label.text += "\nSauvegarde impossible";
            Debug.LogWarning("Volume applique, mais non sauvegarde : " + exception.Message, this);
        }
    }

    private void Refresh()
    {
        foreach (Controls controls in _controls)
        {
            controls.label.text = _percent == 0 ? "Volume : coupe" : $"Volume : {_percent} %";
            controls.minus.interactable = _percent > 0;
            controls.plus.interactable = _percent < 100;
        }
    }

    private Button MakeButton(string text, float x, UnityAction action)
    {
        RectTransform rect = Rect(text, _row, x, 0f, 1.5f, 1.2f);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.24f, 0.34f, 1f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        button.onClick.AddListener(action);
        Label(rect, text, 1.5f);
        return button;
    }

    private static TMP_Text Label(Transform parent, string text, float width)
    {
        var label = Rect("Texte", parent, 0f, 0f, width, 1.2f).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 0.5f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }
}



