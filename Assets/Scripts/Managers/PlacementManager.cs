using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlacementManager : NetworkBehaviour
{
    public static PlacementManager Instance;

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject buttonConfirm;

    private NetworkVariable<int> confirmedCount = new NetworkVariable<int>(0);

    private float timeLeft = 120f;
    private bool timerRunning = false;
    private bool localConfirmed = false;

    // ultimo barco inserido na grid, usado pelo botao rotate
    public ShipDragger LastPlacedShip { get; set; }
    public bool IsConfirmed => localConfirmed;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // so o server corre o timer
        if (IsServer)
        {
            timerRunning = true;
        }

        confirmedCount.OnValueChanged += OnConfirmedCountChanged;
    }

    void Update()
    {
        if (!IsServer || !timerRunning) return;

        timeLeft -= Time.deltaTime;

        // timer para ambos
        UpdateTimerClientRpc(timeLeft);

        if (timeLeft <= 0)
        {
            timerRunning = false;
            TimeOutClientRpc();
        }
    }

    [ClientRpc]
    private void UpdateTimerClientRpc(float time)
    {
        int seconds = Mathf.CeilToInt(time);
        timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        timerText.color = time <= 30f ? Color.red : Color.black;
    }

    [ClientRpc]
    private void TimeOutClientRpc()
    {
        // timer acaba sem estar confirmado, volta para o menu
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }

    public void OnClickConfirm()
    {
        if (localConfirmed) return;

        // check dos barcos
        if (!GridManager.Instance.AllShipsPlaced())
        {
            Debug.LogWarning("Still ships to place");
            return;
        }

        localConfirmed = true;

        // esconde o botao de confirm
        buttonConfirm.SetActive(false);

        ConfirmServerRpc();
    }

    // botao rotate chama este metodo, que delega no ultimo barco colocado
    public void OnClickRotate()
    {
        if (localConfirmed) return;
        if (LastPlacedShip == null) return;
        LastPlacedShip.Rotate();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ConfirmServerRpc()
    {
        confirmedCount.Value++;
    }

    private void OnConfirmedCountChanged(int previous, int current)
    {
        // se ambos confirmarem
        if (current >= 2)
        {
            if (IsServer)
            {
                timerRunning = false;
                // envia os dados para o GameData antes de mudar de cena
                SendShipDataClientRpc();
            }
        }
    }

    [ClientRpc]
    private void SendShipDataClientRpc()
    {
        // copia os barcos locais para o GameData
        GameData.Instance.MyShips = GridManager.Instance.occupied;
        StartCoroutine(LoadGameSceneCR());
    }

    private IEnumerator LoadGameSceneCR()
    {
        yield return new WaitForSeconds(0.5f);
        if (IsServer)
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
}