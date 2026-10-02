using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ArrowSpawner : MonoBehaviour
{
    public GameObject arrow;
    public GameObject notch;

    private XRGrabInteractable _bow;
    private bool _arrowNotched = false;
    private GameObject _currentArrow;

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
        if (_bow.isSelected && !_arrowNotched)
        {
            StartCoroutine("DelayedSpawn");
        }

        if (!_bow.isSelected)
        {
            Destroy(_currentArrow);
        }
    }

    private void NotchEmpty(float value)
    {
        _arrowNotched = false;
    }

    IEnumerator DelayedSpawn()
    {
        _arrowNotched = true;
        yield return new WaitForSeconds(1f);
        _currentArrow = Instantiate(arrow, notch.transform);
    }
}