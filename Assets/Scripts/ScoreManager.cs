using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    private GameSession _session;

    public int TotalScore { get; private set; }

    private void Awake()
    {
        _session = GetComponent<GameSession>();
        ResetScore();
    }

    public void AddPoints(int points)
    {
        if (points <= 0 || (_session != null && !_session.CanScore))
            return;

        TotalScore += points;
        RefreshDisplay();
        Debug.Log($"+{points} points | Score : {TotalScore}", this);
    }

    public void ResetScore()
    {
        TotalScore = 0;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (scoreText != null)
            scoreText.text = $"Score : {TotalScore}";
    }
}
