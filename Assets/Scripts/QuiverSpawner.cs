using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class QuiverSpawner : MonoBehaviour
{
    public GameObject arrowPrefab;
    public Transform spawnPoint;
    private ManualArrow _available;
    private float _nextSpawn;

    private void Update()
    {
        if (arrowPrefab == null || spawnPoint == null) return;
        if (_available != null && !_available.WasTaken) return;
        if (Time.time < _nextSpawn) return;
        var instance = Instantiate(arrowPrefab, spawnPoint.position, spawnPoint.rotation);
        _available = instance.AddComponent<ManualArrow>();
        var progression = FindAnyObjectByType<ProgressionManager>();
        if (progression != null) progression.ApplyArrowAppearance(instance.GetComponent<Arrow>());
        _nextSpawn = Time.time + 0.5f;
    }
}
