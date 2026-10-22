using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ArrowSpawner : MonoBehaviour
{
    public GameObject arrow;
    public GameObject notch;

    private XRGrabInteractable _bow;
    private Coroutine _spawnRoutine;
    private GameObject _currentArrow;

    public void ApplyBowAppearance(ProgressionManager progression)
    {
        if (_bow == null) _bow = GetComponentInParent<XRGrabInteractable>();
        if (_bow != null) progression.ApplyBowAppearance(_bow.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        _bow = GetComponentInParent<XRGrabInteractable>();
        PullInteraction.PullActionReleased += NotchEmpty;
    }

    private void OnDestroy()
    {
        PullInteraction.PullActionReleased -= NotchEmpty;
    }

    // Update is called once per frame
    void Update()
    {
        if (_bow.isSelected && _currentArrow == null && _spawnRoutine == null)
        {
            _spawnRoutine = StartCoroutine(DelayedSpawn());
        }

        if (!_bow.isSelected)
        {
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }
            Destroy(_currentArrow);
            _currentArrow = null;
        }
    }

    private void NotchEmpty(float value)
    {
        // The fired arrow now belongs to the world (or the target), not the bow.
        _currentArrow = null;
    }

    IEnumerator DelayedSpawn()
    {
        yield return new WaitForSeconds(1f);
        _spawnRoutine = null;
        if (_bow.isSelected && _currentArrow == null)
        {
            _currentArrow = Instantiate(arrow, notch.transform);
            var progression = FindAnyObjectByType<ProgressionManager>();
            if (progression != null && _currentArrow.TryGetComponent<Arrow>(out var spawnedArrow))
                progression.ApplyArrowAppearance(spawnedArrow);
        }
    }
}
