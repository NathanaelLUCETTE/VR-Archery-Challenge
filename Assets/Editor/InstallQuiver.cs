using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallQuiver
{
    [MenuItem("Tools/Archery/Ajouter le carquois fixe")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) return;
        if (Object.FindAnyObjectByType<QuiverSpawner>() != null) { Debug.Log("Un carquois existe deja."); return; }
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Dimasjk Studio/Weapons/Low Poly Style Bows & Quivers Part 1/Prefabs/Bow_01/Quiver_01.prefab");
        var arrow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bow And Arrow/Prefab/Arrow.prefab");
        if (model == null || arrow == null) { Debug.LogError("Prefab carquois ou Arrow introuvable."); return; }
        var root = new GameObject("Carquois fixe");
        Undo.RegisterCreatedObjectUndo(root, "Ajouter carquois fixe");
        Camera camera = Camera.main;
        Vector3 forward = camera != null ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized : Vector3.forward;
        if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        root.transform.position = (camera != null ? camera.transform.position : Vector3.zero) + right * 0.6f + forward * 0.35f;
        var pos = root.transform.position;
        pos.y = 0.75f;
        root.transform.position = pos;
        var stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.name = "Support";
        stand.transform.SetParent(root.transform, false);
        stand.transform.localPosition = new Vector3(0, -0.375f, 0);
        stand.transform.localScale = new Vector3(0.4f, 0.75f, 0.4f);
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>()) collider.enabled = false;
        var renderer = visual.GetComponentInChildren<Renderer>();
        if (renderer != null && renderer.bounds.size.y > 0.001f)
        {
            visual.transform.localScale *= 0.45f / renderer.bounds.size.y;
            visual.transform.position += Vector3.up * (root.transform.position.y - renderer.bounds.min.y);
        }
        var spawn = new GameObject("SpawnPoint").transform;
        spawn.SetParent(root.transform, false);
        spawn.localPosition = new Vector3(0, 0.5f, 0);
        spawn.localRotation = Quaternion.Euler(90, 0, 0);
        var quiver = root.AddComponent<QuiverSpawner>();
        quiver.arrowPrefab = arrow;
        quiver.spawnPoint = spawn;
        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("Carquois ajoute. Ajuste sa position a cote du joueur puis enregistre la scene.");
    }
}
