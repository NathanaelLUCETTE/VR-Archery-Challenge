using UnityEngine;

public sealed class UnityScoreSerializer : IScoreSerializer
{
    public ScoreFile Deserialize(string json) => JsonUtility.FromJson<ScoreFile>(json);
    public string Serialize(ScoreFile data) => JsonUtility.ToJson(data, true);
}
