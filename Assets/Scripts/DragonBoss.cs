using System;
using UnityEngine;

// Final boss: patrols high above the village, swoops in, spits fireballs at the player,
// and takes damage from arrows (double on the head).
public class DragonBoss : MonoBehaviour, IArrowHittable
{
    public const int MaxHp = 400;
    private const int FireballDamage = 15;
    private const float FireballSpeed = 11f;

    private enum Phase { Patrol, Approach, Attack, Dying }

    public int Hp { get; private set; } = MaxHp;
    public bool Dead => Hp <= 0;
    public float DeathTime { get; private set; } = -1f;
    public event Action Changed;

    private Animator _animator;
    private Transform _head;
    private Collider _headCollider;
    private Transform _player;
    private PlayerHealth _health;
    private ScoreManager _score;
    private Vector3 _arena;
    private Vector3 _towardArena;
    private Vector3 _side;
    private Phase _phase = Phase.Patrol;
    private float _phaseStart;
    private float _patrolAngle;
    private Vector3 _velocity;
    private Vector3 _attackPoint;
    private int _fireballsLeft;
    private float _nextShot;
    private float _groundY;

    private bool Enraged => Hp <= MaxHp / 2;

    public void Setup(Vector3 arena, Transform player, PlayerHealth health, ScoreManager score)
    {
        _arena = arena;
        _player = player;
        _health = health;
        _score = score;
        _animator = GetComponentInChildren<Animator>();
        _towardArena = arena - player.position;
        _towardArena.y = 0f;
        _towardArena = _towardArena.sqrMagnitude > 0.01f ? _towardArena.normalized : Vector3.forward;
        _side = Vector3.Cross(Vector3.up, _towardArena);
        _groundY = Physics.Raycast(arena + Vector3.up * 50f, Vector3.down, out RaycastHit ground, 100f, ~0, QueryTriggerInteraction.Ignore)
            ? ground.point.y : 0f;
        foreach (Transform bone in GetComponentsInChildren<Transform>())
            if (bone.name.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0) { _head = bone; break; }
        if (_head != null)
        {
            var sphere = _head.gameObject.AddComponent<SphereCollider>();
            // Bones inherit the model's scale; aim for a ~1 m head hitbox in the world.
            sphere.radius = 0.5f / Mathf.Max(0.0001f, _head.lossyScale.x);
            _headCollider = sphere;
        }
        transform.position = PatrolPoint();
        _phaseStart = Time.time;
    }

    private void Update()
    {
        if (_phase == Phase.Dying)
        {
            // Crash down to the ground after the death animation starts.
            _velocity += Physics.gravity * Time.deltaTime;
            Vector3 next = transform.position + _velocity * Time.deltaTime;
            if (next.y <= _groundY) { next.y = _groundY; _velocity = Vector3.zero; }
            transform.position = next;
            return;
        }

        float elapsed = Time.time - _phaseStart;
        switch (_phase)
        {
            case Phase.Patrol:
                _patrolAngle += Time.deltaTime * (Enraged ? 0.6f : 0.45f);
                FlyTowards(PatrolPoint(), Enraged ? 11f : 9f, false);
                if (elapsed > (Enraged ? 3.5f : 5.5f)) StartApproach();
                break;
            case Phase.Approach:
                FlyTowards(_attackPoint, 13f, true);
                if ((transform.position - _attackPoint).sqrMagnitude < 2.5f || elapsed > 4f) StartAttack();
                break;
            case Phase.Attack:
                FlyTowards(_attackPoint, 3f, true);
                if (_fireballsLeft > 0 && Time.time >= _nextShot)
                {
                    SpitFireball();
                    _fireballsLeft--;
                    _nextShot = Time.time + (Enraged ? 0.3f : 0.45f);
                }
                if (_fireballsLeft == 0 && elapsed > 2.2f) SetPhase(Phase.Patrol);
                break;
        }
    }

    private Vector3 PatrolPoint()
    {
        // Low enough to stay in view under the HUD band, wide enough to be a moving target.
        return _arena + _towardArena * 8f
            + _side * (Mathf.Sin(_patrolAngle) * 12f)
            + Vector3.up * (7f + 1.5f * Mathf.Sin(_patrolAngle * 1.7f));
    }

    private void StartApproach()
    {
        _attackPoint = _player.position + _towardArena * 14f
            + _side * UnityEngine.Random.Range(-6f, 6f) + Vector3.up * UnityEngine.Random.Range(4f, 6.5f);
        SetPhase(Phase.Approach);
    }

    private void StartAttack()
    {
        _fireballsLeft = Enraged ? 5 : 3;
        _nextShot = Time.time + 0.45f;
        if (_animator != null) _animator.SetTrigger("Attack");
        SetPhase(Phase.Attack);
    }

    private void SetPhase(Phase phase)
    {
        _phase = phase;
        _phaseStart = Time.time;
    }

    private void FlyTowards(Vector3 destination, float maxSpeed, bool facePlayer)
    {
        Vector3 desired = Vector3.ClampMagnitude((destination - transform.position) * 1.2f, maxSpeed);
        _velocity = Vector3.MoveTowards(_velocity, desired, 14f * Time.deltaTime);
        transform.position += _velocity * Time.deltaTime;
        Vector3 look = facePlayer ? _player.position - transform.position : _velocity;
        look.y = facePlayer ? look.y * 0.3f : 0f;
        if (look.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look, Vector3.up), Time.deltaTime * 3f);
    }

    private void SpitFireball()
    {
        Vector3 mouth = _head != null ? _head.position + transform.forward * 1.2f : transform.position + transform.forward * 3f;
        // Aim at the head with a little spread, so standing still is punished but dodging works.
        Vector3 aim = _player.position + UnityEngine.Random.insideUnitSphere * 1f;
        var fireball = new GameObject("Boule de feu");
        fireball.transform.position = mouth;
        var fx = Resources.Load<GameObject>("SpecialModes/FireFX");
        if (fx != null) Instantiate(fx, fireball.transform).transform.localScale = Vector3.one * 0.9f;
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "Coeur";
        core.transform.SetParent(fireball.transform, false);
        core.transform.localScale = Vector3.one * 0.45f;
        var coreMaterial = Resources.Load<Material>("SpecialModes/FireballCore");
        if (coreMaterial != null) core.GetComponent<MeshRenderer>().sharedMaterial = coreMaterial;
        core.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var glow = fireball.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.5f, 0.1f);
        glow.range = 5f;
        glow.intensity = 3f;
        glow.shadows = LightShadows.None;
        // Generous hitbox so a well-timed arrow can shoot the fireball down.
        fireball.AddComponent<SphereCollider>().radius = 0.45f;
        var body = fireball.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        fireball.AddComponent<Fireball>().Launch((aim - mouth).normalized * FireballSpeed, FireballDamage, _health, _player, transform, _score);
    }

    public void OnArrowHit(Arrow arrow, Vector3 worldImpact, Collider hitCollider)
    {
        if (Dead) return;
        bool headshot = hitCollider != null && hitCollider == _headCollider;
        float power = arrow != null ? Mathf.Max(0.3f, arrow.LaunchPower) : 1f;
        int damage = Mathf.Max(1, Mathf.RoundToInt(20f * power * (headshot ? 2f : 1f)));
        Hp = Mathf.Max(0, Hp - damage);
        HitPopup.Spawn(worldImpact, headshot ? $"-{damage}\n<size=55%>CRITIQUE !</size>" : "-" + damage,
            headshot ? new Color(1f, 0.85f, 0.1f) : new Color(1f, 0.3f, 0.2f), 2f);
        if (_score != null) _score.AddPoints(damage);
        if (Dead) Die();
        else if (_animator != null && _phase != Phase.Attack) _animator.SetTrigger("Hit");
        Changed?.Invoke();
    }

    private void Die()
    {
        DeathTime = Time.time;
        _phase = Phase.Dying;
        _velocity = Vector3.zero;
        if (_animator != null) _animator.SetTrigger("Die");
        foreach (Collider part in GetComponentsInChildren<Collider>()) part.enabled = false;
    }
}
