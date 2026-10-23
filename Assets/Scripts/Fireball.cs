using UnityEngine;

// A fireball spat by the dragon. Dodge it, or shoot it down with an arrow.
public class Fireball : MonoBehaviour, IArrowHittable
{
    private const float HitRadius = 0.55f;
    private const float Lifetime = 7f;

    private Vector3 _velocity;
    private float _age;
    private int _damage;
    private PlayerHealth _health;
    private Transform _head;
    private Transform _ignoreRoot;
    private ScoreManager _score;
    private readonly Collider[] _overlaps = new Collider[8];

    public void Launch(Vector3 velocity, int damage, PlayerHealth health, Transform head, Transform ignoreRoot, ScoreManager score)
    {
        _velocity = velocity;
        _damage = damage;
        _health = health;
        _head = head;
        _ignoreRoot = ignoreRoot;
        _score = score;
    }

    private void Update()
    {
        _age += Time.deltaTime;
        transform.position += _velocity * Time.deltaTime;
        if (_age > Lifetime) { Explode(false); return; }

        if (_head != null && (_head.position - transform.position).sqrMagnitude < HitRadius * HitRadius)
        {
            if (_health != null) _health.Damage(_damage);
            Explode(true);
            return;
        }

        // Burst on walls, roofs and the ground (not on the dragon, the player rig or itself).
        int count = Physics.OverlapSphereNonAlloc(transform.position, 0.3f, _overlaps, ~0, QueryTriggerInteraction.Ignore);
        Transform playerRoot = _head != null ? _head.root : null;
        for (int i = 0; i < count; i++)
        {
            Transform other = _overlaps[i].transform;
            if (other.IsChildOf(transform) || (_ignoreRoot != null && other.IsChildOf(_ignoreRoot))
                || (playerRoot != null && other.IsChildOf(playerRoot)) || other.GetComponentInParent<Arrow>() != null) continue;
            Explode(true);
            return;
        }
    }

    public void OnArrowHit(Arrow arrow, Vector3 worldImpact, Collider hitCollider)
    {
        if (_score != null && _score.CanScore)
        {
            _score.AddPoints(5);
            HitPopup.Spawn(worldImpact, "+5\n<size=55%>BOULE DETRUITE</size>", new Color(1f, 0.55f, 0.1f), 1.4f);
        }
        // The arrow burns up with the fireball instead of hanging in mid-air.
        if (arrow != null) Destroy(arrow.gameObject);
        Explode(true);
    }

    private void Explode(bool burst)
    {
        if (burst)
        {
            var fx = Resources.Load<GameObject>("SpecialModes/FireFX");
            if (fx != null)
            {
                GameObject blast = Instantiate(fx, transform.position, Quaternion.identity);
                blast.transform.localScale = Vector3.one * 1.6f;
                Destroy(blast, 1.2f);
                foreach (ParticleSystem system in blast.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = system.main;
                    main.loop = false;
                }
            }
        }
        Destroy(gameObject);
    }
}
