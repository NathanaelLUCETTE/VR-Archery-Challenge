using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Arrow))]
public class ManualArrow : MonoBehaviour
{
    public bool WasTaken { get; private set; }
    public bool Loaded { get; private set; }
    private XRGrabInteractable _grab;
    private Collider _handle;

    private void Start()
    {
        gameObject.layer = 0;
        var handle = gameObject.AddComponent<CapsuleCollider>();
        handle.direction = 2;
        handle.radius = 0.035f;
        handle.height = 0.5f;
        _handle = handle;
        _grab = gameObject.AddComponent<XRGrabInteractable>();
        _grab.colliders.Clear();
        _grab.colliders.Add(handle);
        _grab.interactionLayers = ~0;
        _grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        _grab.throwOnDetach = false;
        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnDrop);
    }
    private void OnGrab(SelectEnterEventArgs args) { WasTaken = true; }
    private void OnDrop(SelectExitEventArgs args)
    {
        if (Loaded) return;
        var body = GetComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = true;
    }

    private void LateUpdate()
    {
        if (Loaded || _grab == null || !_grab.isSelected) return;
        var hand = _grab.interactorsSelecting[0];
        var manager = _grab.interactionManager;
        if (manager == null) return;
        foreach (ArrowSpawner bow in FindObjectsByType<ArrowSpawner>())
        {
            var cord = bow.GetComponentInChildren<PullInteraction>();
            if (cord == null || cord.isSelected || !cord.IsSelectableBy(hand)) continue;
            if (!bow.TryLoad(this)) continue;
            Loaded = true;
            // Transfer the same held grip from the arrow to the bowstring.
            manager.SelectExit(hand, _grab);
            _grab.enabled = false;
            _handle.enabled = false;
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            transform.SetParent(bow.notch.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            manager.SelectEnter(hand, cord);
            return;
        }
    }
}
