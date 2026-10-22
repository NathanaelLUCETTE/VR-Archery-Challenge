using System;

public sealed class EquipmentSpec
{
    public string Name { get; }
    public float Multiplier { get; }
    public int RequiredCompletedLevel { get; }
    public EquipmentSpec(string name, float multiplier, int required)
    { Name = name; Multiplier = multiplier; RequiredCompletedLevel = required; }
}

[Serializable]
public class ProgressionData
{
    public int completedLevel;
    public int selectedLevel = 1;
    public int selectedBow;
    public int selectedArrow;
}

public sealed class ProgressionRules
{
    public const int LevelCount = 4;
    public static readonly EquipmentSpec[] Bows = {
        new EquipmentSpec("Arc en bois", 1f, 0),
        new EquipmentSpec("Arc de chasseur", 1.25f, 1),
        new EquipmentSpec("Arc carbone", 1.5f, 2),
        new EquipmentSpec("Arc elite", 2f, 3)
    };
    public static readonly EquipmentSpec[] Arrows = {
        new EquipmentSpec("Fleche standard", 1f, 0),
        new EquipmentSpec("Fleche legere", 1.15f, 1),
        new EquipmentSpec("Fleche carbone", 1.3f, 2),
        new EquipmentSpec("Fleche elite", 1.75f, 3)
    };
    public ProgressionData Data { get; }
    public int UnlockedLevel => Math.Min(LevelCount, Data.completedLevel + 1);
    public float ShotMultiplier => Bows[Data.selectedBow].Multiplier * Arrows[Data.selectedArrow].Multiplier;

    public ProgressionRules(ProgressionData saved)
    {
        Data = saved ?? new ProgressionData();
        Data.completedLevel = Math.Max(0, Math.Min(LevelCount, Data.completedLevel));
        Data.selectedLevel = Math.Max(1, Math.Min(UnlockedLevel, Data.selectedLevel));
        if (!IsUnlocked(Bows, Data.selectedBow)) Data.selectedBow = 0;
        if (!IsUnlocked(Arrows, Data.selectedArrow)) Data.selectedArrow = 0;
    }

    public bool IsUnlocked(EquipmentSpec[] catalog, int index) => index >= 0 && index < catalog.Length &&
        Data.completedLevel >= catalog[index].RequiredCompletedLevel;

    public bool SelectLevel(int level)
    {
        if (level < 1 || level > UnlockedLevel) return false;
        Data.selectedLevel = level;
        return true;
    }
    public bool SelectBow(int index)
    {
        if (!IsUnlocked(Bows, index)) return false;
        Data.selectedBow = index;
        return true;
    }
    public bool SelectArrow(int index)
    {
        if (!IsUnlocked(Arrows, index)) return false;
        Data.selectedArrow = index;
        return true;
    }

    public bool CompleteLevel(int level, int score, int goal, bool training)
    {
        if (training || level < 1 || level > UnlockedLevel || level > LevelCount || goal <= 0 || score < goal)
            return false;
        Data.completedLevel = Math.Max(Data.completedLevel, level);
        Data.selectedLevel = Math.Min(LevelCount, level + 1);
        return true;
    }
}
