using System.Collections;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float speed = 10f;
    public Transform tip;

    private Rigidbody _rigidbody;
    //Polish
    private ParticleSystem _particleSystem;
    private TrailRenderer _trailRenderer;
    private bool _inAir = false;
    private Vector3 _lastPosition = Vector3.zero;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

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
        PullInteraction.PullActionReleased -= Release;

        gameObject.transform.parent = null;
        _inAir = true;

        SetPhysics(true);

        Vector3 force = transform.forward * value * speed;
        _rigidbody.AddForce(force, ForceMode.Impulse);

        StartCoroutine(RotateWithVelocity());

        _lastPosition = tip.position;

        //Polish
        _particleSystem.Play();
        _trailRenderer.emitting = true;
    }

    private IEnumerator RotateWithVelocity()
    {
        yield return new WaitForFixedUpdate();

        while (_inAir)
        {
            Quaternion newRotation =
                Quaternion.LookRotation(_rigidbody.linearVelocity, transform.up);

            transform.rotation = newRotation;

            yield return null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.gameObject.layer != 8)
        {
            if (collision.transform.TryGetComponent(out Rigidbody body))
            {
                _rigidbody.interpolation = RigidbodyInterpolation.None;
                transform.parent = collision.transform;
                body.AddForce(_rigidbody.linearVelocity, ForceMode.Impulse);
            }

            Stop();
        }
    }

    private void Stop()
    {
        _inAir = false;
        SetPhysics(false);

        //Polish
        _particleSystem.Stop();
        _trailRenderer.emitting = false;
    }

    private void SetPhysics(bool usePhysics)
    {
        _rigidbody.useGravity = usePhysics;
        _rigidbody.isKinematic = !usePhysics;
    }
}