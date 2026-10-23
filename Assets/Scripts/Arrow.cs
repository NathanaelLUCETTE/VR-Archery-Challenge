using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Arrow : MonoBehaviour
{
    public float speed = 10f;
    public Transform tip;

    private Rigidbody _rigidbody;
    private Collider[] _arrowColliders;
    private Collider[] _bowColliders = new Collider[0];
    //Polish
    private ParticleSystem _particleSystem;
    private TrailRenderer _trailRenderer;
    private bool _inAir = false;
    private Vector3 _lastPosition = Vector3.zero;
    public bool HasBeenReleased { get; private set; }
    // Draw strength times equipment multiplier: 1 = full draw with standard gear.
    public float LaunchPower { get; private set; }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _arrowColliders = GetComponentsInChildren<Collider>(true);
        var bow = GetComponentInParent<XRGrabInteractable>();
        if (bow != null)
            _bowColliders = bow.GetComponentsInChildren<Collider>(true);

        //Polish
        _particleSystem = GetComponentInChildren<ParticleSystem>();
        _trailRenderer = GetComponentInChildren<TrailRenderer>();

        PullInteraction.PullActionReleased += Release;
        Stop();
    }

    private void OnDestroy()
    {
        PullInteraction.PullActionReleased -= Release;
    }

    private void Release(float value)
    {
        var manual = GetComponent<ManualArrow>();
        if (manual != null && !manual.Loaded) return;
        var owningBow = GetComponentInParent<ArrowSpawner>();
        if (owningBow != null) _bowColliders = owningBow.GetComponentsInChildren<Collider>(true);
        HasBeenReleased = true;
        PullInteraction.PullActionReleased -= Release;

        gameObject.transform.parent = null;
        _inAir = true;

        SetPhysics(true);

        // Keep target collisions, but never hit the bow that spawned this arrow.
        // Apply after enabling the colliders, before the next physics step.
        foreach (var arrowCollider in _arrowColliders)
        {
            foreach (var bowCollider in _bowColliders)
            {
                if (bowCollider != null && !bowCollider.transform.IsChildOf(transform))
                    Physics.IgnoreCollision(arrowCollider, bowCollider);
            }
        }

        var progression = FindAnyObjectByType<ProgressionManager>();
        float equipmentMultiplier = progression != null ? progression.ShotMultiplier : 1f;
        LaunchPower = value * equipmentMultiplier;
        Vector3 force = transform.forward * value * speed * equipmentMultiplier;
        _rigidbody.AddForce(force, ForceMode.Impulse);

        StartCoroutine(RotateWithVelocity());

        _lastPosition = tip != null ? tip.position : transform.position;

        //Polish
        if (_particleSystem != null) _particleSystem.Play();
        if (_trailRenderer != null) _trailRenderer.emitting = true;
    }

    private IEnumerator RotateWithVelocity()
    {
        yield return new WaitForFixedUpdate();

        while (_inAir)
        {
            if (_rigidbody.linearVelocity.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(_rigidbody.linearVelocity, transform.up);

            yield return null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_inAir || !isActiveAndEnabled)
            return;

        if (collision.transform.gameObject.layer != 8)
        {
            // attachedRigidbody also finds a body on the collider's parent.
            Rigidbody body = collision.rigidbody;
            if (body != null && !body.isKinematic)
            {
                body.AddForce(_rigidbody.linearVelocity, ForceMode.Impulse);
            }

            Stop();
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            transform.SetParent(collision.collider.transform, true);

            // Stop clears _inAir before scoring, so later contacts cannot score again.
            var target = collision.collider.GetComponentInParent<TargetScore>();
            Vector3 impact = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            if (target != null && collision.contactCount > 0)
                target.RegisterHit(impact);
            var hittable = collision.collider.GetComponentInParent<IArrowHittable>();
            if (hittable != null)
                hittable.OnArrowHit(this, impact, collision.collider);
        }
    }

    private void Stop()
    {
        _inAir = false;
        SetPhysics(false);

        //Polish
        if (_particleSystem != null) _particleSystem.Stop();
        if (_trailRenderer != null) _trailRenderer.emitting = false;
    }

    private void SetPhysics(bool usePhysics)
    {
        if (!usePhysics)
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

        _rigidbody.useGravity = usePhysics;
        _rigidbody.isKinematic = !usePhysics;

        if (usePhysics)
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // An arrow on the string must not obstruct grabbing or drawing the bow.
        foreach (var arrowCollider in _arrowColliders)
            arrowCollider.enabled = usePhysics;
    }
}

