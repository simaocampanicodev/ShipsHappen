using System.Collections;
using TMPro;
using Unity.Services.Leaderboards;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private TextMeshProUGUI leaderboardText;
    [SerializeField] private GameObject mainMenuButtons;
    [SerializeField] private GameObject logo;

    public void OnClickShowLeaderboard()
    {
        leaderboardPanel.SetActive(true);
        mainMenuButtons.SetActive(false);
        logo.SetActive(false);
        StartCoroutine(LoadLeaderboardCR());
    }

    public void OnClickCloseLeaderboard()
    {
        leaderboardPanel.SetActive(false);
        mainMenuButtons.SetActive(true);
        logo.SetActive(true);
    }

    private IEnumerator LoadLeaderboardCR()
    {
        leaderboardText.text = "Loading...";

        var task = LeaderboardsService.Instance.GetScoresAsync("wins_leaderboard");
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            leaderboardText.text = "Error loading leaderboard";
            Debug.LogError(task.Exception);
            yield break;
        }

        var results = task.Result.Results;

        if (results.Count == 0)
        {
            leaderboardText.text = "No entries";
            yield break;
        }

        string text = "";
        for (int i = 0; i < results.Count; i++)
        {
            text += $"{i + 1}.  {results[i].PlayerName}  —  {(int)results[i].Score} wins\n";
        }

        leaderboardText.text = text;
    }
}