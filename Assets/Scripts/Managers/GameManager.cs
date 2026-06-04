using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private GameObject gridAttack;
    [SerializeField] private GameObject gridDefense;
    [SerializeField] private GameObject shipsStatusPanel;

    private NetworkVariable<int> currentTurn = new NetworkVariable<int>(0); // 0 = host, 1 = client
    private NetworkVariable<bool> gameOver = new NetworkVariable<bool>(false);

    private bool[,] attackGrid = new bool[10, 10];
    private bool[,] defenseGrid = new bool[10, 10];
    private bool[,] myShips = new bool[10, 10];

    // marcadores visuais
    [SerializeField] private GameObject redMarkerPrefab;
    [SerializeField] private GameObject greenMarkerPrefab;
    [SerializeField] private GameObject blackMarkerPrefab;
    [SerializeField] private GameObject purpleMarkerPrefab;
    [SerializeField] private Transform attackMarkersParent;
    [SerializeField] private Transform defenseMarkersParent;

    private int myPlayerIndex;
    private int totalEnemyShipCells = 3 * 1 + 2 * 2 + 3 + 4 + 5; // 21
    private int[] hitCounts = new int[2];
    private bool isWaitingResult = false;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        myPlayerIndex = IsHost ? 0 : 1;

        currentTurn.OnValueChanged += OnTurnChanged;
        gameOver.OnValueChanged += OnGameOverChanged;

        // recebe os barcos do PlacementScene via GameData
        myShips = GameData.Instance.MyShips;
        // mostra os barcos na grid de defesa
        DefenseGridManager.Instance.ShowShips(myShips);

        gridAttack.SetActive(false);
        gridDefense.SetActive(false);
        shipsStatusPanel.SetActive(false);
        UpdateTurnUI(currentTurn.Value);
    }

    private void OnTurnChanged(int previous, int current)
    {
        UpdateTurnUI(current);
    }

    private void UpdateTurnUI(int turn)
    {
        bool isMyTurn = turn == myPlayerIndex;

        turnText.text = isMyTurn ? "My Turn" : "Enemy's Turn";
        turnText.color = isMyTurn ? Color.black : Color.red;

        // grid de ataque so no meu turno
        var cg = gridAttack.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = isMyTurn;
        cg.interactable = isMyTurn;
        cg.alpha = isMyTurn ? 1f : 0.6f;

        gridAttack.SetActive(isMyTurn);
        gridDefense.SetActive(!isMyTurn);
        shipsStatusPanel.SetActive(!isMyTurn);

        attackMarkersParent.gameObject.SetActive(isMyTurn);
        defenseMarkersParent.gameObject.SetActive(!isMyTurn);
    }

    // chamado pelo AttackCell quando o jogador clica numa celula
    public void OnCellClicked(int x, int y)
    {
        if (currentTurn.Value != myPlayerIndex) return;
        if (attackGrid[x, y]) return; // atacado
        if (isWaitingResult) return;

        isWaitingResult = true;
        attackGrid[x, y] = true;
        SendAttackServerRpc(x, y, myPlayerIndex);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SendAttackServerRpc(int x, int y, int attackerIndex)
    {
        // encontra o clientId do inimigo
        ulong attackerClientId = attackerIndex == 0
            ? NetworkManager.Singleton.LocalClientId
            : NetworkManager.Singleton.ConnectedClientsIds[1];

        ulong defenderClientId = attackerIndex == 0
            ? NetworkManager.Singleton.ConnectedClientsIds[1]
            : NetworkManager.Singleton.LocalClientId;

        bool[,] defenderShips = GameData.Instance.GetShips(defenderClientId);
        bool isHit = defenderShips[x, y];

        if (isHit)
        {
            hitCounts[attackerIndex]++;
            Debug.Log($"Player {attackerIndex} hits: {hitCounts[attackerIndex]} / {totalEnemyShipCells}");

            if (hitCounts[attackerIndex] >= totalEnemyShipCells)
            {
                gameOver.Value = true;
                DeclareResultClientRpc(attackerIndex);
                return;
            }
        }

        ResolveAttackClientRpc(x, y, attackerIndex, isHit);
    }

    [ClientRpc]
    private void ResolveAttackClientRpc(int x, int y, int attackerIndex, bool isHit)
    {
        StartCoroutine(ResolveAttackCR(x, y, attackerIndex, isHit));
    }

    private IEnumerator ResolveAttackCR(int x, int y, int attackerIndex, bool isHit)
    {
        yield return new WaitForSeconds(1.5f);
        bool isMyAttack = attackerIndex == myPlayerIndex;

        if (isMyAttack)
        {
            // resultado do meu ataque na grid de ataque
            SpawnMarker(x, y, isHit ? greenMarkerPrefab : redMarkerPrefab, attackMarkersParent, true);
        }
        else
        {
            // ataque do inimigo na minha grid de defesa
            SpawnMarker(x, y, isHit ? purpleMarkerPrefab : blackMarkerPrefab, defenseMarkersParent, false);
            if (isHit)
                ShipStatusPanel.Instance.RegisterHit(x, y);
        }

        // passa o turno
        if (IsServer && !gameOver.Value)
        {
            yield return new WaitForSeconds(1.5f);
            currentTurn.Value = attackerIndex == 0 ? 1 : 0;
        }

        // liberta o bloqueio so para quem atacou
        if (isMyAttack)
            isWaitingResult = false;
    }

    [ClientRpc]
    private void DeclareResultClientRpc(int winnerIndex)
    {
        bool iWon = winnerIndex == myPlayerIndex;

        // analytics
        if (iWon) AnalyticsManager.Instance.RegisterWin();

        StartCoroutine(LoadResultCR(iWon));
    }

    private IEnumerator LoadResultCR(bool won)
    {
        yield return new WaitForSeconds(1.5f);
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene(won ? "VictoryScene" : "LossScene");
    }

    private void SpawnMarker(int x, int y, GameObject prefab, Transform parent, bool isAttackGrid)
    {
        // busca a posicao da celula correta
        GameObject[,] grid = isAttackGrid
            ? AttackGridManager.Instance.cells
            : DefenseGridManager.Instance.cells;

        Vector3 pos = grid[x, y].transform.position;
        Instantiate(prefab, pos, Quaternion.identity, parent);
    }

    private void OnGameOverChanged(bool previous, bool current)
    {
        if (current)
        {
            gridAttack.GetComponent<CanvasGroup>().blocksRaycasts = false;
        }
    }
}