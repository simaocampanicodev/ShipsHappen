using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private GameObject panelMain;
    [SerializeField] private GameObject panelHost;
    [SerializeField] private GameObject buttonJoin;
    [SerializeField] private GameObject joinInputGroup;
    [SerializeField] private GameObject logo;

    [SerializeField] private TextMeshProUGUI textJoinCode;
    [SerializeField] private TMP_InputField inputJoinCode;

    [SerializeField] private UnityTransport transport;

    private bool isCancelled = false;

    void Start()
    {
        panelMain.SetActive(true);
        panelHost.SetActive(false);
    }

    // host panel
    public void OnClickHost()
    {
        isCancelled = false;
        panelMain.SetActive(false);
        panelHost.SetActive(true);
        logo.SetActive(false);
        textJoinCode.text = "Creating room...";
        StartCoroutine(HostGameCR());
    }

    // host game
    private IEnumerator HostGameCR()
    {
        var allocationTask = RelayService.Instance.CreateAllocationAsync(1);
        yield return new WaitUntil(() => allocationTask.IsCompleted);

        if (isCancelled) yield break;

        if (allocationTask.Exception != null)
        {
            textJoinCode.text = "Error creating room.";
            Debug.LogError(allocationTask.Exception);
            yield break;
        }

        Allocation allocation = allocationTask.Result;

        // join code
        var joinCodeTask = RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        yield return new WaitUntil(() => joinCodeTask.IsCompleted);

        if (isCancelled) yield break;

        if (joinCodeTask.Exception != null)
        {
            textJoinCode.text = "Error getting join code.";
            Debug.LogError(joinCodeTask.Exception);
            yield break;
        }

        string joinCode = joinCodeTask.Result;

        // transport com o relay host
        foreach (var endpoint in allocation.ServerEndpoints)
        {
            transport.SetRelayServerData(
                endpoint.Host,
                (ushort)endpoint.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );
            break;
        }

        if (isCancelled) yield break;

        textJoinCode.text = $"Join Code: {joinCode}";

        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
    }

    // começar o jogo com 2 players
    private void OnPlayerJoined(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count == 2)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("PlacementScene", LoadSceneMode.Single);
        }
    }

    // join panel
    public void OnClickJoin()
    {
        buttonJoin.SetActive(false);
        joinInputGroup.SetActive(true);
    }

    // confirm join
    public void OnClickConfirmJoin()
    {
        string code = inputJoinCode.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Join code is empty");
            return;
        }

        StartCoroutine(JoinGameCR(code));
    }

    public void OnClickBack()
    {
        isCancelled = true;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            NetworkManager.Singleton.Shutdown();

        panelHost.SetActive(false);
        panelMain.SetActive(true);
        logo.SetActive(true);
    }

    // join game
    private IEnumerator JoinGameCR(string code)
    {
        var joinTask = RelayService.Instance.JoinAllocationAsync(code);
        yield return new WaitUntil(() => joinTask.IsCompleted);

        if (joinTask.Exception != null)
        {
            Debug.LogError("Invalid join code: " + joinTask.Exception);
            yield break;
        }

        JoinAllocation allocation = joinTask.Result;

        // transport com o relay client
        foreach (var endpoint in allocation.ServerEndpoints)
        {
            transport.SetRelayServerData(
                endpoint.Host,
                (ushort)endpoint.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );
            break;
        }

        NetworkManager.Singleton.StartClient();
    }

    // quit
    public void OnClickQuit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}