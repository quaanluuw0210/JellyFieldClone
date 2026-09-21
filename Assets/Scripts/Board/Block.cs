
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider))]
public class Block : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private GridSystem gridSystem;

    [SerializeField] private List<JellyBlockBase> subBlocks = new List<JellyBlockBase>();

    [Header("Drag")]
    [SerializeField] private float dragHeight = 0.35f;
    [SerializeField] private float pickupHeight = 0.15f;
    [SerializeField] private float pickupScale = 1.08f;
    [SerializeField] private float dragSmoothTime = 0.04f;

    [Header("Drop")]
    [SerializeField] private float snapDuration = 0.22f;
    [SerializeField] private float returnDuration = 0.32f;
    [SerializeField] private Ease snapEase = Ease.OutBack;
    [SerializeField] private Ease returnEase = Ease.OutBounce;

    [Header("Jelly Animation")]
    [SerializeField] private float jiggleDuration = 0.2f;
    [SerializeField] private float jiggleStrength = 0.1f;
    [SerializeField] private int jiggleVibrato = 8;

    public event Action<Block, Vector2Int> OnBlockDropped;

    private Camera mainCamera;
    private Plane dragPlane;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Vector3 dragVelocity;
    private Vector3 boardWorldOffset;
    private bool isDragging;
    private bool hasBeenPlaced;
    private bool wasPlacedBeforeDrag;
    private Vector2Int previousGridPosition;
    private Tween movementTween;
    private Tween scaleTween;

    public IReadOnlyList<JellyBlockBase> SubBlocks => subBlocks;
    public bool IsDragging => isDragging;

    public void SetBoardWorldOffset(Vector3 offset)
    {
        boardWorldOffset = offset;
    }

    private void Awake()
    {
        mainCamera = Camera.main;
        originalScale = transform.localScale;

        if (subBlocks.Count == 0)
        {
            JellyBlockBase[] childBlocks = GetComponentsInChildren<JellyBlockBase>(true);
            foreach (JellyBlockBase childBlock in childBlocks)
            {
                if (childBlock.transform != transform)
                {
                    subBlocks.Add(childBlock);
                }
            }
        }

        if (gridSystem == null)
            gridSystem = FindFirstObjectByType<GridSystem>();
    }

    private void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.position;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isDragging || gridSystem == null) return;

        if (mainCamera == null) mainCamera = Camera.main;

        // Dừng animation cũ ngay lập tức
        movementTween?.Kill();
        scaleTween?.Kill();

        originalPosition = transform.position;
        originalScale = transform.localScale;
        wasPlacedBeforeDrag = hasBeenPlaced;

        if (wasPlacedBeforeDrag)
        {
            previousGridPosition = gridSystem.GetGridPosition(originalPosition - boardWorldOffset);
            ClearPlacementAt(originalPosition);
        }

        isDragging = true;
        dragVelocity = Vector3.zero;

        dragPlane = new Plane(Vector3.up, new Vector3(0f, originalPosition.y + dragHeight, 0f));

        Vector3 liftedPosition = originalPosition + Vector3.up * pickupHeight;
        movementTween = transform.DOMove(liftedPosition, 0.12f).SetEase(Ease.OutQuad);
        scaleTween = transform.DOScale(originalScale * pickupScale, 0.12f).SetEase(Ease.OutQuad);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || mainCamera == null) return;

        Ray pointerRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!dragPlane.Raycast(pointerRay, out float distance)) return;

        Vector3 targetPosition = pointerRay.GetPoint(distance);
        targetPosition.y = originalPosition.y + pickupHeight;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref dragVelocity,
            dragSmoothTime);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale, 0.12f).SetEase(Ease.OutQuad);

        if (!TryGetDropPosition(out Vector2Int gridPosition, out Cell cell))
        {
            ReturnToOriginalPosition();
            return;
        }

        RegisterInCell(cell);

        Vector3 snapPosition = gridSystem.GetWorldPosition(gridPosition) + boardWorldOffset;
        snapPosition.y = originalPosition.y;

        movementTween?.Kill();
        movementTween = transform.DOMove(snapPosition, snapDuration)
            .SetEase(snapEase)
            .OnComplete(() =>
            {
                originalPosition = transform.position;
                wasPlacedBeforeDrag = false;
                PlayJiggleAnimation();
                OnBlockDropped?.Invoke(this, gridPosition);
            });
    }

    private bool TryGetDropPosition(out Vector2Int gridPosition, out Cell cell)
    {
        gridPosition = default;
        cell = null;

        if (gridSystem == null || subBlocks.Count == 0) return false;

        gridPosition = gridSystem.GetGridPosition(transform.position - boardWorldOffset);
        cell = gridSystem.GetCell(gridPosition);
        if (cell == null) return false;

        return !cell.HasBlock();
    }

    private void RegisterInCell(Cell cell)
    {
        hasBeenPlaced = cell != null && cell.PlaceBlock(this);
    }

    private void ReturnToOriginalPosition()
    {
        isDragging = false;

        movementTween?.Kill();
        movementTween = transform.DOMove(originalPosition, returnDuration)
            .SetEase(returnEase)
            .OnComplete(() =>
            {
                // Phôi phục vị trí cũ trên Grid nếu trước đó đã ở trong Grid
                if (wasPlacedBeforeDrag)
                {
                    Cell previousCell = gridSystem.GetCell(previousGridPosition);
                    if (previousCell != null)
                    {
                        RegisterInCell(previousCell);
                    }
                }

                wasPlacedBeforeDrag = false;
                transform.position = originalPosition; // Reset chuẩn vị trí Y
                PlayJiggleAnimation();
            });
    }

    public void PlayJiggleAnimation()
    {
        scaleTween?.Kill();
        transform.localScale = originalScale;

        scaleTween = transform.DOShakeScale(
                jiggleDuration,
                jiggleStrength,
                jiggleVibrato,
                90f,
                false)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => transform.localScale = originalScale);
    }

    public void ClearPlacement()
    {
        ClearPlacementAt(transform.position);
    }

    private void ClearPlacementAt(Vector3 worldPos)
    {
        if (!hasBeenPlaced || gridSystem == null) return;

        Vector2Int gridPosition = gridSystem.GetGridPosition(worldPos - boardWorldOffset);
        Cell cell = gridSystem.GetCell(gridPosition);
        if (cell == null) return;

        cell.ClearBlock(this);

        hasBeenPlaced = false;
    }

    private void OnDestroy()
    {
        movementTween?.Kill();
        scaleTween?.Kill();
    }
}