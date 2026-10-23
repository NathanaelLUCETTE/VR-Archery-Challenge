using UnityEngine;

// Anything an arrow can hit besides the classic target: birds, the dragon, fireballs.
public interface IArrowHittable
{
    void OnArrowHit(Arrow arrow, Vector3 worldImpact, Collider hitCollider);
}
