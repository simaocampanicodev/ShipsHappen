using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance;

    public bool[,] MyShips = new bool[10, 10];
    public bool[,] EnemyShips = new bool[10, 10];

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
}