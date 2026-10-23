using UnityEngine;

// Keeps the quiver at the player's hip, so arrows stay within reach after walking,
// teleporting or turning. Only the head's yaw is used: looking up or down must not move it.
[DisallowMultipleComponent]
public class QuiverFollower : MonoBehaviour
{
    [Tooltip("Decalage par rapport a la tete : x = droite, y = haut, z = avant (metres).")]
    // Front-right of the belly: visible with a glance down and reachable without twisting.
    [SerializeField] private Vector3 offset = new Vector3(0.25f, -0.9f, 0.22f);
    [SerializeField, Min(0.1f)] private float followSpeed = 8f;
    [Tooltip("Au-dela de cette distance (teleportation), le carquois se replace instantanement.")]
    [SerializeField, Min(0.1f)] private float snapDistance = 1.5f;

    private Transform _head;

    private void LateUpdate()
    {
        if (_head == null)
        {
            Camera main = Camera.main;
            if (main == null) return;
            _head = main.transform;
        }

        Vector3 forward = _head.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = transform.forward;
        forward.Normalize();
        Quaternion yaw = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 target = _head.position + yaw * offset;

        if ((target - transform.position).sqrMagnitude > snapDistance * snapDistance)
        {
            transform.SetPositionAndRotation(target, yaw);
            return;
        }
        float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        transform.SetPositionAndRotation(Vector3.Lerp(transform.position, target, t),
            Quaternion.Slerp(transform.rotation, yaw, t));
    }
}
