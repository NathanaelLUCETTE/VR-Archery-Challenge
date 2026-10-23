using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Arrow))]
public class ManualArrow : MonoBehaviour
{
    public bool WasTaken { get; private set; }
    public bool Loaded { get; private set; }
    public bool IsHeld => _grab != null && _grab.isSelected;
    private XRGrabInteractable _grab;
    private Collider _handle;

    private void Start()
    {
        gameObject.layer = 0;
        var handle = gameObject.AddComponent<CapsuleCollider>();
        handle.direction = 2;
        // Generous grab zone (18 cm thick, a bit longer than the arrow): reaching roughly
        // towards the quiver is enough, no need to pinch the thin shaft precisely.
        handle.radius = 0.09f;
        handle.height = 0.75f;
        _handle = handle;
        _grab = gameObject.AddComponent<XRGrabInteractable>();
        _grab.colliders.Clear();
        _grab.colliders.Add(handle);
        _grab.interactionLayers = ~0;
        // A ray grab must bring the arrow into the hand, otherwise it can never reach the notch.
        _grab.farAttachMode = InteractableFarAttachMode.Near;
        _grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        _grab.throwOnDetach = false;
        // A dropped arrow must not snap back into the (moving) quiver.
        _grab.retainTransformParent = false;
        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnDrop);
    }
    private void OnGrab(SelectEnterEventArgs args)
    {
        WasTaken = true;
        if (!Loaded) transform.SetParent(null, true);
    }
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
