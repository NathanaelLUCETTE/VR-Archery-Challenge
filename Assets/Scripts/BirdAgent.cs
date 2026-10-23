using System;
using UnityEngine;

// An eagle flying freely inside a box: wanders between random waypoints, banks into turns,
// and tumbles to the ground when an arrow hits it.
public class BirdAgent : MonoBehaviour, IArrowHittable
{
    private Bounds _area;
    private Vector3 _waypoint;
    private float _speed;
    private float _nextWaypointTime;
    private Transform _model;
    private Animator _animator;
    private ScoreManager _score;
    private Transform _player;
    private Action<BirdAgent> _onDeath;
    private bool _dead;

    public void Setup(Bounds area, ScoreManager score, Transform player, Action<BirdAgent> onDeath)
    {
        _area = area;
        _score = score;
        _player = player;
        _onDeath = onDeath;
        _speed = UnityEngine.Random.Range(4.5f, 7.5f);
        _model = transform.childCount > 0 ? transform.GetChild(0) : transform;
        _animator = GetComponentInChildren<Animator>();
        if (_animator != null) _animator.speed = _speed / 6f;
        PickWaypoint();
        transform.rotation = Quaternion.LookRotation(Flat(_waypoint - transform.position), Vector3.up);
    }

    private void Update()
    {
        if (_dead) return;
        if (Time.time >= _nextWaypointTime || (transform.position - _waypoint).sqrMagnitude < 9f) PickWaypoint();

        Vector3 desired = (_waypoint - transform.position).normalized;
        Quaternion target = Quaternion.LookRotation(desired, Vector3.up);
        Quaternion before = transform.rotation;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 75f * Time.deltaTime);
        transform.position += transform.forward * (_speed * Time.deltaTime);

        // Bank into the turn, like a real bird.
        float yawRate = Mathf.DeltaAngle(before.eulerAngles.y, transform.eulerAngles.y) / Mathf.Max(Time.deltaTime, 0.0001f);
        float roll = Mathf.Clamp(-yawRate * 0.5f, -40f, 40f);
        _model.localRotation = Quaternion.Slerp(_model.localRotation, Quaternion.Euler(0f, 0f, roll), Time.deltaTime * 3f);
    }

    private void PickWaypoint()
    {
        _waypoint = new Vector3(
            UnityEngine.Random.Range(_area.min.x, _area.max.x),
            UnityEngine.Random.Range(_area.min.y, _area.max.y),
            UnityEngine.Random.Range(_area.min.z, _area.max.z));
        _nextWaypointTime = Time.time + UnityEngine.Random.Range(3f, 6f);
    }

    public void OnArrowHit(Arrow arrow, Vector3 worldImpact, Collider hitCollider)
    {
        if (_dead) return;
        _dead = true;
        if (_score != null && _score.CanScore)
        {
            float distance = _player != null ? Vector3.Distance(_player.position, worldImpact) : 0f;
            bool far = distance > 18f;
            int points = far ? 15 : 10;
            HitPopup.Spawn(worldImpact, far ? $"+{points}\n<size=55%>TIR DE LOIN !</size>" : "+" + points,
                new Color(1f, 0.85f, 0.1f), 1.6f);
            _score.AddPoints(points);
        }
        if (_animator != null) _animator.speed = 0f;
        // Fall with the arrow still planted in it.
        var body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = transform.forward * (_speed * 0.5f);
            body.angularVelocity = UnityEngine.Random.onUnitSphere * 6f;
        }
        _onDeath?.Invoke(this);
        Destroy(gameObject, 4f);
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.001f ? v : Vector3.forward;
    }
}
