using UnityEngine;
using UnityEngine.EventSystems;

public class AttackCell : MonoBehaviour, IPointerClickHandler
{
    public int x;
    public int y;

    public void OnPointerClick(PointerEventData e)
    {
        Debug.Log($"Clicked cell {x},{y}");

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null!");
            return;
        }

        GameManager.Instance.OnCellClicked(x, y);
    }
}