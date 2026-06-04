using System.Collections.Generic;
using UnityEngine;

public class ShipStatusPanel : MonoBehaviour
{
    public static ShipStatusPanel Instance;

    [System.Serializable]
    public class ShipEntry
    {
        public GameObject shipObject;
        public int shipSize;
        [HideInInspector] public List<Vector2Int> cells = new List<Vector2Int>();
        [HideInInspector] public int hitCount = 0;
        [HideInInspector] public bool sunk = false;
    }

    [SerializeField] private List<ShipEntry> ships;

    void Awake()
    {
        Instance = this;
    }

    public void RegisterShipsExact(List<List<Vector2Int>> shipCellGroups)
    {
        // reseta tudo
        foreach (var s in ships)
        {
            s.cells.Clear();
            s.hitCount = 0;
            s.sunk = false;
            s.shipObject.SetActive(true);
        }

        // ordena os grupos por tamanho
        shipCellGroups.Sort((a, b) => a.Count.CompareTo(b.Count));

        // ordena os ShipEntry por tamanho
        List<ShipEntry> sortedShips = new List<ShipEntry>(ships);
        sortedShips.Sort((a, b) => a.shipSize.CompareTo(b.shipSize));

        for (int i = 0; i < Mathf.Min(shipCellGroups.Count, sortedShips.Count); i++)
        {
            sortedShips[i].cells = shipCellGroups[i];
            Debug.Log($"Ship size {sortedShips[i].shipSize} registered at cells: {string.Join(", ", shipCellGroups[i])}");
        }
    }

    private void FloodFill(bool[,] grid, bool[,] visited, int x, int y, List<Vector2Int> result)
    {
        if (x < 0 || x >= 10 || y < 0 || y >= 10) return;
        if (!grid[x, y] || visited[x, y]) return;

        visited[x, y] = true;
        result.Add(new Vector2Int(x, y));

        FloodFill(grid, visited, x + 1, y, result);
        FloodFill(grid, visited, x - 1, y, result);
        FloodFill(grid, visited, x, y + 1, result);
        FloodFill(grid, visited, x, y - 1, result);
    }

    public void RegisterHit(int x, int y)
    {
        foreach (var entry in ships)
        {
            if (entry.sunk) continue;

            foreach (var cell in entry.cells)
            {
                if (cell.x == x && cell.y == y)
                {
                    entry.hitCount++;

                    if (entry.hitCount >= entry.shipSize)
                    {
                        entry.sunk = true;
                        entry.shipObject.SetActive(false);
                        Debug.Log($"Ship size {entry.shipSize} sunk!");
                    }
                    return;
                }
            }
        }
    }
}