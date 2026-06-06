using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultSceneController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private bool isVictory;

    void Start()
    {
        if (isVictory)
        {
            resultText.text = "Victory";
            resultText.color = Color.green;
        }
        else
        {
            resultText.text = "Loss";
            resultText.color = Color.red;
        }
    }

    public void OnClickMenu()
    {
        if (NetworkManager.Singleton != null)
            Destroy(NetworkManager.Singleton.gameObject);

        SceneManager.LoadScene("MainMenu");
    }
}