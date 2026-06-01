using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShipDragger : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] public int shipSize = 1;

    private bool isHorizontal = true;
    private bool isPlaced = false;
    private List<Vector2Int> occupiedCells = new List<Vector2Int>();

    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Image image;
    private Canvas rootCanvas;
    private RectTransform rootCanvasRect;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        image = GetComponent<Image>();
        originalPosition = transform.localPosition;
        originalParent = transform.parent;
        rootCanvas = GetComponentInParent<Canvas>();
        rootCanvasRect = rootCanvas.GetComponent<RectTransform>();
    }

    void Start()
    {
        StartCoroutine(InitAfterLayout());
    }

    private IEnumerator InitAfterLayout()
    {
        yield return null;
        AdjustSize();
        originalPosition = transform.localPosition;
    }

    private void AdjustSize()
    {
        float cellSize = GridManager.Instance.GetCellSize();
        var rect = GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        // tamanho correto consoante orientacao
        rect.sizeDelta = isHorizontal
            ? new Vector2(cellSize * shipSize, cellSize)
            : new Vector2(cellSize, cellSize * shipSize);
    }

    public void Rotate()
    {
        if (isPlaced)
        {
            // libertar celula anterior
            GridManager.Instance.SetOccupied(occupiedCells, false);
            isPlaced = false;
            occupiedCells.Clear();
        }

        isHorizontal = !isHorizontal;

        // rotate visual via sizeDelta, sem usar transform.rotation
        float cellSize = GridManager.Instance.GetCellSize();
        var rect = GetComponent<RectTransform>();
        rect.sizeDelta = isHorizontal
            ? new Vector2(cellSize * shipSize, cellSize)
            : new Vector2(cellSize, cellSize * shipSize);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (isPlaced)
        {
            GridManager.Instance.SetOccupied(occupiedCells, false);
            occupiedCells.Clear();
            isPlaced = false;
        }

        // sobe ao root do canvas mas preserva scale
        transform.SetParent(rootCanvasRect, true);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f; // semi-transparente durante o drag
    }

    public void OnDrag(PointerEventData e)
    {
        // movimento correto para Scale With Screen Size
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvasRect,
            e.position,
            e.pressEventCamera,
            out Vector2 localPoint
        );
        transform.localPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData e)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        int gx, gy;
        if (!GridManager.Instance.WorldToGrid(e.position, out gx, out gy))
        {
            ReturnToOrigin();
            return;
        }

        var cells = GetCells(gx, gy);

        if (!GridManager.Instance.CanPlace(cells))
        {
            ReturnToOrigin();
            return;
        }

        // ocupar barco
        GridManager.Instance.SetOccupied(cells, true);
        occupiedCells = cells;
        isPlaced = true;

        // posicionar celula no centro
        SnapToCell(gx, gy);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right)
            Rotate();
    }

    private List<Vector2Int> GetCells(int gx, int gy)
    {
        var list = new List<Vector2Int>();
        for (int i = 0; i < shipSize; i++)
        {
            list.Add(isHorizontal
                ? new Vector2Int(gx + i, gy)
                : new Vector2Int(gx, gy - i));
        }
        return list;
    }

    private void SnapToCell(int gx, int gy)
    {
        transform.SetParent(GridManager.Instance.ShipsParent, true);

        // pega a posicao de ecra da celula alvo
        RectTransform cellRect = GridManager.Instance.cells[gx, gy].GetComponent<RectTransform>();
        float cellSize = cellRect.rect.width;

        // converte posicao da celula para localPosition
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, cellRect.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GridManager.Instance.ShipsParent.GetComponent<RectTransform>(),
            screenPoint,
            null,
            out Vector2 localPoint
        );

        // offset para centrar o barco nas celulas que ocupa
        Vector2 offset = isHorizontal
            ? new Vector2(cellSize * (shipSize - 1) / 2f, 0f)
            : new Vector2(0f, -cellSize * (shipSize - 1) / 2f);

        GetComponent<RectTransform>().localPosition = localPoint + offset;
    }

    private void ReturnToOrigin()
    {
        // volta para a posicao original
        transform.SetParent(originalParent, true);
        transform.localPosition = originalPosition;
        isHorizontal = true;
        AdjustSize();
    }
}