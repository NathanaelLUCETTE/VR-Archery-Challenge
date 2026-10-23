using System;
using UnityEngine;

// Saved on/off switch for the aiming arc, shared by every game mode.
public static class TrajectorySetting
{
    private const string SaveKey = "Archery.Trajectory.v1";
    public static event Action Changed;

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(SaveKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(SaveKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}

// Draws the ballistic path the nocked arrow would follow if released now.
[DisallowMultipleComponent]
public class TrajectoryPreview : MonoBehaviour
{
    private const int MaxPoints = 90;
    private const float TimeStep = 0.025f;

    private PullInteraction _string;
    private LineRenderer _line;
    private Transform _marker;
    private readonly Vector3[] _points = new Vector3[MaxPoints];
    private ProgressionManager _progression;

    private void Awake()
    {
        _string = GetComponentInChildren<PullInteraction>(true);
        var material = Resources.Load<Material>("SpecialModes/TrajectoryLine");

        var lineObject = new GameObject("Trajectoire");
        lineObject.transform.SetParent(transform, false);
        _line = lineObject.AddComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.widthMultiplier = 0.012f;
        _line.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.4f);
        _line.numCapVertices = 2;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;
        if (material != null) _line.sharedMaterial = material;
        _line.enabled = false;

        // Small ring where the arrow would land.
        _marker = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        _marker.name = "Point d'impact";
        Destroy(_marker.GetComponent<Collider>());
        _marker.SetParent(lineObject.transform, false);
        _marker.localScale = Vector3.one * 0.06f;
        var markerRenderer = _marker.GetComponent<MeshRenderer>();
        if (material != null) markerRenderer.sharedMaterial = material;
        markerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _marker.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        Arrow arrow = NockedArrow();
        bool show = TrajectorySetting.Enabled && arrow != null && _string.isSelected && _string.pullAmount > 0.05f;
        _line.enabled = show;
        _marker.gameObject.SetActive(show);
        if (!show) return;

        // Same launch as Arrow.Release: impulse = forward * pull * speed * equipment.
        if (_progression == null) _progression = FindAnyObjectByType<ProgressionManager>();
        float multiplier = _progression != null ? _progression.ShotMultiplier : 1f;
        var body = arrow.GetComponent<Rigidbody>();
        float mass = body != null ? Mathf.Max(0.001f, body.mass) : 1f;
        Vector3 velocity = arrow.transform.forward * (_string.pullAmount * arrow.speed * multiplier / mass);
        Vector3 position = arrow.tip != null ? arrow.tip.position : arrow.transform.position;

        int count = 0;
        _points[count++] = position;
        bool hit = false;
        while (count < MaxPoints && !hit)
        {
            Vector3 next = position + velocity * TimeStep + 0.5f * Physics.gravity * TimeStep * TimeStep;
            velocity += Physics.gravity * TimeStep;
            if (TryHit(position, next, out Vector3 impact))
            {
                next = impact;
                hit = true;
            }
            _points[count++] = next;
            position = next;
        }
        _line.positionCount = count;
        _line.SetPositions(_points);
        _marker.position = _points[count - 1];
        _marker.gameObject.SetActive(hit);
    }

    private readonly RaycastHit[] _hits = new RaycastHit[16];
    private Transform _player;

    // Nearest hit along the segment, ignoring the bow itself and the player's rig (hands, body).
    private bool TryHit(Vector3 from, Vector3 to, out Vector3 impact)
    {
        impact = to;
        if (_player == null && Camera.main != null) _player = Camera.main.transform.root;
        Vector3 delta = to - from;
        int count = Physics.RaycastNonAlloc(from, delta.normalized, _hits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Transform other = _hits[i].collider.transform;
            if (other.IsChildOf(transform) || (_player != null && other.IsChildOf(_player))) continue;
            if (_hits[i].distance < best)
            {
                best = _hits[i].distance;
                impact = _hits[i].point;
            }
        }
        return best < float.MaxValue;
    }

    private Arrow NockedArrow()
    {
        if (_string == null || _string.notch == null) return null;
        foreach (Arrow arrow in _string.notch.GetComponentsInChildren<Arrow>())
            if (!arrow.HasBeenReleased) return arrow;
        return null;
    }
}
