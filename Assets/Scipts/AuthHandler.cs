using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class AuthHandler : MonoBehaviour
{
    [SerializeField] private string Url = "https://sid-restapi.onrender.com/";
    private string token = "";
    private string username = "";

    [SerializeField] private TMP_InputField inputUsername;
    [SerializeField] private TMP_InputField inputPassword;
    [SerializeField] private GameObject authPanel; // Assign PanelAuth in the 

    private void Awake()
    {
        PlayerPrefs.SetString("token", "");
    }

    private void Start()
    {
        //token = PlayerPrefs.GetString("token", "");
        //username = PlayerPrefs.GetString("username", "");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(username))
        {
            Debug.Log("No auth token found, redirecting to login.");
        }
        else
        {
            StartCoroutine(GetPerfil());
        }
    }

    public void Login()
    {
        if (inputUsername == null || inputPassword == null)
        {
            Debug.LogError("Input fields not assigned in the Inspector!");
            return;
        }

        Credentials credentials = new Credentials
        {
            username = inputUsername.text,
            password = inputPassword.text
            
        };
        ScoreManager.Instance.StartScore();

        string postData = JsonUtility.ToJson(credentials);
        StartCoroutine(LoginPost(postData));
    }

    IEnumerator RegisterPost(string postData)
    {
        string path = "api/usuarios";
        UnityWebRequest www = new UnityWebRequest(Url + path, "POST");
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(postData);
        www.uploadHandler = new UploadHandlerRaw(jsonBytes);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Register Error: {www.error}");
        }
        else
        {
            if (www.responseCode == 200)
            {
                Debug.Log("Registration successful. Logging in...");
                ScoreManager.Instance.StartScore();
                StartCoroutine(LoginPost(postData));
            }
            else
            {
                Debug.LogError($"Register Failed: {www.downloadHandler.text}");
            }
        }
    }
    public void Logout()
    {
        PlayerPrefs.DeleteKey("token");
        PlayerPrefs.DeleteKey("username");
        PlayerPrefs.Save();
        Debug.Log("User logged out. Redirecting to login...");
        authPanel.SetActive(true); // Muestra la pantalla de autenticación
    }
    IEnumerator LoginPost(string postData)
    {
        string path = "api/auth/login";
        UnityWebRequest www = new UnityWebRequest(Url + path, "POST");
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(postData);
        www.uploadHandler = new UploadHandlerRaw(jsonBytes);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Login Error: {www.error}");
        }
        else
        {
            if (www.responseCode == 200)
            {
                string json = www.downloadHandler.text;
                AuthResponse response = JsonUtility.FromJson<AuthResponse>(json);

                Debug.Log("Login successful!");

                PlayerPrefs.SetString("token", response.token);
                PlayerPrefs.SetString("username", response.usuario.username);
                PlayerPrefs.Save();

                if (authPanel != null)
                {
                    authPanel.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("Auth panel reference is missing!");
                }
            }
            else
            {
                Debug.LogError($"Login Failed: {www.downloadHandler.text}");
            }
        }
    }

    IEnumerator GetPerfil()
    {
        string path = "api/usuarios";
        UnityWebRequest www = UnityWebRequest.Get(Url + path);
        www.SetRequestHeader("x-token", token);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Profile Fetch Error: {www.error}");
            Logout(); // Si hay error en el token, cerrar sesión
        }
        else
        {
            if (www.responseCode == 200)
            {
                Debug.Log("Profile loaded successfully.");
                string json = www.downloadHandler.text;
                AuthResponse response = JsonUtility.FromJson<AuthResponse>(json);

                if (authPanel != null)
                {
                    authPanel.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning("Token expired, redirecting to login.");
                Logout();
            }
        }
    }
    public void Register()
    {
        if (inputUsername == null || inputPassword == null)
        {
            Debug.LogError("Input fields not assigned in the Inspector!");
            return;
        }

        Credentials credentials = new Credentials
        {
            username = inputUsername.text,
            password = inputPassword.text
        };

        string postData = JsonUtility.ToJson(credentials);
        StartCoroutine(RegisterPost(postData));
    }
}

[System.Serializable]
public class Credentials
{
    public string username;
    public string password;
}

[System.Serializable]
public class AuthResponse
{
    public UserModel usuario;
    public string token;
}

