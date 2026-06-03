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
    private int hitCount = 0;

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
        turnText.color = isMyTurn ? Color.green : Color.red;

        gridAttack.GetComponent<CanvasGroup>().blocksRaycasts = isMyTurn;
        gridAttack.GetComponent<CanvasGroup>().alpha = isMyTurn ? 1f : 0.6f;
    }

    // chamado pelo AttackCell quando o jogador clica numa celula
    public void OnCellClicked(int x, int y)
    {
        if (currentTurn.Value != myPlayerIndex) return;
        if (attackGrid[x, y]) return; // atacado

        attackGrid[x, y] = true;
        SendAttackServerRpc(x, y, myPlayerIndex);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SendAttackServerRpc(int x, int y, int attackerIndex)
    {
        // passa o ataque para todos os clientes
        ResolveAttackClientRpc(x, y, attackerIndex);
    }

    [ClientRpc]
    private void ResolveAttackClientRpc(int x, int y, int attackerIndex)
    {
        StartCoroutine(ResolveAttackCR(x, y, attackerIndex));
    }

    private IEnumerator ResolveAttackCR(int x, int y, int attackerIndex)
    {
        yield return new WaitForSeconds(1f);

        bool isMyAttack = attackerIndex == myPlayerIndex;

        if (isMyAttack)
        {
            // resultado do meu ataque na grid de ataque
            // servidor sabe os barcos do inimigo via GameData
            bool isHit = GameData.Instance.EnemyShips[x, y];
            SpawnMarker(x, y, isHit ? greenMarkerPrefab : redMarkerPrefab, attackMarkersParent, true);

            if (isHit && IsServer)
            {
                hitCount++;
                if (hitCount >= totalEnemyShipCells)
                {
                    gameOver.Value = true;
                    DeclareResultClientRpc(attackerIndex);
                    yield break;
                }
            }
        }
        else
        {
            // ataque do inimigo na minha grid de defesa
            bool isHit = myShips[x, y];
            SpawnMarker(x, y, isHit ? purpleMarkerPrefab : blackMarkerPrefab, defenseMarkersParent, false);
        }

        // passa o turno
        if (IsServer && !gameOver.Value)
        {
            yield return new WaitForSeconds(0.5f);
            currentTurn.Value = attackerIndex == 0 ? 1 : 0;
        }
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