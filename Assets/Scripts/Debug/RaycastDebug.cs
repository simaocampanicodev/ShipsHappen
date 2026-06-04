using UnityEngine;

public class RaycastDebug : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            var ped = new UnityEngine.EventSystems.PointerEventData(
                UnityEngine.EventSystems.EventSystem.current)
            {
                position = Input.mousePosition
            };
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(ped, results);

            foreach (var r in results)
                Debug.Log("Hit: " + r.gameObject.name);
        }
    }
}