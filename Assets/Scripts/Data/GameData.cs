using UnityEngine;
using System.Collections.Generic;

public class GameData : MonoBehaviour
{
    public static GameData Instance;

    private Dictionary<ulong, bool[,]> playerShips = new Dictionary<ulong, bool[,]>();
    public bool[,] MyShips = new bool[10, 10];

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetShips(ulong clientId, bool[,] ships)
    {
        playerShips[clientId] = ships;
    }

    public bool[,] GetShips(ulong clientId)
    {
        return playerShips.ContainsKey(clientId) ? playerShips[clientId] : new bool[10, 10];
    }
}