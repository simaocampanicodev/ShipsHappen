using System.Collections;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputUsername;
    [SerializeField] private TMP_InputField inputPassword;
    [SerializeField] private TextMeshProUGUI textMessage;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private Toggle toggleRememberMe;

    private const string REMEMBER_ME_KEY = "RememberMe";
    private const string USERNAME_KEY = "SavedUsername";

    void Start()
    {
        loginPanel.SetActive(false);
        StartCoroutine(InitCR());
    }

    private IEnumerator InitCR()
    {
        var initTask = UnityServices.InitializeAsync();
        yield return new WaitUntil(() => initTask.IsCompleted);

        if (initTask.Exception != null)
        {
            textMessage.text = "Error initializing services.";
            loginPanel.SetActive(true);
            yield break;
        }

        // se remember me estava ativo e tem sessao guardada, entra direto
        bool rememberMe = PlayerPrefs.GetInt(REMEMBER_ME_KEY, 0) == 1;
        if (rememberMe && AuthenticationService.Instance.SessionTokenExists)
        {
            textMessage.text = "Signing in...";

            var sessionTask = AuthenticationService.Instance.SignInAnonymouslyAsync();
            yield return new WaitUntil(() => sessionTask.IsCompleted);

            if (sessionTask.Exception == null)
            {
                GoToMainMenu();
                yield break;
            }
        }

        // preenche o username guardado se existir
        string savedUsername = PlayerPrefs.GetString(USERNAME_KEY, "");
        if (!string.IsNullOrEmpty(savedUsername))
            inputUsername.text = savedUsername;

        toggleRememberMe.isOn = rememberMe;
        loginPanel.SetActive(true);
    }

    public void OnClickLogin()
    {
        string username = inputUsername.text.Trim();
        string password = inputPassword.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            textMessage.text = "Please fill in all fields.";
            return;
        }

        StartCoroutine(LoginCR(username, password));
    }

    public void OnClickRegister()
    {
        string username = inputUsername.text.Trim();
        string password = inputPassword.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            textMessage.text = "Please fill in all fields.";
            return;
        }

        if (password.Length < 8 || password.Length > 30)
        {
            textMessage.text = "Password must be 8-30 characters.";
            return;
        }

        bool hasUpper = false, hasLower = false, hasDigit = false, hasSymbol = false;
        foreach (char c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            if (char.IsLower(c)) hasLower = true;
            if (char.IsDigit(c)) hasDigit = true;
            if (!char.IsLetterOrDigit(c)) hasSymbol = true;
        }

        if (!hasUpper || !hasLower || !hasDigit || !hasSymbol)
        {
            textMessage.text = "Password requires:1 uppercase,1 lowercase,1 number,1 symbol";
            return;
        }

        StartCoroutine(RegisterCR(username, password));
    }

    private IEnumerator LoginCR(string username, string password)
    {
        textMessage.text = "Signing in...";
        SetInteractable(false);

        var task = AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
        yield return new WaitUntil(() => task.IsCompleted);

        SetInteractable(true);

        if (task.Exception != null)
        {
            textMessage.text = GetErrorMessage(task.Exception.Message);
            yield break;
        }

        SaveRememberMe(username);
        GoToMainMenu();
    }

    private IEnumerator RegisterCR(string username, string password)
    {
        textMessage.text = "Creating account...";
        SetInteractable(false);

        var task = AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
        yield return new WaitUntil(() => task.IsCompleted);

        SetInteractable(true);

        if (task.Exception != null)
        {
            textMessage.text = GetErrorMessage(task.Exception.Message);
            yield break;
        }

        // guarda o username como nome para a leaderboard
        var nameTask = AuthenticationService.Instance.UpdatePlayerNameAsync(username);
        yield return new WaitUntil(() => nameTask.IsCompleted);

        SaveRememberMe(username);
        GoToMainMenu();
    }

    private void SaveRememberMe(string username)
    {
        if (toggleRememberMe.isOn)
        {
            PlayerPrefs.SetInt(REMEMBER_ME_KEY, 1);
            PlayerPrefs.SetString(USERNAME_KEY, username);
        }
        else
        {
            PlayerPrefs.SetInt(REMEMBER_ME_KEY, 0);
            PlayerPrefs.DeleteKey(USERNAME_KEY);
        }
        PlayerPrefs.Save();
    }

    private void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void SetInteractable(bool state)
    {
        inputUsername.interactable = state;
        inputPassword.interactable = state;
        toggleRememberMe.interactable = state;
    }

    private string GetErrorMessage(string raw)
    {
        if (raw.Contains("ENTITY_EXISTS") || raw.Contains("already exists"))
            return "Username already taken.";
        if (raw.Contains("INVALID_PASSWORD"))
            return "Password requires:\n• 1 uppercase\n• 1 lowercase\n• 1 number\n• 1 symbol (e.g. !@#$)";
        if (raw.Contains("INVALID_PARAMETERS") || raw.Contains("credentials") || raw.Contains("INVALID"))
            return "Invalid username or password.";
        if (raw.Contains("RATE_LIMITED"))
            return "Too many attempts. Try again later.";
        return "Something went wrong. Try again.";
    }
}