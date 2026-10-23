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

    public int GetLevelGoal(int level)
    {
        int index = Mathf.Clamp(level, 1, ProgressionRules.LevelCount) - 1;
        return levelGoals != null && index < levelGoals.Length ? Mathf.Max(1, levelGoals[index]) : 30 * (index + 1);
    }

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
        }
    }

    public void StartEasy() { difficulty = Difficulty.Facile; BeginSession(false); }
    public void StartNormal() { difficulty = Difficulty.Normal; BeginSession(false); }
    public void StartHard() { difficulty = Difficulty.Difficile; BeginSession(false); }
    public void StartTraining() { BeginSession(true); }
    public void Replay()
    {
        Initialize();
        _progression.SelectLevel(_roundLevel);
        BeginSession(false);
    }

    private void BeginSession(bool training)
    {
        Initialize();
        _leaderboard.Hide();
        _equipmentMenu.Hide();
        CompletedResult = null;
        State = SessionState.Menu;
        ClearFiredArrows();
        DifficultySettings settings = difficulty == Difficulty.Facile ? easy
            : difficulty == Difficulty.Difficile ? hard : normal;
        _roundLevel = _progression.Rules.Data.selectedLevel;
        _roundGoal = GetLevelGoal(_roundLevel);
        ConfigureTargets(training ? 1f : Mathf.Max(0.1f, settings.targetScale) * Mathf.Pow(0.9f, _roundLevel - 1),
            !training && (_roundLevel == 2 || _roundLevel == 4) ? settings.targetSpeed * 1.2f : 0f,
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

    public void ReturnToMenu()
    {
        Initialize();
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
            if (!arrow.HasBeenReleased) continue;
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

        if (Time.time >= _endTime)
            EndGame();
        else
            RefreshTimer();
    }

    private void EndGame()
    {
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




