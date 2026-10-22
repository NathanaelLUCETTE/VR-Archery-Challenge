using UnityEngine;

public class TargetScore : MonoBehaviour
{
    private const int RingCount = 10;

    [Tooltip("Centre des anneaux : axes X/Y dans la face, Z perpendiculaire.")]
    [SerializeField] private Transform scoreCenter;
    [Tooltip("Rayon du disque en unites locales de ScoreCenter.")]
    [SerializeField, Min(0.001f)] private float radius = 0.6f;
    [SerializeField] private ScoreManager scoreManager;

    public int GetPoints(Vector3 worldImpact)
    {
        if (scoreCenter == null || radius <= 0f)
            return 0;

        Vector3 localImpact = scoreCenter.InverseTransformPoint(worldImpact);
        float distance = new Vector2(localImpact.x, localImpact.y).magnitude;
        float normalizedDistance = distance / radius;

        // Ten equal-width rings; a boundary belongs to the inner ring.
        for (int ring = 1; ring <= RingCount; ring++)
        {
            if (normalizedDistance <= (float)ring / RingCount)
                return RingCount + 1 - ring;
        }
        return 0;
    }

    public void RegisterHit(Vector3 worldImpact)
    {
        var visibility = GetComponentInParent<TargetVisibility>();
        if (visibility != null && !visibility.IsVisible) return;
        if (scoreCenter == null || scoreManager == null)
        {
            Debug.LogWarning("TargetScore : renseigner Score Center et Score Manager.", this);
            return;
        }

        int points = GetPoints(worldImpact);
        scoreManager.AddPoints(points);
        if (points == 0)
            Debug.Log("Impact hors du disque : 0 point.", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (scoreCenter == null || radius <= 0f)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.matrix = scoreCenter.localToWorldMatrix;
        Gizmos.color = Color.yellow;

        for (int ring = 1; ring <= RingCount; ring++)
        {
            float ringRadius = radius * ring / RingCount;
            Vector3 previous = new Vector3(ringRadius, 0f, 0f);
            for (int segment = 1; segment <= 64; segment++)
            {
                float angle = segment * Mathf.PI * 2f / 64f;
                Vector3 next = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ringRadius;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}
