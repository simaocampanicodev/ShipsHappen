using UnityEngine;
using UnityEngine.UI;

public class DefenseGridManager : MonoBehaviour
{
    public static DefenseGridManager Instance;

    [SerializeField] private GameObject defenseCellPrefab;
    [SerializeField] private Color shipColor = new Color(0.2f, 0.4f, 0.8f, 1f);
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

    public void ShowShips(bool[,] ships)
    {
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                if (ships[x, y])
                {
                    cells[x, y].GetComponent<Image>().color = shipColor;
                }
            }
        }
    }
}