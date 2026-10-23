using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ScoreManager))]
public class GameSession : MonoBehaviour
{
    public enum Difficulty { Facile, Normal, Difficile }
    public enum SessionState { Menu, Playing, Training, Results }
    public enum Mode { Classic, Hardcore, Extreme, Dragon }

    [Serializable]
    public class DifficultySettings
    {
        [Min(1f)] public float duration = 90f;
        [Min(0.1f)] public float targetScale = 1f;
        [Min(0f)] public float targetSpeed = 0.5f;

        public DifficultySettings(float duration, float targetScale, float targetSpeed)
        {
            this.duration = duration;
            this.targetScale = targetScale;
            this.targetSpeed = targetSpeed;
        }
    }

    [SerializeField] private Difficulty difficulty = Difficulty.Normal;
    [SerializeField] private DifficultySettings easy = new DifficultySettings(120f, 1.3f, 0.25f);
    [SerializeField] private DifficultySettings normal = new DifficultySettings(90f, 1f, 0.5f);
    [SerializeField] private DifficultySettings hard = new DifficultySettings(60f, 0.7f, 0.9f);
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text statusText;
    [Tooltip("Laisser vide pour utiliser toutes les cibles actives de la scene.")]
    [SerializeField] private TargetScore[] targets;
    [Tooltip("Decocher pour commencer sur le menu VR.")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TMP_Text resultsText;
    [Tooltip("Objectifs de score des quatre niveaux, evalues a la fin du chronometre.")]
    [SerializeField] private int[] levelGoals = { 30, 60, 90, 120 };
    [Header("Niveaux 3 et 4 : apparitions des cibles")]
    [SerializeField, Min(0.1f)] private float targetVisibleDuration = 3f;
    [SerializeField, Min(0.1f)] private float targetHiddenDuration = 2f;

    private readonly HashSet<MovingTarget> _movingTargets = new HashSet<MovingTarget>();
    private readonly List<TargetVisibility> _targetVisibility = new List<TargetVisibility>();
    private readonly Dictionary<TargetScore, Vector3> _originalScales = new Dictionary<TargetScore, Vector3>();
    private ScoreManager _scoreManager;
    private float _endTime;
    private int _lastDisplayedSecond = -1;
    private bool _initialized;
    public SessionState State { get; private set; } = SessionState.Menu;
    public SavedScore CompletedResult { get; private set; }
    private LeaderboardUI _leaderboard;
    private ProgressionManager _progression;
    private EquipmentMenuUI _equipmentMenu;
    private int _roundLevel = 1;
    private int _roundGoal;
    private Mode _mode = Mode.Classic;
    private SpecialModes _special;
    private readonly List<GameObject> _targetRoots = new List<GameObject>();

    public Mode CurrentMode => _mode;

    public int GetLevelGoal(int level) => GetLevelGoal(level, difficulty);

    // Goals are tuned for Normal; scaling by duration keeps the same points-per-second pace,
    // so Easy is not simply "more time for the same goal".
    public int GetLevelGoal(int level, Difficulty forDifficulty)
    {
        int index = Mathf.Clamp(level, 1, ProgressionRules.LevelCount) - 1;
        int baseGoal = levelGoals != null && index < levelGoals.Length ? Mathf.Max(1, levelGoals[index]) : 30 * (index + 1);
        float ratio = GetSettings(forDifficulty).duration / Mathf.Max(1f, normal.duration);
        return Mathf.Max(1, Mathf.RoundToInt(baseGoal * ratio));
    }

    private DifficultySettings GetSettings(Difficulty forDifficulty) =>
        forDifficulty == Difficulty.Facile ? easy : forDifficulty == Difficulty.Difficile ? hard : normal;

    // Checking the deadline here also rejects impacts before Update ends the round.
    public bool CanScore => isActiveAndEnabled && (State == SessionState.Training ||
        (State == SessionState.Playing && Time.time < _endTime));
    public float RemainingTime => State == SessionState.Playing ? Mathf.Max(0f, _endTime - Time.time) : 0f;

    private void Start()
    {
        Initialize();
        if (autoStart)
            BeginSession(false);
        else
            ReturnToMenu();
    }

    private void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        if (GetComponent<BackgroundMusic>() == null) gameObject.AddComponent<BackgroundMusic>();
        _scoreManager = GetComponent<ScoreManager>();
        _leaderboard = GetComponent<LeaderboardUI>();
        if (_leaderboard == null) _leaderboard = gameObject.AddComponent<LeaderboardUI>();
        _leaderboard.Initialize(this, menuPanel, resultsPanel);
        _progression = GetComponent<ProgressionManager>();
        if (_progression == null) _progression = gameObject.AddComponent<ProgressionManager>();
        _progression.Initialize();
        _progression.RefreshVisuals();
        _equipmentMenu = GetComponent<EquipmentMenuUI>();
        if (_equipmentMenu == null) _equipmentMenu = gameObject.AddComponent<EquipmentMenuUI>();
        _equipmentMenu.Initialize(this, _progression, menuPanel);
        var volumeMenu = GetComponent<VolumeMenuUI>();
        if (volumeMenu == null) volumeMenu = gameObject.AddComponent<VolumeMenuUI>();
        volumeMenu.Initialize(menuPanel, gameplayPanel);
        var resetMenu = GetComponent<ResetDataMenuUI>();
        if (resetMenu == null) resetMenu = gameObject.AddComponent<ResetDataMenuUI>();
        resetMenu.Initialize(this, menuPanel);

        if (targets == null || targets.Length == 0)
            targets = FindObjectsByType<TargetScore>();

        foreach (TargetScore target in targets)
        {
            if (target == null || _originalScales.ContainsKey(target))
                continue;

            _originalScales.Add(target, target.transform.localScale);
            var movement = target.GetComponentInParent<MovingTarget>();
            if (movement != null)
                _movingTargets.Add(movement);
            GameObject visibilityRoot = movement != null ? movement.gameObject : target.gameObject;
            var visibility = visibilityRoot.GetComponent<TargetVisibility>();
            if (visibility == null) visibility = visibilityRoot.AddComponent<TargetVisibility>();
            if (!_targetVisibility.Contains(visibility)) _targetVisibility.Add(visibility);
            if (!_targetRoots.Contains(visibilityRoot)) _targetRoots.Add(visibilityRoot);
        }

        _special = GetComponent<SpecialModes>();
        if (_special == null) _special = gameObject.AddComponent<SpecialModes>();
        _special.Initialize(this, _scoreManager, targets.Length > 0 ? targets[0] : null);
        var modeMenu = GetComponent<ModeMenuUI>();
        if (modeMenu == null) modeMenu = gameObject.AddComponent<ModeMenuUI>();
        modeMenu.Initialize(this, menuPanel, gameplayPanel);
    }

    public void SetStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    // The classic range targets make way for the special modes' own targets.
    private void SetClassicTargetsActive(bool active)
    {
        foreach (GameObject root in _targetRoots)
            if (root != null && root.activeSelf != active) root.SetActive(active);
    }

    public void StartHardcore() => BeginSession(false, Mode.Hardcore);
    public void StartExtreme() => BeginSession(false, Mode.Extreme);
    public void StartDragon() => BeginSession(false, Mode.Dragon);

    public void StartEasy() { difficulty = Difficulty.Facile; BeginSession(false); }
    public void StartNormal() { difficulty = Difficulty.Normal; BeginSession(false); }
    public void StartHard() { difficulty = Difficulty.Difficile; BeginSession(false); }
    public void StartTraining() { BeginSession(true); }
    public void Replay()
    {
        Initialize();
        if (_mode != Mode.Classic)
        {
            BeginSession(false, _mode);
            return;
        }
        _progression.SelectLevel(_roundLevel);
        BeginSession(false);
    }

    private void BeginSession(bool training, Mode mode = Mode.Classic)
    {
        Initialize();
        _leaderboard.Hide();
        _equipmentMenu.Hide();
        CompletedResult = null;
        State = SessionState.Menu;
        ClearFiredArrows();
        _mode = training ? Mode.Classic : mode;
        if (_mode != Mode.Classic)
        {
            BeginSpecialSession();
            return;
        }
        _special.Stop();
        SetClassicTargetsActive(true);
        DifficultySettings settings = GetSettings(difficulty);
        _roundLevel = _progression.Rules.Data.selectedLevel;
        _roundGoal = GetLevelGoal(_roundLevel);
        bool movingLevel = _roundLevel == 2 || _roundLevel == 4;
        // In Difficile, targets also drift slowly on the "fixed" levels.
        float speed = movingLevel ? settings.targetSpeed * 1.2f
            : difficulty == Difficulty.Difficile ? settings.targetSpeed * 0.5f : 0f;
        ConfigureTargets(training ? 1f : Mathf.Max(0.1f, settings.targetScale) * Mathf.Pow(0.9f, _roundLevel - 1),
            training ? 0f : speed,
            !training && (_roundLevel == 3 || _roundLevel == 4));

        _scoreManager.ResetScore();
        _endTime = Time.time + Mathf.Max(1f, settings.duration);
        State = training ? SessionState.Training : SessionState.Playing;
        _lastDisplayedSecond = -1;
        if (statusText != null)
            statusText.text = training ? "Entraînement - tirs libres" : $"Niveau {_roundLevel} - {difficulty}\nObjectif : {_roundGoal} points";
        if (training)
        {
            if (timerText != null)
                timerText.text = "Temps : illimité";
        }
        else
            RefreshTimer();
        RefreshPanels();
    }

    private void BeginSpecialSession()
    {
        SetClassicTargetsActive(false);
        _roundGoal = _special.Goal(_mode);
        _scoreManager.ResetScore();
        _endTime = Time.time + _special.Duration(_mode);
        State = SessionState.Playing;
        _lastDisplayedSecond = -1;
        SetStatus($"{SpecialModes.Title(_mode)}\n{_special.Rules(_mode)}");
        _special.Begin(_mode);
        RefreshTimer();
        RefreshPanels();
    }

    public void ReturnToMenu()
    {
        Initialize();
        _special.Stop();
        _mode = Mode.Classic;
        SetClassicTargetsActive(true);
        _leaderboard.Hide();
        _equipmentMenu.Hide();
        _equipmentMenu.Refresh();
        CompletedResult = null;
        State = SessionState.Menu;
        ClearFiredArrows();
        ConfigureTargets(1f, 0f);
        _scoreManager.ResetScore();
        if (timerText != null) timerText.text = "Temps : --:--";
        if (statusText != null) statusText.text = "Choisis un mode de jeu";
        RefreshPanels();
    }

    private void ConfigureTargets(float scale, float movementSpeed, bool intermittent = false)
    {
        for (int i = 0; i < _targetVisibility.Count; i++)
            if (_targetVisibility[i] != null)
                _targetVisibility[i].Configure(intermittent, targetVisibleDuration, targetHiddenDuration, i * 0.7f);
        foreach (var entry in _originalScales)
        {
            if (entry.Key != null)
                entry.Key.transform.localScale = entry.Value * scale;
        }
        foreach (MovingTarget movement in _movingTargets)
        {
            if (movement == null) continue;
            movement.ResetPosition();
            movement.SetSpeed(movementSpeed);
        }
    }

    private void ClearFiredArrows()
    {
        // Leave the notched arrow alone so the bow can keep loading normally.
        foreach (Arrow arrow in FindObjectsByType<Arrow>())
        {
            // Arrows taken from the quiver then dropped on the ground are cleared too.
            var manual = arrow.GetComponent<ManualArrow>();
            bool dropped = manual != null && manual.WasTaken && !manual.Loaded && !manual.IsHeld;
            if (!arrow.HasBeenReleased && !dropped) continue;
            arrow.gameObject.SetActive(false);
            Destroy(arrow.gameObject);
        }
    }

    private void RefreshPanels()
    {
        if (menuPanel != null) menuPanel.SetActive(State == SessionState.Menu);
        if (gameplayPanel != null) gameplayPanel.SetActive(State == SessionState.Playing || State == SessionState.Training);
        if (resultsPanel != null) resultsPanel.SetActive(State == SessionState.Results);
    }

    private void Update()
    {
        if (State != SessionState.Playing)
            return;

        if (Time.time >= _endTime || (_mode != Mode.Classic && _special.Finished))
            EndGame();
        else
            RefreshTimer();
    }

    private void EndGame()
    {
        if (_mode != Mode.Classic)
        {
            EndSpecialGame();
            return;
        }
        State = SessionState.Results;
        foreach (TargetVisibility visibility in _targetVisibility)
            if (visibility != null) visibility.Configure(false, targetVisibleDuration, targetHiddenDuration, 0f);
        CompletedResult = new SavedScore { id = Guid.NewGuid().ToString(), score = _scoreManager.TotalScore,
            difficulty = difficulty.ToString(), playedAtUtc = DateTime.UtcNow.ToString("O") };
        bool won = _progression.CompleteLevel(_roundLevel, _scoreManager.TotalScore, _roundGoal);
        string outcome = won ? (_roundLevel < ProgressionRules.LevelCount
            ? $"Niveau reussi ! Niveau {_roundLevel + 1} et equipement accessibles au menu."
            : "Bravo ! Les quatre niveaux sont termines.")
            : $"Objectif non atteint : {_roundGoal} points requis. Reessaie !";
        if (!string.IsNullOrEmpty(_progression.SaveWarning)) outcome += "\n" + _progression.SaveWarning;
        foreach (MovingTarget movement in _movingTargets)
        {
            if (movement != null)
                movement.SetSpeed(0f);
        }

        RefreshTimer();
        if (statusText != null)
            statusText.text = $"Partie terminee !\nScore final : {_scoreManager.TotalScore}";
        if (resultsText != null)
            resultsText.text = $"Niveau {_roundLevel} - {difficulty}\nScore final : {_scoreManager.TotalScore}\n{outcome}";
        RefreshPanels();
        Debug.Log($"Partie terminee ({difficulty}) : {_scoreManager.TotalScore} points.", this);
    }

    private void EndSpecialGame()
    {
        State = SessionState.Results;
        int score = _scoreManager.TotalScore;
        string title = SpecialModes.Title(_mode);
        string outcome;
        if (_mode == Mode.Dragon)
            outcome = _special.DragonDefeated ? "VICTOIRE ! Le dragon est vaincu !"
                : _special.PlayerDefeated ? "Tu as ete brule par le dragon... Reessaie !"
                : "Le dragon s'est enfui. Sois plus rapide !";
        else
            outcome = score >= _roundGoal ? "Bravo, objectif atteint !" : $"Objectif non atteint : {_roundGoal} points requis. Reessaie !";
        _special.Stop();
        CompletedResult = new SavedScore { id = Guid.NewGuid().ToString(), score = score,
            difficulty = title, playedAtUtc = DateTime.UtcNow.ToString("O") };
        RefreshTimer();
        SetStatus($"Partie terminee !\nScore final : {score}");
        if (resultsText != null)
            resultsText.text = $"Mode {title}\nScore final : {score}\n{outcome}";
        RefreshPanels();
    }

    private void RefreshTimer()
    {
        int seconds = Mathf.CeilToInt(RemainingTime);
        if (seconds == _lastDisplayedSecond)
            return;

        _lastDisplayedSecond = seconds;
        if (timerText != null)
            timerText.text = $"Temps : {seconds / 60:00}:{seconds % 60:00}";
    }
}




