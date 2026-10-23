using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Generated under the existing XR Canvas: its tracked raycaster handles these buttons too.
public class LeaderboardUI : MonoBehaviour
{
    private GameSession _session;
    private GameObject _menu;
    private GameObject _results;
    private RectTransform _overlay;
    private GameObject _keyboard;
    private GameObject _board;
    private TMP_Text _nameText;
    private TMP_Text _message;
    private TMP_Text _ranking;
    private TMP_Text _heading;
    private Button _saveButton;
    private ScoreRepository _repository;
    private string _name = "";
    private string _savedRound;
    private readonly Color _background = new Color(0.035f, 0.065f, 0.11f, 1f);
    private readonly Color _buttonColor = new Color(0.08f, 0.24f, 0.34f, 1f);

    public void Initialize(GameSession session, GameObject menu, GameObject results)
    {
        if (_overlay != null) return;
        _session = session;
        _menu = menu;
        _results = results;
        Canvas canvas = menu != null ? menu.GetComponentInParent<Canvas>() : null;
        if (canvas == null || results == null)
        {
            Debug.LogWarning("Classement : relier Menu Panel et Results Panel dans Game Session.", this);
            return;
        }
        _repository = new ScoreRepository(Path.Combine(Application.persistentDataPath, "scores.json"), new UnityScoreSerializer());

        // Match the existing panel's local units, while using legible pixel-sized text internally.
        _overlay = Rect("Classement et clavier VR", canvas.transform, 0f, 5f, 1000f, 760f);
        _overlay.localScale = Vector3.one * 0.01f;
        _overlay.gameObject.AddComponent<Image>().color = _background;
        _heading = Label(_overlay, "Titre", "CLASSEMENT", 0, 325, 920, 60, 36);
        _message = Label(_overlay, "Message", "", 0, -235, 920, 65, 24);

        _keyboard = Rect("Clavier AZERTY", _overlay, 0, 0, 1000, 600).gameObject;
        _nameText = Label(_keyboard.transform, "Nom", "Ton nom : _", 0, 235, 900, 60, 32);
        string[] rows = { "AZERTYUIOP", "QSDFGHJKLM", "WXCVBN", "0123456789", "ÉÈÀÇÙ-'" };
        for (int row = 0; row < rows.Length; row++)
        {
            string letters = rows[row];
            for (int column = 0; column < letters.Length; column++)
            {
                string letter = letters[column].ToString();
                Button key = MakeButton(_keyboard.transform, letter, (column - (letters.Length - 1) / 2f) * 88f,
                    155f - row * 65f, 78f, 55f, () => AddCharacter(letter));
                key.name = "Touche " + letter;
            }
        }
        MakeButton(_keyboard.transform, "Espace", -250, -170, 220, 55, () => AddCharacter(" "));
        MakeButton(_keyboard.transform, "Effacer", 0, -170, 220, 55, Backspace);
        _saveButton = MakeButton(_keyboard.transform, "Enregistrer", 250, -170, 220, 55, SaveResult);

        _board = Rect("Meilleurs scores", _overlay, 0, 0, 1000, 600).gameObject;
        _ranking = Label(_board.transform, "Tableau", "", 0, 40, 920, 470, 27);
        _ranking.alignment = TextAlignmentOptions.TopLeft;
        _ranking.richText = false;
        MakeButton(_overlay, "Retour", -230, -315, 300, 65, Close);
        MakeButton(_overlay, "Voir le classement", 180, -315, 420, 65, ShowLeaderboard);

        // These controls use the same units as the user's existing menu buttons.
        Button menuReference = menu.GetComponentInChildren<Button>(true);
        Button resultsReference = results.GetComponentInChildren<Button>(true);
        Button menuButton = MakeButton(menu.transform, "Classement", -5, 6.5f, 8, 1.5f, ShowLeaderboard);
        MatchDepth(menuButton.transform, menuReference);
        menuButton.GetComponentInChildren<TMP_Text>().fontSize = 0.5f;
        Button resultButton = MakeButton(results.transform, "Enregistrer mon score", 10, 2f, 10, 1.5f, ShowKeyboard);
        MatchDepth(resultButton.transform, resultsReference);
        resultButton.GetComponentInChildren<TMP_Text>().fontSize = 0.5f;
        _overlay.gameObject.SetActive(false);
    }

    public void ResetSavedScores()
    {
        string path = Path.Combine(Application.persistentDataPath, "scores.json");
        // Only the game's ranking and its own recovery files are removed.
        foreach (string suffix in new[] { "", ".bak", ".tmp" })
            if (File.Exists(path + suffix)) File.Delete(path + suffix);
        _name = "";
        _savedRound = null;
        if (_nameText != null) _nameText.text = "Ton nom : _";
        if (_ranking != null) _ranking.text = "Aucun score enregistre pour le moment.";
    }

    public void Hide()
    {
        if (_overlay != null) _overlay.gameObject.SetActive(false);
    }

    private bool CanOpen => _session != null &&
        (_session.State == GameSession.SessionState.Menu || _session.State == GameSession.SessionState.Results);

    private void Open()
    {
        GameObject sourcePanel = _session.State == GameSession.SessionState.Results ? _results : _menu;
        MatchDepth(_overlay, sourcePanel.GetComponentInChildren<Button>(true));
        _menu.SetActive(false);
        _results.SetActive(false);
        _overlay.gameObject.SetActive(true);
        _overlay.SetAsLastSibling();
        _message.text = "";
    }

    private static void MatchDepth(Transform element, Button reference)
    {
        if (reference == null) return;
        Vector3 position = element.localPosition;
        position.z = element.parent.InverseTransformPoint(reference.transform.position).z;
        element.localPosition = position;
    }

    public void ShowKeyboard()
    {
        if (!CanOpen || _session.CompletedResult == null || _session.State != GameSession.SessionState.Results) return;
        Open();
        _keyboard.SetActive(true);
        _board.SetActive(false);
        _heading.text = $"{_session.CompletedResult.score} POINTS - {_session.CompletedResult.difficulty}";
        _nameText.text = "Ton nom : " + _name + "_";
        _saveButton.interactable = _savedRound != _session.CompletedResult.id;
        if (!_saveButton.interactable) _message.text = "Ce resultat est deja enregistre.";
    }

    private void AddCharacter(string character)
    {
        if (_name.Length >= ScoreRepository.MaxNameLength) return;
        _name += character;
        _nameText.text = "Ton nom : " + _name + "_";
    }

    private void Backspace()
    {
        if (_name.Length > 0) _name = _name.Substring(0, _name.Length - 1);
        _nameText.text = "Ton nom : " + _name + "_";
    }

    private void SaveResult()
    {
        SavedScore result = _session.CompletedResult;
        if (_session.State != GameSession.SessionState.Results || result == null) return;
        string cleanName = ScoreRepository.CleanName(_name);
        if (string.IsNullOrWhiteSpace(cleanName))
        {
            _message.text = "Saisis ton nom avant d'enregistrer.";
            return;
        }
        try
        {
            bool added = _repository.Save(new SavedScore { id = result.id, playerName = cleanName,
                score = result.score, difficulty = result.difficulty, playedAtUtc = result.playedAtUtc });
            _savedRound = result.id;
            _saveButton.interactable = false;
            ShowLeaderboard();
            _message.text = added ? "Score enregistre !" : "Ce resultat est deja enregistre.";
        }
        catch (Exception exception)
        {
            _message.text = "Sauvegarde impossible. Ton resultat reste disponible : reessaie.";
            Debug.LogError($"Sauvegarde impossible : {_repository.FilePath}\n{exception}", this);
        }
    }

    public void ShowLeaderboard()
    {
        if (!CanOpen) return;
        Open();
        _keyboard.SetActive(false);
        _board.SetActive(true);
        _heading.text = "MEILLEURS SCORES";
        try
        {
            var entries = _repository.Load();
            var lines = new StringBuilder("RANG   NOM   —   POINTS   —   DIFFICULTE\n\n");
            for (int i = 0; i < Math.Min(10, entries.Count); i++)
                lines.AppendLine($"{i + 1}.  {entries[i].playerName}  —  {entries[i].score}  —  {entries[i].difficulty}");
            _ranking.text = entries.Count == 0 ? "Aucun score enregistre pour le moment." : lines.ToString();
        }
        catch (Exception exception)
        {
            _ranking.text = "Impossible de charger le classement.\nLe fichier existant est conserve.";
            Debug.LogError($"Lecture impossible : {_repository.FilePath}\n{exception}", this);
        }
    }

    public void Close()
    {
        Hide();
        _menu.SetActive(_session.State == GameSession.SessionState.Menu);
        _results.SetActive(_session.State == GameSession.SessionState.Results);
    }

    private RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    private TMP_Text Label(Transform parent, string name, string text, float x, float y, float width, float height, float size)
    {
        var label = Rect(name, parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.richText = false;
        return label;
    }

    private Button MakeButton(Transform parent, string text, float x, float y, float width, float height, UnityAction action)
    {
        RectTransform rect = Rect(text, parent, x, y, width, height);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = _buttonColor;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        button.onClick.AddListener(action);
        Label(rect, "Libelle", text, 0, 0, width, height, 26);
        return button;
    }
}



