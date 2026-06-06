using System.Collections;
using System.Collections.Generic;
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
        if (!GridManager.Instance.AllShipsPlaced())
        {
            Debug.LogWarning("Still ships to place");
            return;
        }

        localConfirmed = true;
        buttonConfirm.SetActive(false);

        // recolhe as celulas de cada barco individualmente
        List<ShipDragger> allShips = new List<ShipDragger>(
            FindObjectsByType<ShipDragger>()
        );

        List<List<Vector2Int>> shipCellGroups = new List<List<Vector2Int>>();
        foreach (var ship in allShips)
        {
            var cells = ship.GetOccupiedCells();
            if (cells.Count > 0)
                shipCellGroups.Add(cells);
        }

        // guarda no GameData para usar na GameScene
        GameData.Instance.MyShipCellGroups = shipCellGroups;

        // converte o array 2D para flat para enviar pela rede
        bool[] shipsFlat = new bool[100];
        for (int x = 0; x < 10; x++)
            for (int y = 0; y < 10; y++)
                shipsFlat[x + y * 10] = GridManager.Instance.occupied[x, y];

        SendShipsToServerServerRpc(shipsFlat, NetworkManager.Singleton.LocalClientId);
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SendShipsToServerServerRpc(bool[] shipsFlat, ulong clientId)
    {
        bool[,] ships = new bool[10, 10];
        for (int i = 0; i < 100; i++)
            ships[i % 10, i / 10] = shipsFlat[i];

        GameData.Instance.SetShips(clientId, ships);
    }
}