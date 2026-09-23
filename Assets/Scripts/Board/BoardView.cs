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
    }

    /// <summary>
    /// Tạo lại toàn bộ visual Cell theo dữ liệu hiện có trong GridSystem.
    /// </summary>
    public void GenerateBoardVisuals()
    {
        ClearBoardVisuals();

        if (gridSystem == null || cellPrefab == null) return;

        foreach (Cell cell in gridSystem.GetAllCells())
        {
            if (cell == null) continue;

            Vector3 worldPosition = GetWorldPositionForCell(cell.GridPosition);
            GameObject cellView = Instantiate(cellPrefab, worldPosition, Quaternion.identity, transform);
            cellView.name = string.Format("Cell_{0}_{1}", cell.GridPosition.x, cell.GridPosition.y);
            cellViews[cell.GridPosition] = cellView;
        }
    }

    /// <summary>
    /// Sinh một Block tại Cell hợp lệ, cao hơn mặt bàn 0.1 đơn vị để tránh
    /// z-fighting với khung ô và kích hoạt animation xuất hiện.
    /// </summary>
    public Block SpawnBlockAt(Vector2Int gridPos, Block customBlockPrefab)
    {
        Debug.Log($"[SpawnBlockAt] ENTER, gridPos={gridPos}, prefab={customBlockPrefab}");
        if (customBlockPrefab == null) { Debug.LogWarning($"[SpawnBlockAt] prefab null tại {gridPos}"); return null; }

        Block prefabToSpawn = customBlockPrefab;


        if (gridSystem == null) { Debug.LogWarning("[SpawnBlockAt] gridSystem null"); return null; }
        if (!gridSystem.IsValidPosition(gridPos)) { Debug.LogWarning($"[SpawnBlockAt] {gridPos} không phải vị trí hợp lệ trên grid"); return null; }

        Cell cell = gridSystem.GetCell(gridPos);
        if (cell == null) { Debug.LogWarning($"[SpawnBlockAt] cell null tại {gridPos}"); return null; }
        if (cell.HasBlock()) { Debug.LogWarning($"[SpawnBlockAt] cell {gridPos} đã có block rồi"); return null; }

        Vector3 spawnPosition = GetWorldPositionForCell(gridPos);
        spawnPosition += Vector3.up * 0.1f;

        Block blockView = Instantiate(customBlockPrefab, spawnPosition, Quaternion.identity, transform);
        blockView.name = string.Format("Block_{0}_{1}", gridPos.x, gridPos.y);

        blockView.InitializePlacedState(cell);
        blockView.SetPlaced(true);

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            blockView.OnBlockDropped += gameManager.HandleBlockDropped;
        }

        blockViews.Add(blockView);
        return blockView;
    }

    public Vector3 GetWorldPositionForCell(Vector2Int gridPos)
    {
        return gridSystem.GetWorldPosition(gridPos);
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

    public void InitializeBoard(LevelData levelData)
    {
        if (levelData == null || gridSystem == null) return;

        // 1. Khởi tạo dữ liệu ô cờ trong GridSystem từ danh sách vị trí hợp lệ
        gridSystem.InitializeGrid(levelData.validCellPositions);

        // 2. Sinh Visual cho các Cell
        GenerateBoardVisuals();

        // 3. Sinh các Block được đặt sẵn theo LevelData
        if (levelData.placedBlocks != null)
        {
            foreach (var placedData in levelData.placedBlocks)
            {
                Debug.Log($"[InitializeBoard] xét placedData tại {placedData.gridPosition}, prefab = {placedData.blockPrefab}");
                if (placedData.blockPrefab != null)
                {
                    SpawnBlockAt(placedData.gridPosition, placedData.blockPrefab);
                }
            }
        }
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