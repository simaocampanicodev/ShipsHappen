using UnityEngine;
using UnityEngine.EventSystems;

public class AttackCell : MonoBehaviour, IPointerClickHandler
{
    public int x;
    public int y;

    public void OnPointerClick(PointerEventData e)
    {
        GameManager.Instance.OnCellClicked(x, y);
    }
}