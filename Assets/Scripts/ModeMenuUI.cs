using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Adds the Hardcore / Extreme / Dragon buttons and the trajectory switch to the VR menus.
[DisallowMultipleComponent]
public class ModeMenuUI : MonoBehaviour
{
    private readonly System.Collections.Generic.List<TMP_Text> _trajectoryLabels = new System.Collections.Generic.List<TMP_Text>();
    private bool _initialized;

    public void Initialize(GameSession session, GameObject menu, GameObject gameplay)
    {
        if (_initialized || menu == null) return;
        _initialized = true;

        // Clone a styled scene button so the new modes look like Facile / Normal / Difficile.
        Button style = FindButton(menu.transform, "Facile");
        if (style != null)
        {
            ModeButton(style, menu.transform, "HARDCORE", new Vector2(-6.6f, 6.7f), new Color(0.48f, 0.2f, 0.7f), session.StartHardcore);
            ModeButton(style, menu.transform, "EXTREME", new Vector2(0f, 6.7f), new Color(0.85f, 0.42f, 0.08f), session.StartExtreme);
            ModeButton(style, menu.transform, "DRAGON", new Vector2(6.6f, 6.7f), new Color(0.45f, 0.05f, 0.05f), session.StartDragon);
        }

        TrajectoryButton(menu.transform, new Vector2(5f, 3.5f), new Vector2(9f, 1.2f), 0.5f);
        if (gameplay != null) TrajectoryButton(gameplay.transform, new Vector2(-2.5f, 0.3f), new Vector2(5f, 1.1f), 0.42f);
        TrajectorySetting.Changed += RefreshTrajectory;
        RefreshTrajectory();
    }

    private void OnDestroy() => TrajectorySetting.Changed -= RefreshTrajectory;

    private static Button FindButton(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private static void ModeButton(Button style, Transform parent, string label, Vector2 position, Color color, UnityAction action)
    {
        Button button = Instantiate(style, parent);
        button.name = label;
        var rect = (RectTransform)button.transform;
        Vector3 local = rect.anchoredPosition3D;
        rect.anchoredPosition3D = new Vector3(position.x, position.y, local.z);
        // Replacing the event drops the copied persistent "StartEasy" call.
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
        button.targetGraphic.color = color;
        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null) text.text = label;
    }

    private void TrajectoryButton(Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        Button reference = parent.GetComponentInChildren<Button>(true);
        var rect = new GameObject("Trajectoire", typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        if (reference != null)
        {
            Vector3 local = rect.localPosition;
            local.z = parent.InverseTransformPoint(reference.transform.position).z;
            rect.localPosition = local;
        }
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.24f, 0.34f, 1f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        button.onClick.AddListener(() => TrajectorySetting.Enabled = !TrajectorySetting.Enabled);

        var textRect = new GameObject("Texte", typeof(RectTransform)).GetComponent<RectTransform>();
        textRect.gameObject.layer = parent.gameObject.layer;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var label = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        _trajectoryLabels.Add(label);
    }

    private void RefreshTrajectory()
    {
        foreach (TMP_Text label in _trajectoryLabels)
            if (label != null) label.text = TrajectorySetting.Enabled ? "Trajectoire : OUI" : "Trajectoire : NON";
    }
}
