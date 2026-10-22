using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[Serializable]
public class SavedScore
{
    public string id;
    public string playerName;
    public int score;
    public string difficulty;
    public string playedAtUtc;
}

[Serializable]
public class ScoreFile
{
    public int version;
    public List<SavedScore> entries;
}

public interface IScoreSerializer
{
    ScoreFile Deserialize(string json);
    string Serialize(ScoreFile data);
}

// File access is separate from Unity/UI so persistence can be tested independently.
public sealed class ScoreRepository
{
    public const int MaxNameLength = 20;
    public string FilePath { get; }
    private readonly IScoreSerializer _serializer;

    public ScoreRepository(string filePath, IScoreSerializer serializer)
    {
        FilePath = filePath;
        _serializer = serializer;
    }

    public static string CleanName(string value)
    {
        string clean = new string((value ?? "").Where(c => char.IsLetterOrDigit(c) ||
            c == ' ' || c == '-' || c == '\'').ToArray()).Trim();
        return clean.Length > MaxNameLength ? clean.Substring(0, MaxNameLength) : clean;
    }

    public List<SavedScore> Load()
    {
        if (!File.Exists(FilePath))
            return new List<SavedScore>();

        ScoreFile file;
        try { file = _serializer.Deserialize(File.ReadAllText(FilePath)); }
        catch (Exception exception) { throw new InvalidDataException("Fichier de scores illisible. Il a ete conserve.", exception); }

        if (file == null || file.version != 1 || file.entries == null)
            throw new InvalidDataException("Format de classement invalide ou incompatible.");

        var ids = new HashSet<string>();
        foreach (SavedScore entry in file.entries)
        {
            if (!IsValid(entry) || !ids.Add(entry.id))
                throw new InvalidDataException("Le classement contient une entree invalide.");
        }
        return file.entries.OrderByDescending(e => e.score)
            .ThenBy(e => e.playedAtUtc, StringComparer.Ordinal).ThenBy(e => e.id, StringComparer.Ordinal).ToList();
    }

    // false means this round was already recorded, including after a reload.
    public bool Save(SavedScore entry)
    {
        if (!IsValid(entry))
            throw new ArgumentException("Nom ou resultat invalide.");

        List<SavedScore> entries = Load(); // Never overwrite a corrupt existing file.
        if (entries.Exists(existing => existing.id == entry.id))
            return false;

        entries.Add(entry);
        string json = _serializer.Serialize(new ScoreFile { version = 1, entries = entries });
        string directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        string temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, json);
        if (!File.Exists(FilePath))
            File.Move(temporary, FilePath);
        else
        {
            try { File.Replace(temporary, FilePath, FilePath + ".bak"); }
            catch (PlatformNotSupportedException)
            {
                File.Copy(FilePath, FilePath + ".bak", true);
                File.Copy(temporary, FilePath, true);
                File.Delete(temporary);
            }
        }
        return true;
    }

    private static bool IsValid(SavedScore entry)
    {
        return entry != null && Guid.TryParse(entry.id, out _) && entry.score >= 0 &&
            !string.IsNullOrWhiteSpace(entry.playerName) && entry.playerName == CleanName(entry.playerName) &&
            (entry.difficulty == "Facile" || entry.difficulty == "Normal" || entry.difficulty == "Difficile") &&
            DateTimeOffset.TryParse(entry.playedAtUtc, out _);
    }
}
