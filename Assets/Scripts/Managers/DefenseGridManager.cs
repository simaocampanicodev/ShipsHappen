using UnityEngine;

public class DefenseGridManager : MonoBehaviour
{
    public static DefenseGridManager Instance;

    [SerializeField] private GameObject defenseCellPrefab;
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
                var cell = Instantiate(defenseCellPrefab, transform);
                cell.name = $"DefCell_{x}_{y}";
                cells[x, y] = cell;
            }
        }
    }
}