using TMPro;
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
        SceneManager.LoadScene("MainMenu");
    }
}