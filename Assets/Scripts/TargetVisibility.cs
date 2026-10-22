using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TargetVisibility : MonoBehaviour
{
    private readonly Dictionary<Collider, bool> _colliders = new Dictionary<Collider, bool>();
    private readonly Dictionary<Renderer, bool> _renderers = new Dictionary<Renderer, bool>();
    private bool _cycling;
    private float _visibleDuration;
    private float _hiddenDuration;
    private float _nextChange;
    public bool IsVisible { get; private set; } = true;

    public void Configure(bool cycling, float visibleDuration, float hiddenDuration, float delay)
    {
        _cycling = cycling;
        _visibleDuration = Mathf.Max(0.1f, visibleDuration);
        _hiddenDuration = Mathf.Max(0.1f, hiddenDuration);
        SetVisible(true);
        _nextChange = Time.time + _visibleDuration + Mathf.Max(0f, delay);
    }

    private void Update()
    {
        if (!_cycling || Time.time < _nextChange) return;
        SetVisible(!IsVisible);
        _nextChange = Time.time + (IsVisible ? _visibleDuration : _hiddenDuration);
    }

    private void SetVisible(bool visible)
    {
        IsVisible = visible;
        if (!visible)
        {
            // Include arrows planted since the previous appearance, preserving their state.
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                _renderers[renderer] = renderer.forceRenderingOff;
                renderer.forceRenderingOff = true;
            }
            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                _colliders[collider] = collider.enabled;
                collider.enabled = false;
            }
        }
        else
        {
            foreach (var entry in _renderers)
                if (entry.Key != null) entry.Key.forceRenderingOff = entry.Value;
            foreach (var entry in _colliders)
                if (entry.Key != null) entry.Key.enabled = entry.Value;
            _renderers.Clear();
            _colliders.Clear();
        }
    }

    private void OnDisable()
    {
        _cycling = false;
        SetVisible(true);
    }
}
