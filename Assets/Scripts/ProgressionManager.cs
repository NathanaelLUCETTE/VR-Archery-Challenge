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

    public void ApplyArrowAppearance(Arrow arrow)
    {
        Tint(arrow.gameObject, Rules.Data.selectedArrow, false);
    }
    public void ApplyBowAppearance(GameObject bow)
    {
        Tint(bow, Rules.Data.selectedBow, true);
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
