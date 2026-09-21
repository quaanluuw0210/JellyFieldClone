using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Thành phần điều khiển một khối Jelly mà người chơi có thể kéo và thả.
///
/// Block chỉ chịu trách nhiệm về tương tác, animation và việc đăng ký vào
/// Cell. BoardController/GridSystem có thể lắng nghe OnBlockDropped để xử lý
/// merge hoặc chain reaction sau khi thao tác kéo thả hoàn tất.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class Block : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridSystem gridSystem;
    [SerializeField] private JellyBlockBase jellyBlock;

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

    [Header("Single Block")]
    [SerializeField] private SubSlotIndex singleSlot = SubSlotIndex.TopLeft;

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
    private List<int> previousSlots;
    private Tween movementTween;
    private Tween scaleTween;

    public JellyBlockBase JellyBlock => jellyBlock;
    public bool IsDragging => isDragging;

    /// <summary>
    /// Offset mà BoardView dùng để căn giữa visual so với tọa độ dữ liệu.
    /// </summary>
    public void SetBoardWorldOffset(Vector3 offset)
    {
        boardWorldOffset = offset;
    }

    private void Awake()
    {
        mainCamera = Camera.main;
        originalScale = transform.localScale;

        if (jellyBlock == null)
        {
            jellyBlock = GetComponent<JellyBlockBase>();
        }

        if (gridSystem == null)
        {
            gridSystem = FindObjectOfType<GridSystem>();
        }
    }

    private void Start()
    {
        // Các lớp JellyBlockBase dẫn xuất cũng thiết lập scale trong Awake.
        // Đọc lại ở Start để không phụ thuộc thứ tự Awake giữa các component.
        originalScale = transform.localScale;
    }

    private void OnMouseDown()
    {
        if (isDragging || gridSystem == null) return;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        originalPosition = transform.position;
        originalScale = transform.localScale;
        wasPlacedBeforeDrag = hasBeenPlaced;
        if (wasPlacedBeforeDrag)
        {
            previousGridPosition = gridSystem.GetGridPosition(originalPosition);
            previousSlots = GetOccupiedSlots();
            ClearPlacement();
        }

        isDragging = true;
        dragVelocity = Vector3.zero;

        movementTween?.Kill();
        scaleTween?.Kill();

        // Mặt phẳng kéo luôn nằm ngang. Nhờ vậy độ cao của khối không bị
        // thay đổi theo tia ray của camera khi người chơi rê chuột.
        dragPlane = new Plane(Vector3.up, new Vector3(0f, originalPosition.y + dragHeight, 0f));

        Vector3 liftedPosition = originalPosition + Vector3.up * pickupHeight;
        movementTween = transform.DOMove(liftedPosition, 0.12f).SetEase(Ease.OutQuad);
        scaleTween = transform.DOScale(originalScale * pickupScale, 0.12f).SetEase(Ease.OutQuad);
    }

    private void OnMouseDrag()
    {
        if (!isDragging || mainCamera == null) return;

        Ray pointerRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!dragPlane.Raycast(pointerRay, out float distance)) return;

        Vector3 targetPosition = pointerRay.GetPoint(distance);
        targetPosition.y = originalPosition.y + pickupHeight;

        // SmoothDamp loại bỏ hiện tượng giật khi tốc độ frame và tốc độ chuột
        // không đồng nhất, đồng thời vẫn giữ Block trên cùng một độ cao.
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref dragVelocity,
            dragSmoothTime);
        transform.position = new Vector3(transform.position.x, targetPosition.y, transform.position.z);
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;

        isDragging = false;
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale, 0.12f).SetEase(Ease.OutQuad);

        if (!TryGetDropPosition(out Vector2Int gridPosition, out Cell cell, out List<int> slots))
        {
            ReturnToOriginalPosition();
            return;
        }

        RegisterInCell(cell, slots);

        Vector3 snapPosition = gridSystem.GetWorldPosition(gridPosition) + boardWorldOffset;
        snapPosition.y = originalPosition.y;
        movementTween?.Kill();
        movementTween = transform.DOMove(snapPosition, snapDuration)
            .SetEase(snapEase)
            .OnComplete(() =>
            {
                PlayJiggleAnimation();
                OnBlockDropped?.Invoke(this, gridPosition);
            });
    }

    /// <summary>
    /// Tìm ô gần nhất và danh sách sub-slot mà Block cần chiếm.
    /// </summary>
    private bool TryGetDropPosition(out Vector2Int gridPosition, out Cell cell, out List<int> slots)
    {
        gridPosition = default;
        cell = null;
        slots = new List<int>();

        if (gridSystem == null || jellyBlock == null) return false;

        gridPosition = gridSystem.GetGridPosition(transform.position - boardWorldOffset);
        cell = gridSystem.GetCell(gridPosition);
        if (cell == null) return false;

        slots = GetOccupiedSlots();
        if (slots.Count == 0) return false;

        foreach (int slot in slots)
        {
            if (!cell.IsSlotEmpty(slot)) return false;
        }

        return true;
    }

    /// <summary>
    /// Quy đổi loại block thành các sub-slot cố định của Cell.
    /// Top/Bottom được hiểu theo trục Z; Left/Right theo trục X.
    /// </summary>
    private List<int> GetOccupiedSlots()
    {
        List<int> slots = new List<int>();

        if (jellyBlock is JellySingleBlock)
        {
            slots.Add((int)singleSlot);
            return slots;
        }

        if (jellyBlock is JellyDoubleBlock doubleBlock)
        {
            if (doubleBlock.Orientation == JellyDoubleOrientation.Horizontal)
            {
                slots.Add((int)SubSlotIndex.TopLeft);
                slots.Add((int)SubSlotIndex.TopRight);
            }
            else
            {
                slots.Add((int)SubSlotIndex.TopLeft);
                slots.Add((int)SubSlotIndex.BottomLeft);
            }

            return slots;
        }

        if (jellyBlock is JellyFullBlock)
        {
            slots.Add((int)SubSlotIndex.TopLeft);
            slots.Add((int)SubSlotIndex.TopRight);
            slots.Add((int)SubSlotIndex.BottomLeft);
            slots.Add((int)SubSlotIndex.BottomRight);
        }

        return slots;
    }

    private void RegisterInCell(Cell cell, List<int> slots)
    {
        foreach (int slot in slots)
        {
            cell.PlaceBlock(slot, jellyBlock);
        }

        hasBeenPlaced = true;
    }

    private void ReturnToOriginalPosition()
    {
        movementTween?.Kill();
        movementTween = transform.DOMove(originalPosition, returnDuration)
            .SetEase(returnEase)
            .OnComplete(() =>
            {
                RestorePreviousPlacement();
                PlayJiggleAnimation();
            });
    }

    private void RestorePreviousPlacement()
    {
        if (!wasPlacedBeforeDrag || gridSystem == null || previousSlots == null) return;

        Cell previousCell = gridSystem.GetCell(previousGridPosition);
        if (previousCell == null) return;

        RegisterInCell(previousCell, previousSlots);
        wasPlacedBeforeDrag = false;
    }

    /// <summary>
    /// Bóp scale theo trục dọc/ngang rồi trả về scale ban đầu của prefab.
    /// </summary>
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

    /// <summary>
    /// Cho phép BoardController xóa đăng ký logic trước khi phá hủy Block.
    /// </summary>
    public void ClearPlacement()
    {
        if (!hasBeenPlaced || jellyBlock == null || gridSystem == null) return;

        Vector2Int gridPosition = gridSystem.GetGridPosition(transform.position - boardWorldOffset);
        Cell cell = gridSystem.GetCell(gridPosition);
        if (cell == null) return;

        foreach (int slot in GetOccupiedSlots())
        {
            if (cell.GetBlockAt(slot) == jellyBlock)
            {
                cell.ClearSlot(slot);
            }
        }

        hasBeenPlaced = false;
    }

    private void OnDestroy()
    {
        movementTween?.Kill();
        scaleTween?.Kill();
    }
}