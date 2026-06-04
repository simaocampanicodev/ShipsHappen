using UnityEngine;
using UnityEngine.UI;

public class AttackGridManager : MonoBehaviour
{
    public static AttackGridManager Instance;

    [SerializeField] private GameObject attackCellPrefab;
    public GameObject[,] cells = new GameObject[10, 10];

    void Awake()
    {
        Instance = this;
        BuildGrid();
    }

    void BuildGrid()
    {
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                var cell = Instantiate(attackCellPrefab, transform);
                cell.name = $"AtkCell_{x}_{y}";

                // liga o AttackCell
                var attackCell = cell.GetComponent<AttackCell>();
                attackCell.x = x;
                attackCell.y = y;

                // liga o botao por codigo
                int capturedX = x;
                int capturedY = y;
                var button = cell.GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    GameManager.Instance.OnCellClicked(capturedX, capturedY);
                });

                cells[x, y] = cell;
            }
        }
    }
}