using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EquipmentMenuUI : MonoBehaviour
{
    private GameSession _session;
    private ProgressionManager _progression;
    private GameObject _menu;
    private RectTransform _panel;
    private TMP_Text _summary;
    private TMP_Text _menuSummary;
    private readonly Button[] _levels = new Button[ProgressionRules.LevelCount];
    private readonly Button[] _bows = new Button[ProgressionRules.Bows.Length];
    private readonly Button[] _arrows = new Button[ProgressionRules.Arrows.Length];

    public void Initialize(GameSession session, ProgressionManager progression, GameObject menu)
    {
        if (_panel != null || menu == null) return;
        Canvas canvas = menu.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        _session = session;
        _progression = progression;
        _menu = menu;
        Button reference = menu.GetComponentInChildren<Button>(true);
        _panel = Rect("Niveaux et equipement", canvas.transform, 0, 9, 1100, 800);
        _panel.localScale = Vector3.one * 0.02f;
        MatchDepth(_panel, reference);
        _panel.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.065f, 0.11f, 1f);
        Label(_panel, "NIVEAUX ET EQUIPEMENT", 0, 345, 1000, 60, 36);
        for (int i = 0; i < ProgressionRules.LevelCount; i++)
        {
            int index = i;
            float x = (i - (ProgressionRules.LevelCount - 1) / 2f) * 265;
            _levels[i] = Button(_panel, $"Niveau {i + 1}", x, 235, 255, 110,
                () => { _progression.SelectLevel(index + 1); Refresh(); });
            _bows[i] = Button(_panel, ProgressionRules.Bows[i].Name, x, 60, 255, 125,
                () => { _progression.SelectBow(index); Refresh(); });
            _arrows[i] = Button(_panel, ProgressionRules.Arrows[i].Name, x, -130, 255, 125,
                () => { _progression.SelectArrow(index); Refresh(); });
        }
        Label(_panel, "ARCS - puissance de tir", 0, 150, 1000, 40, 26);
        Label(_panel, "FLECHES - vitesse de depart", 0, -40, 1000, 40, 26);
        _summary = Label(_panel, "", 0, -250, 1000, 80, 23);
        Button(_panel, "Retour au menu", 0, -345, 420, 65, Close);

        Button access = Button(menu.transform, "Niveaux / equipement", 10, 6.5f, 10, 1.5f, Open);
        access.GetComponentInChildren<TMP_Text>().fontSize = 0.5f;
        MatchDepth(access.transform, reference);
        _menuSummary = Label(menu.transform, "", 0, 9, 20, 1.5f, 0.5f);
        MatchDepth(_menuSummary.transform, reference);
        Refresh();
        Hide();
    }

    public void Refresh()
    {
        if (_panel == null) return;
        ProgressionRules rules = _progression.Rules;
        for (int i = 0; i < ProgressionRules.LevelCount; i++)
        {
            bool unlocked = i + 1 <= rules.UnlockedLevel;
            _levels[i].interactable = unlocked;
            string behavior = i == 0 ? "Cibles fixes" : i == 1 ? "Cibles mobiles" :
                i == 2 ? "Apparitions" : "Mobiles + apparitions";
            _levels[i].GetComponentInChildren<TMP_Text>().text = $"Niveau {i + 1} - {_session.GetLevelGoal(i + 1)} points\n{behavior}\n" +
                (!unlocked ? $"Reussir le niveau {i}" : rules.Data.selectedLevel == i + 1 ? "CHOISI" : "Choisir");
            Describe(_bows[i], ProgressionRules.Bows[i], rules.IsUnlocked(ProgressionRules.Bows, i), rules.Data.selectedBow == i);
            Describe(_arrows[i], ProgressionRules.Arrows[i], rules.IsUnlocked(ProgressionRules.Arrows, i), rules.Data.selectedArrow == i);
        }
        _summary.text = string.IsNullOrEmpty(_progression.SaveWarning)
            ? "Les choix s'appliquent au prochain tir.\nRetourne au menu pour choisir une difficulte ou t'entrainer."
            : _progression.SaveWarning;
        _menuSummary.text = $"Niveau {rules.Data.selectedLevel} - Objectif : {_session.GetLevelGoal(rules.Data.selectedLevel)} points\n{_progression.EquipmentLabel}";
    }

    private static void Describe(Button button, EquipmentSpec spec, bool unlocked, bool selected)
    {
        button.interactable = unlocked;
        button.GetComponentInChildren<TMP_Text>().text = $"{spec.Name}\nx{spec.Multiplier:0.##}\n" +
            (!unlocked ? $"Reussir le niveau {spec.RequiredCompletedLevel}" : selected ? "CHOISI" : "Choisir");
    }
    public void Open()
    {
        if (_session.State != GameSession.SessionState.Menu || _panel == null) return;
        Refresh();
        _menu.SetActive(false);
        _panel.gameObject.SetActive(true);
        _panel.SetAsLastSibling();
    }
    public void Hide() { if (_panel != null) _panel.gameObject.SetActive(false); }
    public void Close() { Hide(); _menu.SetActive(_session.State == GameSession.SessionState.Menu); }

    private static void MatchDepth(Transform element, Button reference)
    {
        if (reference == null) return;
        Vector3 position = element.localPosition;
        position.z = element.parent.InverseTransformPoint(reference.transform.position).z;
        element.localPosition = position;
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
    private static TMP_Text Label(Transform parent, string text, float x, float y, float width, float height, float size)
    {
        var label = Rect("Texte", parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.richText = false;
        return label;
    }
    private static Button Button(Transform parent, string text, float x, float y, float width, float height, UnityAction action)
    {
        RectTransform rect = Rect(text, parent, x, y, width, height);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.24f, 0.34f, 1f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        button.onClick.AddListener(action);
        Label(rect, text, 0, 0, width, height, 22);
        return button;
    }
}
