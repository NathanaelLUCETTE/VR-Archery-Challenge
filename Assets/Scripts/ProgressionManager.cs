using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ProgressionManager : MonoBehaviour
{
    private const string SaveKey = "Archery.Progression.v1";
    private ProgressionRules _rules;
    public ProgressionRules Rules { get { Initialize(); return _rules; } }
    public float ShotMultiplier => Rules.ShotMultiplier;
    public string EquipmentLabel => ProgressionRules.Bows[Rules.Data.selectedBow].Name + " / " +
        ProgressionRules.Arrows[Rules.Data.selectedArrow].Name;
    public string SaveWarning { get; private set; } = "";
    private bool _invalidSave;

    public void Initialize()
    {
        if (_rules != null) return;
        ProgressionData data = null;
        try
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                data = JsonUtility.FromJson<ProgressionData>(PlayerPrefs.GetString(SaveKey));
                if (data == null) throw new InvalidOperationException("Progression vide.");
            }
        }
        catch (Exception exception)
        {
            _invalidSave = true;
            SaveWarning = "Progression illisible : session temporaire, sauvegarde conservee.";
            Debug.LogWarning(SaveWarning + " " + exception.Message, this);
        }
        _rules = new ProgressionRules(data);
    }

    public void SelectLevel(int level) { if (Rules.SelectLevel(level)) Save(); }
    public void SelectBow(int index) { if (Rules.SelectBow(index)) { Save(); RefreshVisuals(); } }
    public void SelectArrow(int index) { if (Rules.SelectArrow(index)) { Save(); RefreshVisuals(); } }
    public bool CompleteLevel(int level, int score, int goal)
    {
        bool won = Rules.CompleteLevel(level, score, goal, false);
        if (won) Save();
        return won;
    }

    public void ResetSavedProgression()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        _rules = new ProgressionRules(null);
        _invalidSave = false;
        SaveWarning = "";
        RefreshVisuals();
    }

    private void Save()
    {
        if (_invalidSave) return;
        try
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Rules.Data));
            PlayerPrefs.Save();
            SaveWarning = "";
        }
        catch (Exception exception)
        {
            SaveWarning = "Progression disponible, mais sauvegarde locale impossible.";
            Debug.LogWarning(SaveWarning + " " + exception.Message, this);
        }
    }

    public void RefreshVisuals()
    {
        foreach (ArrowSpawner spawner in FindObjectsByType<ArrowSpawner>())
            spawner.ApplyBowAppearance(this);
        foreach (Arrow arrow in FindObjectsByType<Arrow>())
            if (!arrow.HasBeenReleased) ApplyArrowAppearance(arrow);
    }

    // Tier 0 keeps the original model; higher tiers swap to the curved bow / real arrow model
    // with their own material (natural wood, carbon, glowing gold).
    private static readonly string[] BowMaterials = { null, "Equipment/BowHunter", "Equipment/BowCarbon", "Equipment/BowElite" };
    private static readonly string[] ArrowMaterials = { null, "Equipment/ArrowLight", "Equipment/ArrowCarbon", "Equipment/ArrowElite" };

    public void ApplyArrowAppearance(Arrow arrow)
    {
        if (!SwapModel(arrow.transform, "Arrow", "Fleche Modele", Rules.Data.selectedArrow, ArrowMaterials))
            Tint(arrow.gameObject, Rules.Data.selectedArrow, false);
    }
    public void ApplyBowAppearance(GameObject bow)
    {
        if (!SwapModel(bow.transform, "PlankBow", "Arc Modele", Rules.Data.selectedBow, BowMaterials))
            Tint(bow, Rules.Data.selectedBow, true);
    }

    private static bool SwapModel(Transform root, string basicName, string modelName, int tier, string[] materials)
    {
        Transform basic = root.Find(basicName);
        Transform model = root.Find(modelName);
        if (basic == null || model == null) return false;
        var basicRenderer = basic.GetComponent<MeshRenderer>();
        var modelRenderer = model.GetComponent<MeshRenderer>();
        if (basicRenderer == null || modelRenderer == null) return false;
        bool upgraded = tier > 0;
        // Only the renderers change: the original object also carries the grab/arrow colliders.
        basicRenderer.enabled = !upgraded;
        modelRenderer.enabled = upgraded;
        if (upgraded && tier < materials.Length)
        {
            var material = Resources.Load<Material>(materials[tier]);
            if (material != null) modelRenderer.sharedMaterial = material;
        }
        basicRenderer.SetPropertyBlock(null);
        return true;
    }
    private static void Tint(GameObject root, int tier, bool skipArrows)
    {
        // Leave the original artwork intact for standard equipment.
        Color tint = tier == 1 ? new Color(0.45f, 0.85f, 1f) : new Color(1f, 0.75f, 0.35f);
        bool goldBow = skipArrows && tier == 2;
        if (goldBow) tint = new Color(1f, 0.72f, 0.16f);
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (skipArrows && renderer.GetComponentInParent<Arrow>() != null) continue;
            var properties = new MaterialPropertyBlock();
            if (tier > 0)
            {
                properties.SetColor("_BaseColor", tint);
                properties.SetColor("_Color", tint);
                if (goldBow)
                {
                    properties.SetFloat("_Metallic", 0.8f);
                    properties.SetFloat("_Smoothness", 0.65f);
                    properties.SetFloat("_Glossiness", 0.65f);
                }
            }
            renderer.SetPropertyBlock(properties);
        }
    }
}

