using UnityEngine;

// A Hardcore target: pops up in the air, weaves sideways and up/down, then vanishes.
public class HardcoreTarget : MonoBehaviour
{
    private Vector3 _origin;
    private Vector3 _side;
    private float _sideAmplitude, _upAmplitude, _sideFrequency, _upFrequency, _sidePhase, _upPhase;
    private float _life;
    private float _age;
    private float _despawnAt = -1f;
    private Vector3 _fullScale;
    private Transform _player;

    public bool Expired { get; private set; }

    public void Setup(Vector3 origin, Vector3 side, Transform player, float scale)
    {
        _origin = origin;
        _side = side;
        _player = player;
        _fullScale = Vector3.one * scale;
        _sideAmplitude = Random.Range(1.2f, 2.6f);
        _upAmplitude = Random.Range(0.6f, 1.4f);
        _sideFrequency = Random.Range(0.9f, 1.9f);
        _upFrequency = Random.Range(1.1f, 2.2f);
        _sidePhase = Random.Range(0f, Mathf.PI * 2f);
        _upPhase = Random.Range(0f, Mathf.PI * 2f);
        _life = Random.Range(2.2f, 3.1f);
        transform.localScale = Vector3.zero;
        Place();
    }

    // Leave a moment to see the arrow land before disappearing.
    public void DespawnSoon() { if (_despawnAt < 0f) _despawnAt = _age + 0.3f; }

    private void Update()
    {
        _age += Time.deltaTime;
        float end = _despawnAt >= 0f ? Mathf.Min(_despawnAt, _life) : _life;
        float grow = Mathf.Clamp01(_age / 0.2f);
        float shrink = Mathf.Clamp01((end - _age) / 0.25f);
        transform.localScale = _fullScale * Mathf.Min(grow, shrink);
        Place();
        if (_age >= end)
        {
            Expired = true;
            Destroy(gameObject);
        }
    }

    private void Place()
    {
        transform.position = _origin
            + _side * (_sideAmplitude * Mathf.Sin(_age * _sideFrequency + _sidePhase))
            + Vector3.up * (_upAmplitude * Mathf.Sin(_age * _upFrequency + _upPhase));
        if (_player != null)
        {
            // The ring face (local +Z) turns towards the shooter.
            Vector3 toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toPlayer, Vector3.up);
        }
    }
}
