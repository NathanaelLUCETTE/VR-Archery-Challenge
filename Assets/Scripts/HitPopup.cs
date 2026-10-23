using TMPro;
using UnityEngine;

// Floating "+N" shown where an arrow lands: pops in, rises, faces the player, then fades out.
public class HitPopup : MonoBehaviour
{
    private const float Lifetime = 1.4f;
    private const float RiseDistance = 0.9f;

    private static TMP_FontAsset _font;
    private TextMeshPro _text;
    private Color _color;
    private Vector3 _start;
    private float _age;

    public static void Spawn(Vector3 worldImpact, int points)
    {
        string text = points == 10 ? "+10\n<size=55%>PLEIN CENTRE !</size>"
            : points > 0 ? "+" + points
            : "RATE";
        Spawn(worldImpact, text, ColorFor(points));
    }

    // Free text, e.g. dragon damage. sizeScale grows the text for far-away hits.
    public static void Spawn(Vector3 worldImpact, string text, Color color, float sizeScale = 1f)
    {
        Camera viewer = Camera.main;
        // Pull the text slightly towards the player so it is never buried in what was hit.
        Vector3 toViewer = viewer != null ? (viewer.transform.position - worldImpact).normalized : Vector3.zero;
        var go = new GameObject("Popup " + text);
        go.transform.position = worldImpact + toViewer * 0.3f + Vector3.up * 0.15f;
        go.AddComponent<HitPopup>().Setup(text, color, sizeScale);
    }

    private void Setup(string text, Color color, float sizeScale)
    {
        if (_font == null) _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Bangers SDF");
        _text = gameObject.AddComponent<TextMeshPro>();
        if (_font != null) _text.font = _font;
        _text.alignment = TextAlignmentOptions.Center;
        _text.textWrappingMode = TextWrappingModes.NoWrap;
        // Sized to stay readable from the shooting line, about 9 m away.
        _text.fontSize = 11f * sizeScale;
        _text.outlineWidth = 0.25f;
        _text.outlineColor = new Color32(0, 0, 0, 255);
        _color = color;
        _text.color = _color;
        _text.text = text;
        _start = transform.position;
        transform.localScale = Vector3.zero;
        FaceViewer();
    }

    // Same colour bands as a real archery face: gold, red, blue, black, white.
    private static Color ColorFor(int points)
    {
        if (points >= 9) return new Color(1f, 0.85f, 0.1f);
        if (points >= 7) return new Color(1f, 0.25f, 0.2f);
        if (points >= 5) return new Color(0.3f, 0.65f, 1f);
        if (points >= 3) return new Color(0.75f, 0.75f, 0.75f);
        if (points >= 1) return Color.white;
        return new Color(0.7f, 0.7f, 0.7f);
    }

    private void Update()
    {
        _age += Time.deltaTime;
        float t = _age / Lifetime;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // Quick overshoot pop, then a gentle ease-out rise.
        float pop = _age < 0.15f ? Mathf.Lerp(0f, 1.25f, _age / 0.15f)
            : _age < 0.3f ? Mathf.Lerp(1.25f, 1f, (_age - 0.15f) / 0.15f) : 1f;
        transform.localScale = Vector3.one * pop;
        transform.position = _start + Vector3.up * (RiseDistance * (1f - (1f - t) * (1f - t)));

        Color color = _color;
        color.a = t < 0.65f ? 1f : Mathf.InverseLerp(1f, 0.65f, t);
        _text.color = color;
        FaceViewer();
    }

    private void FaceViewer()
    {
        Camera viewer = Camera.main;
        if (viewer != null)
            transform.rotation = Quaternion.LookRotation(transform.position - viewer.transform.position, Vector3.up);
    }
}
