
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
    [SerializeField] private BlockSpreadManager spreadManager;

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

    public event Action<Block, Vector2Int> OnBlockDropped;
    public event Action<Block> OnBlockCleanedUp;

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

    public bool IsFullJelly
    {
        get
        {
            return subBlocks.Count == 1 && subBlocks[0] is JellyFullBlock;
        }
    }

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

        if (spreadManager == null)
            spreadManager = GetComponent<BlockSpreadManager>();
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
              
            });
    }

    /// <summary>
    /// Khai báo và đăng ký Block vào Cell ngay khi được sinh ra trực tiếp trên bàn chơi.
    /// </summary>
    public void InitializePlacedState(Cell cell)
    {
        if (cell == null) return;

        RegisterInCell(cell);
        originalPosition = transform.position;
        hasBeenPlaced = true;
    }

    public void ClearPlacement()
    {
        ClearPlacementAt(transform.position);
    }

    public List<JellyBlockBase> GetSubBlocksTouchingEdge(Vector2Int direction)
    {
        List<JellyBlockBase> touchingBlocks = new List<JellyBlockBase>();
        const float edgeTolerance = 0.05f;

        foreach (JellyBlockBase subBlock in subBlocks)
        {
            if (subBlock == null) continue;
            if (!TryGetLocalBounds(subBlock, out Bounds bounds)) continue;

            bool touchesEdge = direction == Vector2Int.right && bounds.max.x >= 0.5f - edgeTolerance;
            touchesEdge |= direction == Vector2Int.left && bounds.min.x <= -0.5f + edgeTolerance;
            touchesEdge |= direction == Vector2Int.up && bounds.max.z >= 0.5f - edgeTolerance;
            touchesEdge |= direction == Vector2Int.down && bounds.min.z <= -0.5f + edgeTolerance;

            if (touchesEdge) touchingBlocks.Add(subBlock);
        }

        return touchingBlocks;
    }

    ///// <summary>
    ///// Kiểm tra hai Jelly ở hai Block kề nhau có thật sự đối diện trên cùng
    ///// đoạn biên hay chỉ chạm cùng một cạnh nhưng lệch góc.
    ///// </summary>
    //public bool IsSubBlockAlignedAcrossEdge(
    //    JellyBlockBase localSubBlock,
    //    Block neighborBlock,
    //    JellyBlockBase neighborSubBlock,
    //    Vector2Int direction)
    //{
    //    if (localSubBlock == null || neighborBlock == null || neighborSubBlock == null)
    //    {
    //        return false;
    //    }

    //    if (!TryGetLocalBounds(localSubBlock, out Bounds localBounds) ||
    //        !neighborBlock.TryGetLocalBounds(neighborSubBlock, out Bounds neighborBounds))
    //    {
    //        return false;
    //    }

    //    const float overlapTolerance = 0.001f;
    //    if (direction == Vector2Int.left || direction == Vector2Int.right)
    //    {
    //        return localBounds.min.z <= neighborBounds.max.z + overlapTolerance &&
    //               localBounds.max.z >= neighborBounds.min.z - overlapTolerance;
    //    }

    //    if (direction == Vector2Int.up || direction == Vector2Int.down)
    //    {
    //        return localBounds.min.x <= neighborBounds.max.x + overlapTolerance &&
    //               localBounds.max.x >= neighborBounds.min.x - overlapTolerance;
    //    }

    //    return false;
    //}

    // Trong Block.cs

    /// <summary>
    /// Kiểm tra xem 2 SubBlock thuộc 2 Block lân cận có đối diện trực tiếp (thẳng hàng) với nhau qua cạnh tiếp xúc hay không.
    /// </summary>
    /// <summary>
    /// Kiểm tra hai Jelly ở hai Block kề nhau có thật sự đối diện trên cùng
    /// đoạn biên hay chỉ chạm cùng một cạnh nhưng lệch góc.
    /// </summary>
    public bool IsSubBlockAlignedAcrossEdge(
        JellyBlockBase localSubBlock,
        Block neighborBlock,
        JellyBlockBase neighborSubBlock,
        Vector2Int direction)
    {
        if (localSubBlock == null || neighborBlock == null || neighborSubBlock == null)
        {
            return false;
        }

        if (!TryGetLocalBounds(localSubBlock, out Bounds localBounds) ||
            !neighborBlock.TryGetLocalBounds(neighborSubBlock, out Bounds neighborBounds))
        {
            return false;
        }

        // Yêu cầu phần giao nhau (Overlap) phải lớn hơn minOverlapThreshold để không bị dính vết chạm góc
        const float minOverlapThreshold = 0.1f;

        if (direction == Vector2Int.left || direction == Vector2Int.right)
        {
            // Tính độ dài phần đè lên nhau theo trục Z
            float overlapMinZ = Mathf.Max(localBounds.min.z, neighborBounds.min.z);
            float overlapMaxZ = Mathf.Min(localBounds.max.z, neighborBounds.max.z);
            float overlapLength = overlapMaxZ - overlapMinZ;

            return overlapLength >= minOverlapThreshold;
        }

        if (direction == Vector2Int.up || direction == Vector2Int.down)
        {
            // Tính độ dài phần đè lên nhau theo trục X
            float overlapMinX = Mathf.Max(localBounds.min.x, neighborBounds.min.x);
            float overlapMaxX = Mathf.Min(localBounds.max.x, neighborBounds.max.x);
            float overlapLength = overlapMaxX - overlapMinX;

            return overlapLength >= minOverlapThreshold;
        }

        return false;
    }
    public void RemoveSubBlocks(List<JellyBlockBase> subBlocksToRemove)
    {
        if (subBlocksToRemove == null || subBlocksToRemove.Count == 0) return;

        bool fullJellyMatched = IsFullJelly && subBlocksToRemove.Contains(subBlocks[0]);
        HashSet<JellyBlockBase> uniqueBlocks = new HashSet<JellyBlockBase>(subBlocksToRemove);

        foreach (JellyBlockBase subBlock in uniqueBlocks)
        {
            if (subBlock == null) continue;
            subBlocks.Remove(subBlock);
            Destroy(subBlock.gameObject);
        }

        if (fullJellyMatched || subBlocks.Count == 0)
        {
            ClearPlacement();
            OnBlockDropped = null;
            OnBlockCleanedUp?.Invoke(this);
            OnBlockCleanedUp = null;
            Destroy(gameObject);
            return;
        }

        RecoverShape();
    }

    public void RecoverShape()
    {
        if (spreadManager == null)
            spreadManager = GetComponent<BlockSpreadManager>();

        if (spreadManager == null)
        {
            Debug.LogWarning($"[Block {name}] Không tìm thấy BlockSpreadManager trên cùng GameObject.", this);
            return;
        }

        // Nhận danh sách subBlocks mới đã được dãn lấp đầy từ Manager
        List<JellyBlockBase> recoveredSubBlocks = spreadManager.RecoverShape(subBlocks);

        if (recoveredSubBlocks != null && recoveredSubBlocks.Count > 0)
        {
            subBlocks.Clear();
            subBlocks.AddRange(recoveredSubBlocks);
        }
    }

    private bool TryGetLocalBounds(JellyBlockBase subBlock, out Bounds localBounds)
    {
        localBounds = default;
        bool hasBounds = false;

        foreach (Renderer renderer in subBlock.GetComponentsInChildren<Renderer>(true))
        {
            AddWorldBoundsToLocalBounds(renderer.bounds, ref localBounds, ref hasBounds);
        }

        if (!hasBounds)
        {
            foreach (Collider collider in subBlock.GetComponentsInChildren<Collider>(true))
            {
                AddWorldBoundsToLocalBounds(collider.bounds, ref localBounds, ref hasBounds);
            }
        }

        return hasBounds;
    }

    private void AddWorldBoundsToLocalBounds(
        Bounds worldBounds,
        ref Bounds localBounds,
        ref bool hasBounds)
    {
        Vector3 min = worldBounds.min;
        Vector3 max = worldBounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z), new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z), new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z)
        };

        foreach (Vector3 corner in corners)
        {
            Vector3 localCorner = transform.InverseTransformPoint(corner);
            if (!hasBounds)
            {
                localBounds = new Bounds(localCorner, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                localBounds.Encapsulate(localCorner);
            }
        }
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