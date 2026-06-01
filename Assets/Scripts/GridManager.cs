using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Transform gridParent;
    public Transform GridParent => gridParent;

    [SerializeField] private Transform shipsParent;
    public Transform ShipsParent => shipsParent;

    public const int SIZE = 10;
    public GameObject[,] cells = new GameObject[SIZE, SIZE];

    // se for true a célula esta ocupada
    public bool[,] occupied = new bool[SIZE, SIZE];

    private int totalShipCells = 3 * 1 + 2 * 2 + 3 + 4 + 5;

    void Awake()
    {
        Instance = this;
        BuildGrid();
    }

    void BuildGrid()
    {
        for (int y = 0; y < SIZE; y++)
        {
            for (int x = 0; x < SIZE; x++)
            {
                var cell = Instantiate(cellPrefab, gridParent);
                cell.name = $"Cell_{x}_{y}";
                cells[x, y] = cell;
            }
        }
    }

    public bool AllShipsPlaced()
    {
        int count = 0;
        for (int y = 0; y < SIZE; y++)
            for (int x = 0; x < SIZE; x++)
                if (occupied[x, y]) count++;
        return count >= totalShipCells;
    }

    public float GetCellSize()
    {
        // força o layout a calcular antes de ler o tamanho
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
            gridParent.GetComponent<RectTransform>()
        );
        return cells[0, 0].GetComponent<RectTransform>().rect.width;
    }

    // converte world para grid
    public bool WorldToGrid(Vector2 screenPos, out int gx, out int gy)
    {
        gx = gy = 0;
        for (int y = 0; y < SIZE; y++)
        {
            for (int x = 0; x < SIZE; x++)
            {
                var rect = cells[x, y].GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(
                    rect, screenPos, null)) // null = Screen Space Overlay
                {
                    gx = x; gy = y;
                    return true;
                }
            }
        }
        return false;
    }

    public void SetOccupied(List<Vector2Int> cells, bool state)
    {
        foreach (var c in cells)
            if (c.x >= 0 && c.x < SIZE && c.y >= 0 && c.y < SIZE)
                occupied[c.x, c.y] = state;
    }

    public bool CanPlace(List<Vector2Int> cells)
    {
        foreach (var c in cells)
        {
            if (c.x < 0 || c.x >= SIZE || c.y < 0 || c.y >= SIZE) return false;
            if (occupied[c.x, c.y]) return false;
        }
        return true;
    }
}