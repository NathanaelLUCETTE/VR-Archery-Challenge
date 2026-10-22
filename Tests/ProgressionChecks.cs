using System;
using System.Text.Json;

public static class ProgressionChecks
{
    private static int _checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        _checks++;
        Console.WriteLine("PASS " + message);
    }
    public static void Main(string[] args)
    {
        var rules = new ProgressionRules(null);
        Check(rules.UnlockedLevel == 1 && rules.Data.selectedLevel == 1, "First launch starts at level one");
        Check(rules.ShotMultiplier == 1f, "Standard equipment preserves the original shot");
        Check(!rules.SelectLevel(2) && !rules.SelectBow(1) && !rules.SelectArrow(1), "Locked content cannot be selected");
        Check(!rules.CompleteLevel(1, 29, 30, false), "Below-goal score fails");
        Check(!rules.CompleteLevel(1, 100, 30, true), "Training never advances progression");
        Check(!rules.CompleteLevel(3, 100, 30, false), "Locked level cannot be completed");
        Check(rules.CompleteLevel(1, 30, 30, false) && rules.UnlockedLevel == 2, "Exact objective unlocks level two");
        Check(rules.Data.selectedLevel == 2, "Next level selected after success");
        Check(rules.SelectBow(1) && rules.SelectArrow(1), "First equipment upgrades unlocked");
        Check(Math.Abs(rules.ShotMultiplier - 1.4375f) < 0.0001f, "Bow and arrow multipliers combine correctly");
        Check(!rules.SelectBow(2) && !rules.SelectArrow(2), "Third equipment remains locked");
        rules.CompleteLevel(1, 999, 30, false);
        Check(rules.Data.completedLevel == 1, "Replaying an old level does not farm unlocks");
        Check(rules.CompleteLevel(2, 60, 60, false) && rules.UnlockedLevel == 3, "Level two unlocks level three");
        Check(rules.SelectBow(2) && rules.SelectArrow(2), "Third equipment unlocked by level two");
        Check(Math.Abs(rules.ShotMultiplier - 1.95f) < 0.0001f, "Third equipment multiplier is unchanged");
        Check(!rules.SelectLevel(4) && !rules.SelectBow(3) && !rules.SelectArrow(3), "Fourth level and equipment require completing level three");
        Check(rules.CompleteLevel(3, 90, 90, false) && rules.Data.selectedLevel == 4 && rules.UnlockedLevel == 4, "Level three unlocks and selects level four");
        Check(rules.SelectBow(3) && rules.SelectArrow(3), "Fourth equipment unlocked by level three");
        Check(ProgressionRules.Bows[3].Multiplier == 2f && ProgressionRules.Arrows[3].Multiplier == 1.75f && rules.ShotMultiplier == 3.5f, "Fourth bow and arrow give the requested multipliers");
        Check(!rules.CompleteLevel(4, 119, 120, false), "Fourth level objective is enforced");
        Check(rules.CompleteLevel(4, 120, 120, false) && rules.Data.selectedLevel == 4, "Campaign completion stays on level four");
        var oldSave = new ProgressionRules(new ProgressionData { completedLevel = 3, selectedLevel = 3, selectedBow = 2, selectedArrow = 2 });
        Check(oldSave.UnlockedLevel == 4 && oldSave.Data.selectedBow == 2 && oldSave.SelectLevel(4), "Existing completed campaigns unlock level four without losing equipment");
        var jsonOptions = new JsonSerializerOptions { IncludeFields = true };
        string json = JsonSerializer.Serialize(rules.Data, jsonOptions);
        var restored = new ProgressionRules(JsonSerializer.Deserialize<ProgressionData>(json, jsonOptions));
        Check(restored.Data.completedLevel == 4 && restored.Data.selectedBow == 3 && restored.Data.selectedArrow == 3,
            "Progression and selections round-trip through JSON");
        var invalid = new ProgressionRules(new ProgressionData { completedLevel = -5, selectedLevel = 99, selectedBow = 2, selectedArrow = -1 });
        Check(invalid.Data.selectedLevel == 1 && invalid.Data.selectedBow == 0 && invalid.Data.selectedArrow == 0,
            "Invalid or locked saved selections fall back to defaults");
        Check(!rules.SelectLevel(0) && !rules.SelectLevel(5) && !rules.SelectBow(99), "Out-of-range selections rejected");
        Check(!rules.CompleteLevel(1, 100, 0, false), "Invalid objectives cannot grant progression");
        Console.WriteLine($"{_checks} progression checks passed.");
    }
}
