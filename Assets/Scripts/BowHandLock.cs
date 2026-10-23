using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Keeps the bow in the hand that holds it: the other hand, reaching for the string,
// must not steal the bow. Releasing the bow first still lets either hand pick it up.
[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public class BowHandLock : MonoBehaviour, IXRSelectFilter
{
    private XRGrabInteractable _grab;

    public bool canProcess => isActiveAndEnabled;

    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _grab.selectFilters.Add(this);
    }

    private void OnDestroy()
    {
        if (_grab != null)
            _grab.selectFilters.Remove(this);
    }

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        return !_grab.isSelected || _grab.interactorsSelecting.Contains(interactor);
    }
}
