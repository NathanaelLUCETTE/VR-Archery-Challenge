using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ResetDataMenuUI : MonoBehaviour
{
    private GameSession _session;
    private Button _button;
    private TMP_Text _label;
    private float _confirmUntil;

    public void Initialize(GameSession session, GameObject menu)
    {
        if (_button != null || menu == null) return;
        _session = session;
        Button reference = menu.GetComponentInChildren<Button>(true);
        var rect = new GameObject("Reinitialiser les donnees", typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = menu.layer;
        rect.SetParent(menu.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(6f, 0.9f);
        if (reference != null)
        {
            Vector3 position = rect.localPosition;
            position.z = menu.transform.InverseTransformPoint(reference.transform.position).z;
            rect.localPosition = position;
        }
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.45f, 0.13f, 0.1f, 1f);
        _button = rect.gameObject.AddComponent<Button>();
        _button.targetGraphic = image;
        var navigation = _button.navigation;
        navigation.mode = Navigation.Mode.None;
        _button.navigation = navigation;
        _button.onClick.AddListener(ResetClicked);
        var textRect = new GameObject("Texte", typeof(RectTransform)).GetComponent<RectTransform>();
        textRect.gameObject.layer = menu.layer;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        _label = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        _label.text = "Reinitialiser";
        _label.fontSize = 0.4f;
        _label.color = Color.white;
        _label.alignment = TextAlignmentOptions.Center;
        _label.raycastTarget = false;
    }

    private void Update()
    {
        if (_confirmUntil <= 0f) return;
        if (Time.unscaledTime >= _confirmUntil || !_button.gameObject.activeInHierarchy)
        {
            _confirmUntil = 0f;
            _label.text = "Reinitialiser";
        }
    }

    private void ResetClicked()
    {
        if (_session.State != GameSession.SessionState.Menu) return;
        if (_confirmUntil <= Time.unscaledTime)
        {
            _confirmUntil = Time.unscaledTime + 5f;
            _label.text = "Tout effacer ? Confirmer\nScores, progression et volume";
            return;
        }
        _confirmUntil = 0f;
        try
        {
            GetComponent<LeaderboardUI>().ResetSavedScores();
            GetComponent<ProgressionManager>().ResetSavedProgression();
            GetComponent<VolumeMenuUI>().ResetSavedVolume();
            _session.ReturnToMenu();
            _label.text = "Donnees effacees";
        }
        catch (Exception exception)
        {
            _label.text = "Effacement incomplet : reessayer";
            Debug.LogError("Reinitialisation incomplete : " + exception.Message, this);
        }
    }
}
