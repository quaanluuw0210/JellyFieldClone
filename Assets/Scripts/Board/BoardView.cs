using DG.Tweening.Core.Easing;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private BlockFactory blockFactory;


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

    public Block SpawnBlockAt(BoardCellData blockData)
    {
        if (blockData == null || blockFactory == null || gridSystem == null) return null;

        Vector2Int gridPos = new Vector2Int(blockData.x, blockData.y);
        if (!gridSystem.IsValidPosition(gridPos)) return null;

        Cell cell = gridSystem.GetCell(gridPos);
        if (cell == null || cell.HasBlock()) return null;

        Block blockView = blockFactory.Create(blockData);
        if (blockView == null) return null;

        Vector3 spawnPosition = GetWorldPositionForCell(gridPos) + Vector3.up * 0.1f;
        blockView.transform.SetParent(transform);
        blockView.transform.position = spawnPosition;
        blockView.name = string.Format("Block_{0}_{1}", gridPos.x, gridPos.y);
        blockView.InitializePlacedState(cell);
        blockView.SetPlaced(true);

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null) blockView.OnBlockDropped += gameManager.HandleBlockDropped;

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
                if (GameManager.instance != null)
                {
                    blockView.OnBlockDropped -= GameManager.instance.HandleBlockDropped;
                }

                if (gridSystem != null)
                {
                   
                    Vector2Int gridPos = gridSystem.GetGridPosition(blockView.transform.position);
                    Cell cell = gridSystem.GetCell(gridPos);

                    if (cell != null)
                    {
                        cell.ClearBlock(blockView);
                    }
                }

                blockView.ClearPlacement();
                DestroyVisual(blockView.gameObject);
            }
        }

        cellViews.Clear();
        blockViews.Clear();

        Block[] childBlocks = GetComponentsInChildren<Block>(true);
        foreach (Block child in childBlocks)
        {
            if (child != null)
            {
                child.ClearPlacement();
                DestroyVisual(child.gameObject);
            }
        }
    }

    public void InitializeBoard(LevelData levelData, List<BoardCellData> boardSetup)
    {
        if (levelData == null || gridSystem == null) return;
        ClearBoardVisuals();

        // 1. Khởi tạo dữ liệu ô cờ trong GridSystem từ danh sách vị trí hợp lệ
        gridSystem.InitializeGrid(levelData.validCellPositions);

        // 2. Sinh Visual cho các Cell
        GenerateBoardVisuals();

        // 3. Sinh các Block đặt sẵn từ JSON thông qua BlockFactory.
        if (boardSetup != null)
        {
            foreach (BoardCellData blockData in boardSetup)
            {
                SpawnBlockAt(blockData);
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

    /// </summary>
    public bool IsBoardFull()
    {
        if (gridSystem == null) return false;

        var allCells = gridSystem.GetAllCells();

       
        if (allCells == null || allCells.Count() == 0) return false;

        foreach (Cell cell in allCells)
        {
            // Nếu tìm thấy ít nhất 1 ô hợp lệ chưa có Block 
            if (cell != null && !cell.HasBlock())
            {
                return false;
            }
        }

        // Tất cả các ô hợp lệ đều đã có Block
        return true;
    }
    public void RegisterPlacedBlock(Block block)
    {
        if (block != null && !blockViews.Contains(block))
        {
            blockViews.Add(block);
        }
    }
}