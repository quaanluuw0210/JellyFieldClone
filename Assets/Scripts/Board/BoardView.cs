using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lớp hiển thị 3D của bàn chơi.
/// BoardView không tự lưu trạng thái Cell; nó chỉ đọc GridSystem rồi tạo và
/// quản lý các GameObject hiển thị tương ứng trong scene.
/// </summary>
public class BoardView : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private GridSystem gridSystem;

    [Header("Prefabs")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Block blockPrefab;

    [Header("Layout")]
    [SerializeField] private bool centerBoard = true;

    [Header("Startup Test")]
    [SerializeField] private bool runHardcodedTestOnStart;

    private readonly Dictionary<Vector2Int, GameObject> cellViews =
        new Dictionary<Vector2Int, GameObject>();
    private readonly List<Block> blockViews = new List<Block>();

    public IReadOnlyDictionary<Vector2Int, GameObject> CellViews => cellViews;

    private void Awake()
    {
        if (gridSystem == null)
        {
            gridSystem = FindFirstObjectByType<GridSystem>();
        }
    }

    private void Start()
    {
        if (runHardcodedTestOnStart)
        {
            TestSpawnHardcodedBoard();
            return;
        }

        GenerateBoardVisuals();
    }

    /// <summary>
    /// Tạo lại toàn bộ visual Cell theo dữ liệu hiện có trong GridSystem.
    /// </summary>
    public void GenerateBoardVisuals()
    {
        ClearBoardVisuals();

        if (gridSystem == null || cellPrefab == null) return;

        Vector3 centerOffset = GetBoardCenterOffset();
        foreach (Cell cell in gridSystem.GetAllCells())
        {
            if (cell == null) continue;

            Vector3 worldPosition = GetWorldPositionForCell(cell.GridPosition, centerOffset);
            GameObject cellView = Instantiate(cellPrefab, worldPosition, Quaternion.identity, transform);
            cellView.name = string.Format("Cell_{0}_{1}", cell.GridPosition.x, cell.GridPosition.y);
            cellViews[cell.GridPosition] = cellView;
        }
    }

    /// <summary>
    /// Sinh một Block tại Cell hợp lệ, cao hơn mặt bàn 0.1 đơn vị để tránh
    /// z-fighting với khung ô và kích hoạt animation xuất hiện.
    /// </summary>
    public Block SpawnBlockAt(Vector2Int gridPos)
    {
        if (gridSystem == null || blockPrefab == null) return null;
        if (!gridSystem.IsValidPosition(gridPos)) return null;

        Vector3 spawnPosition = GetWorldPositionForCell(gridPos, GetBoardCenterOffset());
        spawnPosition += Vector3.up * 0.1f;

        Block blockView = Instantiate(blockPrefab, spawnPosition, Quaternion.identity, transform);
        blockView.SetBoardWorldOffset(GetBoardCenterOffset());
        blockView.name = string.Format("Block_{0}_{1}", gridPos.x, gridPos.y);
        blockViews.Add(blockView);
        blockView.PlayJiggleAnimation();
        return blockView;
    }

    /// <summary>
    /// Tính offset đưa tâm của bounding box lưới về vị trí BoardView.
    /// Cách tính theo bounds giúp hoạt động đúng với lưới khuyết hoặc tọa độ
    /// bắt đầu từ giá trị khác 0.
    /// </summary>
    public Vector3 GetBoardCenterOffset()
    {
        if (gridSystem == null || !centerBoard) return Vector3.zero;

        if (!TryGetGridBounds(out Vector2Int minPosition, out Vector2Int maxPosition))
        {
            return Vector3.zero;
        }

        Vector3 minWorld = gridSystem.GetWorldPosition(minPosition);
        Vector3 maxWorld = gridSystem.GetWorldPosition(maxPosition);
        Vector3 boardCenter = (minWorld + maxWorld) * 0.5f;
        return transform.position - boardCenter;
    }

    /// <summary>
    /// Chuyển tọa độ Grid sang World và áp dụng offset căn giữa BoardView.
    /// </summary>
    public Vector3 GetWorldPositionForCell(Vector2Int gridPos, Vector3 centerOffset)
    {
        if (gridSystem == null) return transform.position;
        return gridSystem.GetWorldPosition(gridPos) + centerOffset;
    }

    /// <summary>
    /// Xóa visual cũ trước khi sinh board mới, tránh trùng object và rò rỉ
    /// tham chiếu khi đổi level hoặc regenerate bàn chơi.
    /// </summary>
    public void ClearBoardVisuals()
    {
        foreach (GameObject cellView in cellViews.Values)
        {
            DestroyVisual(cellView);
        }

        foreach (Block blockView in blockViews)
        {
            if (blockView != null)
            {
                blockView.ClearPlacement();
                DestroyVisual(blockView.gameObject);
            }
        }

        cellViews.Clear();
        blockViews.Clear();
    }

    /// <summary>
    /// Tạo lưới 5x5 và sinh Block mẫu ở ô trung tâm (2, 2). Có thể gọi từ
    /// Inspector hoặc bật runHardcodedTestOnStart để kiểm tra pipeline nhanh.
    /// </summary>
    public void TestSpawnHardcodedBoard()
    {
        if (gridSystem == null) return;

        gridSystem.InitializeRectangularGrid(5, 5);
        GenerateBoardVisuals();
        SpawnBlockAt(new Vector2Int(2, 2));
    }

    private bool TryGetGridBounds(out Vector2Int minPosition, out Vector2Int maxPosition)
    {
        minPosition = default;
        maxPosition = default;

        if (gridSystem == null) return false;

        bool hasCell = false;
        foreach (Cell cell in gridSystem.GetAllCells())
        {
            if (cell == null) continue;

            Vector2Int position = cell.GridPosition;
            if (!hasCell)
            {
                minPosition = position;
                maxPosition = position;
                hasCell = true;
                continue;
            }

            minPosition = Vector2Int.Min(minPosition, position);
            maxPosition = Vector2Int.Max(maxPosition, position);
        }

        return hasCell;
    }

    private static void DestroyVisual(Object visual)
    {
        if (visual == null) return;

        if (Application.isPlaying)
        {
            Destroy(visual);
        }
        else
        {
            DestroyImmediate(visual);
        }
    }
}