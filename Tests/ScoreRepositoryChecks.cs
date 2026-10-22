using System;
using System.IO;
using System.Text.Json;

// Runs outside Unity against the production repository; Unity's JsonUtility adapter
// is checked by compilation and still needs an in-player round-trip test.
public static class ScoreRepositoryChecks
{
    private sealed class Serializer : IScoreSerializer
    {
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions { IncludeFields = true };
        public ScoreFile Deserialize(string json) => JsonSerializer.Deserialize<ScoreFile>(json, _options);
        public string Serialize(ScoreFile data) => JsonSerializer.Serialize(data, _options);
    }

    private static int _checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        _checks++;
        Console.WriteLine("PASS " + message);
    }

    private static SavedScore Entry(string name, int points, string difficulty = "Normal") => new SavedScore
    {
        id = Guid.NewGuid().ToString(), playerName = name, score = points,
        difficulty = difficulty, playedAtUtc = DateTime.UtcNow.ToString("O")
    };

    private static void MustReject(Action action, string message)
    {
        bool rejected = false;
        try { action(); } catch (Exception) { rejected = true; }
        Check(rejected, message);
    }

    public static void Main(string[] args)
    {
        string directory = args[0];
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "scores.json");
        var repository = new ScoreRepository(path, new Serializer());
        Check(repository.Load().Count == 0, "First launch has an empty ranking");
        var first = Entry("Élodie", 42);
        Check(repository.Save(first), "First score saved");
        Check(!repository.Save(first), "Duplicate round rejected");
        Check(repository.Load().Count == 1, "Duplicate does not add an entry");
        repository.Save(Entry("Nicolas", 90, "Difficile"));
        var restarted = new ScoreRepository(path, new Serializer());
        var loaded = restarted.Load();
        Check(loaded.Count == 2 && loaded[0].score == 90 && loaded[1].score == 42, "Reload and descending score order");
        Check(loaded[1].playerName == "Élodie" && loaded[0].difficulty == "Difficile", "Unicode name and difficulty round trip");
        Check(File.Exists(path + ".bak"), "Previous save backed up");
        MustReject(() => repository.Save(Entry("", 1)), "Blank name rejected");
        MustReject(() => repository.Save(Entry("Player", -1)), "Negative score rejected");
        MustReject(() => repository.Save(Entry("Player", 1, "Training")), "Training is not a valid saved difficulty");
        Check(ScoreRepository.CleanName("  Élo<die>\n  ") == "Élodie", "Name sanitization");
        Check(ScoreRepository.CleanName(new string('a', 30)).Length == 20, "Name length capped");
        string corruptPath = Path.Combine(directory, "corrupt.json");
        File.WriteAllText(corruptPath, "not JSON");
        var corrupt = new ScoreRepository(corruptPath, new Serializer());
        MustReject(() => corrupt.Load(), "Corrupt file reported");
        MustReject(() => corrupt.Save(Entry("Player", 10)), "Corrupt file not overwritten on save");
        Check(File.ReadAllText(corruptPath) == "not JSON", "Corrupt file preserved byte for byte");
        File.WriteAllText(corruptPath, "{}");
        MustReject(() => corrupt.Load(), "Missing schema rejected");
        File.WriteAllText(corruptPath, "{\"version\":2,\"entries\":[]}");
        MustReject(() => corrupt.Load(), "Unsupported version rejected");
        string blocked = Path.Combine(directory, "not-a-directory");
        File.WriteAllText(blocked, "preserve");
        MustReject(() => new ScoreRepository(Path.Combine(blocked, "scores.json"), new Serializer()).Save(first), "Storage failure propagated to UI");
        Console.WriteLine($"{_checks} checks passed.");
    }
}
