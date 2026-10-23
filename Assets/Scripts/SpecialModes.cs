using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Runs the Hardcore, Extreme (birds) and Dragon modes on top of GameSession's timer and score.
[DisallowMultipleComponent]
public class SpecialModes : MonoBehaviour
{
    private const int HardcoreSimultaneous = 3;
    private const int BirdCount = 5;

    private GameSession _session;
    private ScoreManager _score;
    private PlayerHealth _health;
    private TargetScore _template;
    private Vector3 _arena;
    private Transform _player;
    private GameSession.Mode _mode = GameSession.Mode.Classic;

    private readonly List<HardcoreTarget> _targets = new List<HardcoreTarget>();
    private float _nextTargetSpawn;
    private readonly List<BirdAgent> _birds = new List<BirdAgent>();
    private float _nextBirdSpawn;
    private DragonBoss _dragon;
    private RectTransform _dragonBar;
    private RectTransform _dragonBarFill;

    public PlayerHealth Health => _health;

    public void Initialize(GameSession session, ScoreManager score, TargetScore template)
    {
        _session = session;
        _score = score;
        _template = template;
        if (template != null) _arena = template.transform.position;
        _health = GetComponent<PlayerHealth>();
        if (_health == null) _health = gameObject.AddComponent<PlayerHealth>();
        _health.Changed += RefreshDragonStatus;
    }

    public static string Title(GameSession.Mode mode) =>
        mode == GameSession.Mode.Hardcore ? "HARDCORE" : mode == GameSession.Mode.Extreme ? "EXTREME" : "DRAGON";

    public float Duration(GameSession.Mode mode) =>
        mode == GameSession.Mode.Hardcore ? 60f : mode == GameSession.Mode.Extreme ? 90f : 180f;

    public int Goal(GameSession.Mode mode) =>
        mode == GameSession.Mode.Hardcore ? 70 : mode == GameSession.Mode.Extreme ? 50 : 0;

    public string Rules(GameSession.Mode mode) =>
        mode == GameSession.Mode.Hardcore ? $"Cibles volantes - Objectif : {Goal(mode)} points"
        : mode == GameSession.Mode.Extreme ? $"Abats les aigles - Objectif : {Goal(mode)} points"
        : "Abats le dragon !";

    // The Dragon round ends early on a kill or when the player runs out of HP.
    public bool Finished => _mode == GameSession.Mode.Dragon &&
        (_health.IsDead || (_dragon != null && _dragon.Dead && Time.time - _dragon.DeathTime > 2.5f));
    public bool DragonDefeated => _dragon != null && _dragon.Dead;
    public bool PlayerDefeated => _health != null && _health.IsDead;

    public void Begin(GameSession.Mode mode)
    {
        Stop();
        _mode = mode;
        Camera eyes = Camera.main;
        _player = eyes != null ? eyes.transform : null;
        if (_player == null) return;
        if (mode == GameSession.Mode.Hardcore) _nextTargetSpawn = Time.time + 0.5f;
        if (mode == GameSession.Mode.Extreme) for (int i = 0; i < BirdCount; i++) SpawnBird();
        if (mode == GameSession.Mode.Dragon) StartDragon();
    }

    public void Stop()
    {
        foreach (HardcoreTarget target in _targets) if (target != null) Destroy(target.gameObject);
        _targets.Clear();
        foreach (BirdAgent bird in _birds) if (bird != null) Destroy(bird.gameObject);
        _birds.Clear();
        if (_dragon != null) Destroy(_dragon.gameObject);
        if (_dragonBar != null) Destroy(_dragonBar.root.gameObject);
        foreach (Fireball fireball in FindObjectsByType<Fireball>()) Destroy(fireball.gameObject);
        _dragon = null;
        if (_mode == GameSession.Mode.Dragon)
        {
            var music = GetComponent<BackgroundMusic>();
            if (music != null) music.PlayAmbientMusic();
        }
        _mode = GameSession.Mode.Classic;
    }

    private void Update()
    {
        if (_player == null || _session == null || _session.State != GameSession.SessionState.Playing) return;
        if (_mode == GameSession.Mode.Hardcore) UpdateHardcore();
        if (_mode == GameSession.Mode.Extreme && _birds.Count < BirdCount && Time.time >= _nextBirdSpawn) SpawnBird();
        if (_mode == GameSession.Mode.Dragon) UpdateDragonBar();
    }

    private Vector3 TowardArena()
    {
        Vector3 direction = _arena - _player.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.forward;
    }

    // ---------- HARDCORE ----------
    private void UpdateHardcore()
    {
        _targets.RemoveAll(target => target == null || target.Expired);
        if (_targets.Count >= HardcoreSimultaneous || Time.time < _nextTargetSpawn || _template == null) return;
        _nextTargetSpawn = Time.time + 0.35f;

        Vector3 forward = TowardArena();
        Vector3 side = Vector3.Cross(Vector3.up, forward);
        Vector3 origin = _player.position + forward * Random.Range(8f, 15f) + side * Random.Range(-5f, 5f);
        origin.y = Random.Range(1.4f, 4f);

        GameObject clone = Instantiate(_template.gameObject, origin, Quaternion.identity);
        clone.name = "Cible hardcore";
        clone.SetActive(true);
        var target = clone.AddComponent<HardcoreTarget>();
        target.Setup(origin, side, _player, 1.4f);
        var score = clone.GetComponent<TargetScore>();
        score.Hit += (hitTarget, points) => target.DespawnSoon();
        _targets.Add(target);
    }

    // ---------- EXTREME ----------
    private void SpawnBird()
    {
        var prefab = Resources.Load<GameObject>("SpecialModes/Eagle");
        if (prefab == null) { Debug.LogWarning("Prefab Eagle introuvable (Resources/SpecialModes).", this); return; }
        Vector3 forward = TowardArena();
        Vector3 center = _player.position + forward * 16f;
        center.y = 6.5f;
        // A wide box in front of the player, above the rooftops' lower edge.
        var area = new Bounds(center, new Vector3(28f, 7f, 28f));
        Vector3 start = center + Vector3.Cross(Vector3.up, forward) * Random.Range(-14f, 14f) + forward * Random.Range(-6f, 10f);
        start.y = Random.Range(4f, 9f);
        GameObject instance = Instantiate(prefab, start, Quaternion.identity);
        var bird = instance.GetComponent<BirdAgent>();
        bird.Setup(area, _score, _player, dead => { _birds.Remove(dead); _nextBirdSpawn = Time.time + 1.2f; });
        _birds.Add(bird);
    }

    // ---------- DRAGON ----------
    private void StartDragon()
    {
        _health.ResetHealth();
        var prefab = Resources.Load<GameObject>("SpecialModes/Dragon");
        if (prefab == null) { Debug.LogWarning("Prefab Dragon introuvable (Resources/SpecialModes).", this); return; }
        GameObject instance = Instantiate(prefab);
        _dragon = instance.GetComponent<DragonBoss>();
        _dragon.Setup(_arena, _player, _health, _score);
        _dragon.Changed += RefreshDragonStatus;
        BuildDragonBar();
        var music = GetComponent<BackgroundMusic>();
        if (music != null) music.PlayBossMusic();
        RefreshDragonStatus();
    }

    private void RefreshDragonStatus()
    {
        if (_mode != GameSession.Mode.Dragon || _dragon == null) return;
        _session.SetStatus($"DRAGON : {_dragon.Hp} / {DragonBoss.MaxHp} PV\nTOI : {_health.Current} / {PlayerHealth.Max} PV");
    }

    // Floating red bar above the dragon, always facing the player.
    private void BuildDragonBar()
    {
        var canvasObject = new GameObject("Barre de vie du dragon", typeof(RectTransform));
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var root = (RectTransform)canvasObject.transform;
        root.sizeDelta = new Vector2(400f, 40f);
        root.localScale = Vector3.one * 0.012f;
        var background = new GameObject("Fond", typeof(RectTransform)).AddComponent<Image>();
        background.transform.SetParent(root, false);
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.sizeDelta = new Vector2(8f, 8f);
        background.color = new Color(0f, 0f, 0f, 0.75f);
        var fill = new GameObject("Vie", typeof(RectTransform)).AddComponent<Image>();
        fill.transform.SetParent(root, false);
        fill.rectTransform.anchorMin = new Vector2(0f, 0f);
        fill.rectTransform.anchorMax = new Vector2(0f, 1f);
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        fill.rectTransform.anchoredPosition = Vector2.zero;
        fill.color = new Color(0.9f, 0.15f, 0.1f);
        _dragonBar = root;
        _dragonBarFill = fill.rectTransform;
    }

    private void UpdateDragonBar()
    {
        if (_dragon == null || _dragonBar == null) return;
        _dragonBar.position = _dragon.transform.position + Vector3.up * 6f;
        _dragonBar.rotation = Quaternion.LookRotation(_dragonBar.position - _player.position, Vector3.up);
        _dragonBarFill.sizeDelta = new Vector2(400f * _dragon.Hp / DragonBoss.MaxHp, 0f);
        _dragonBar.gameObject.SetActive(!_dragon.Dead);
    }
}
