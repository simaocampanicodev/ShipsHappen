using System.Collections;
using Unity.Services.Leaderboards;
using Unity.Services.Authentication;
using UnityEngine;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance;

    private const string LEADERBOARD_ID = "wins_leaderboard";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterWin()
    {
        StartCoroutine(AddWinCR());
    }

    private IEnumerator AddWinCR()
    {
        // busca o score atual do jogador
        var getTask = LeaderboardsService.Instance.GetPlayerScoreAsync(LEADERBOARD_ID);
        yield return new WaitUntil(() => getTask.IsCompleted);

        double currentScore = 0;
        if (getTask.Exception == null && getTask.Result != null)
            currentScore = getTask.Result.Score;

        // incrementa +1 vitoria
        var addTask = LeaderboardsService.Instance.AddPlayerScoreAsync(
            LEADERBOARD_ID, currentScore + 1
        );
        yield return new WaitUntil(() => addTask.IsCompleted);

        if (addTask.Exception != null)
            Debug.LogError("Leaderboard error: " + addTask.Exception);
        else
            Debug.Log("Win registered. Total wins: " + (currentScore + 1));
    }

    public IEnumerator GetTopScoresCR(System.Action<string> callback)
    {
        var task = LeaderboardsService.Instance.GetScoresAsync(LEADERBOARD_ID);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            callback("Error loading leaderboard");
            yield break;
        }

        string result = "TOP PLAYERS\n";
        int rank = 1;
        foreach (var entry in task.Result.Results)
        {
            result += $"{rank}. {entry.PlayerName} — {entry.Score} wins\n";
            rank++;
        }

        callback(result);
    }
}