using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    [SerializeField] private TMP_Text leaderboardText;
    private string apiUrl = "https://sid-restapi.onrender.com/api/usuarios";
    private string currentUser;

    public void StartScore()
    {
        currentUser = PlayerPrefs.GetString("username", "");
        if (string.IsNullOrEmpty(currentUser))
        {
            Debug.LogError("No user logged in!");
            return;
        }

        StartCoroutine(GetLeaderboard());
    }

    public void AddScore(int points)
    {
        if (string.IsNullOrEmpty(currentUser))
        {
            Debug.LogError("No user logged in!");
            return;
        }

        int currentScore = PlayerPrefs.GetInt($"score_{currentUser}", 0);
        int newScore = currentScore + points;

        if (newScore > currentScore)
        {
            PlayerPrefs.SetInt($"score_{currentUser}", newScore);
            PlayerPrefs.Save();
            StartCoroutine(UpdateHighScore(currentUser, newScore));
        }

        StartCoroutine(GetLeaderboard());
    }

    private IEnumerator GetLeaderboard()
    {
        string token = PlayerPrefs.GetString("token", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No authentication token found!");
            yield break;
        }

        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        request.SetRequestHeader("x-token", token);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error fetching leaderboard: {request.error}");
        }
        else
        {
            string json = request.downloadHandler.text;
            Debug.Log("Received JSON: " + json);

            List<UserModel> users;

            if (json.Trim().StartsWith("{\"usuarios\""))
            {
                // API returns JSON with "usuarios" object
                UserModelList userList = JsonUtility.FromJson<UserModelList>(json);
                users = userList.usuarios;
            }
            else
            {
                // API returns raw JSON array (unwrapped)
                users = JsonHelper.FromJson<UserModel>(json);
            }

            DisplayLeaderboard(users);
        }
    }

    private void DisplayLeaderboard(List<UserModel> users)
    {
        List<(string, int)> scores = new List<(string, int)>();

        foreach (var user in users)
        {
            if (user.data != null && user.data.score > 0)
            {
                scores.Add((user.username, user.data.score));
            }
        }

        scores.Sort((a, b) => b.Item2.CompareTo(a.Item2));

        string leaderboardTextContent = "🏆 Leaderboard 🏆\n";
        foreach (var (user, score) in scores)
        {
            leaderboardTextContent += $"{user}: {score} pts\n";
        }

        leaderboardText.text = leaderboardTextContent;
    }

    private IEnumerator UpdateHighScore(string username, int newScore)
    {
        string token = PlayerPrefs.GetString("token", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No authentication token found!");
            yield break;
        }

        HighScoreData highScoreData = new HighScoreData
        {
            username = username,
            data = new ScoreWrapper { score = newScore }
        };

        string jsonData = JsonUtility.ToJson(highScoreData);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "PATCH");
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-token", token);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error updating high score: {request.error}");
        }
        else
        {
            Debug.Log($"High score updated: {newScore} points for {username}");
            StartCoroutine(GetLeaderboard());
        }
    }
}

[System.Serializable]
public class HighScoreData
{
    public string username;
    public ScoreWrapper data;
}

[System.Serializable]
public class ScoreWrapper
{
    public int score;
}

[System.Serializable]
public class UserModelList
{
    public List<UserModel> usuarios;
}

[System.Serializable]
public class UserModel
{
    public string _id;
    public string username;
    public bool estado;
    public ScoreWrapper data;
}

// Helper class for JSON array deserialization
public static class JsonHelper
{
    public static List<T> FromJson<T>(string json)
    {
        string wrappedJson = $"{{\"array\":{json}}}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrappedJson);
        return wrapper.array;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public List<T> array;
    }
}
