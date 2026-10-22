using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class MovingTarget : MonoBehaviour
{
    [Tooltip("Direction dans les axes locaux de la cible. (1, 0, 0) = horizontal.")]
    [SerializeField] private Vector3 direction = Vector3.right;
    [Tooltip("Distance maximale de chaque cote du point de depart, en metres.")]
    [SerializeField, Min(0f)] private float amplitude = 1f;
    [Tooltip("Vitesse maximale au milieu du trajet, en metres par seconde.")]
    [SerializeField, Min(0f)] private float speed = 0.5f;

    private Rigidbody _body;
    private Vector3 _origin;
    private Vector3 _worldDirection;
    private float _phase;

    public void ResetPosition()
    {
        if (_body == null)
            return;
        _phase = 0f;
        _body.position = _origin;
        transform.position = _origin;
    }

    public void SetSpeed(float metersPerSecond)
    {
        speed = Mathf.Max(0f, metersPerSecond);
    }

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _body.isKinematic = true;
        _body.useGravity = false;
        _body.interpolation = RigidbodyInterpolation.Interpolate;
        _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        _origin = _body.position;
        _worldDirection = transform.TransformDirection(direction.normalized);
    }

    private void FixedUpdate()
    {
        if (amplitude <= 0f || speed <= 0f || _worldDirection.sqrMagnitude == 0f)
            return;

        // Smooth turnarounds, with peak speed independent of the amplitude.
        _phase = Mathf.Repeat(_phase + speed / amplitude * Time.fixedDeltaTime, Mathf.PI * 2f);
        _body.MovePosition(_origin + _worldDirection * (amplitude * Mathf.Sin(_phase)));
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? _origin : transform.position;
        Vector3 axis = Application.isPlaying
            ? _worldDirection
            : transform.TransformDirection(direction.normalized);
        Color previousColor = Gizmos.color;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin - axis * amplitude, origin + axis * amplitude);
        Gizmos.DrawWireSphere(origin - axis * amplitude, 0.05f);
        Gizmos.DrawWireSphere(origin + axis * amplitude, 0.05f);
        Gizmos.color = previousColor;
    }
}
