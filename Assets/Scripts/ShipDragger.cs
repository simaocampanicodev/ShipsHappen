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

    // guarda a celula de ancoragem para o rotate
    private int anchorX = 0;
    private int anchorY = 0;

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
        float shipWidth = GridManager.Instance.GetShipWidth(shipSize);
        var rect = GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        // sempre horizontal
        rect.sizeDelta = new Vector2(shipWidth, cellSize);
    }

    public void Rotate()
    {
        if (PlacementManager.Instance.IsConfirmed) return;
        if (this != PlacementManager.Instance.LastPlacedShip) return;
        if (!isPlaced) return;

        bool tryHorizontal = !isHorizontal;

        // liberta as celulas atuais
        GridManager.Instance.SetOccupied(occupiedCells, false);

        // tenta rodar a partir da ancora atual
        var newCells = GetCellsOriented(anchorX, anchorY, tryHorizontal);

        // se nao couber, tenta ajustar a ancora para que o barco caiba
        if (!GridManager.Instance.CanPlace(newCells))
        {
            newCells = TryFindValidRotation(anchorX, anchorY, tryHorizontal);
        }

        if (newCells != null && GridManager.Instance.CanPlace(newCells))
        {
            isHorizontal = tryHorizontal;
            occupiedCells = newCells;

            // atualiza a ancora para a primeira celula da nova orientacao
            anchorX = newCells[0].x;
            anchorY = newCells[0].y;

            GridManager.Instance.SetOccupied(occupiedCells, true);

            transform.rotation = isHorizontal
                ? Quaternion.identity
                : Quaternion.Euler(0, 0, 90);

            SnapToCell(anchorX, anchorY);
        }
        else
        {
            // nao ha espaco para rodar, volta a ocupar as celulas originais
            GridManager.Instance.SetOccupied(occupiedCells, true);
            Debug.Log("Cannot rotate");
        }
    }

    private List<Vector2Int> TryFindValidRotation(int gx, int gy, bool horizontal)
    {
        // tenta deslocar a ancora ate encontrar uma posicao valida dentro da grid
        int range = shipSize;
        for (int delta = -range; delta <= range; delta++)
        {
            int nx = horizontal ? gx + delta : gx;
            int ny = horizontal ? gy : gy + delta;

            // verifica limites da grid
            if (nx < 0 || ny < 0 || nx >= GridManager.SIZE || ny >= GridManager.SIZE)
                continue;

            var cells = GetCellsOriented(nx, ny, horizontal);

            // verifica se todas as celulas estao dentro da grid
            bool allInBounds = true;
            foreach (var c in cells)
            {
                if (c.x < 0 || c.x >= GridManager.SIZE || c.y < 0 || c.y >= GridManager.SIZE)
                {
                    allInBounds = false;
                    break;
                }
            }

            if (allInBounds && GridManager.Instance.CanPlace(cells))
                return cells;
        }

        return null; // sem posicao valida encontrada
    }

    public void OnBeginDrag(PointerEventData e)
    {
        // nao arrasta se confirmado
        if (PlacementManager.Instance.IsConfirmed) return;

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
        if (PlacementManager.Instance.IsConfirmed) return;

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

        if (PlacementManager.Instance.IsConfirmed) return;

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

        // guarda ancora para o rotate
        anchorX = gx;
        anchorY = gy;

        // regista este barco como o ultimo colocado
        PlacementManager.Instance.LastPlacedShip = this;

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
        return GetCellsOriented(gx, gy, isHorizontal);
    }

    private List<Vector2Int> GetCellsOriented(int gx, int gy, bool horizontal)
    {
        var list = new List<Vector2Int>();
        for (int i = 0; i < shipSize; i++)
        {
            list.Add(horizontal
                ? new Vector2Int(gx + i, gy)
                : new Vector2Int(gx, gy + i));
        }
        return list;
    }

    private void SnapToCell(int gx, int gy)
    {
        transform.SetParent(GridManager.Instance.ShipsParent, true);

        // calcula a posicao media entre todas as celulas que o barco ocupa
        var cellList = GetCellsOriented(gx, gy, isHorizontal);
        Vector2 screenSum = Vector2.zero;

        foreach (var c in cellList)
        {
            RectTransform cr = GridManager.Instance.cells[c.x, c.y].GetComponent<RectTransform>();
            screenSum += RectTransformUtility.WorldToScreenPoint(null, cr.position);
        }

        Vector2 screenCenter = screenSum / cellList.Count;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GridManager.Instance.ShipsParent.GetComponent<RectTransform>(),
            screenCenter,
            null,
            out Vector2 localPoint
        );

        GetComponent<RectTransform>().localPosition = localPoint;
    }

    private void ReturnToOrigin()
    {
        // volta para a posicao original
        transform.SetParent(originalParent, true);
        transform.localPosition = originalPosition;
        transform.rotation = Quaternion.identity;
        isHorizontal = true;
        AdjustSize();
    }
}