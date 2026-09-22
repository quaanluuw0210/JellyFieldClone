using System.Collections.Generic;
using UnityEngine;

public class GridSystem : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 1.0f;
    [SerializeField] private Vector3 originPosition = Vector3.zero;

    // Dictionary lưu trữ chỉ những Cell hợp lệ có trên bàn chơi
    private Dictionary<Vector2Int, Cell> gridMap = new Dictionary<Vector2Int, Cell>();

    public float CellSize => cellSize;

    // Khởi tạo Lưới theo danh sách tọa độ tùy chỉnh (Shape bất kỳ)
    public void InitializeGrid(List<Vector2Int> validPositions)
    {
        gridMap.Clear();

        foreach (Vector2Int pos in validPositions)
        {
            if (!gridMap.ContainsKey(pos))
            {
                Cell newCell = new Cell(pos);
                gridMap.Add(pos, newCell);
            }
        }
    }

    // Khởi tạo Lưới hình chữ nhật tùy chỉnh chiều rộng (Width) x chiều cao (Height)
    public void InitializeRectangularGrid(int width, int height)
    {
        List<Vector2Int> customPositions = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                customPositions.Add(new Vector2Int(x, y));
            }
        }

        InitializeGrid(customPositions);
    }

    // Lấy Cell tại tọa độ bất kỳ (Nếu ngoài bàn chơi hoặc ô bị khuyết -> trả về null)
    public Cell GetCell(Vector2Int gridPosition)
    {
        if (gridMap.TryGetValue(gridPosition, out Cell cell))
        {
            return cell;
        }
        return null;
    }

    // Kiểm tra xem vị trí này có tồn tại trong Bàn chơi hiện tại không
    public bool IsValidPosition(Vector2Int gridPosition)
    {
        return gridMap.ContainsKey(gridPosition);
    }

    // Lấy danh sách các Cell lân cận (Chỉ lấy các ô hợp lệ thực sự tồn tại xung quanh)
    public List<Cell> GetNeighbors(Vector2Int gridPosition)
    {
        List<Cell> neighbors = new List<Cell>();
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),  // Trên
            new Vector2Int(0, -1), // Dưới
            new Vector2Int(-1, 0), // Trái
            new Vector2Int(1, 0)   // Phải
        };

        foreach (var dir in directions)
        {
            Vector2Int neighborPos = gridPosition + dir;
            Cell neighborCell = GetCell(neighborPos);

            // Chỉ thêm nếu ô lân cận đó tồn tại trên bàn chơi
            if (neighborCell != null)
            {
                neighbors.Add(neighborCell);
            }
        }

        return neighbors;
    }

    // Chuyển đổi Tọa độ Grid sang Tọa độ Thế giới
    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        return originPosition + new Vector3(gridPosition.x * cellSize, 0, gridPosition.y * cellSize);
    }

    
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - originPosition;

        // Cộng cellSize * 0.5f để tâm nhận diện nằm chính giữa lòng ô thay vì ở mép ô
        int x = Mathf.FloorToInt((localPos.x + cellSize * 0.5f) / cellSize);
        int y = Mathf.FloorToInt((localPos.z + cellSize * 0.5f) / cellSize);

        return new Vector2Int(x, y);
    }

    // Lấy tất cả các Cell hiện có trên bàn chơi
    public IEnumerable<Cell> GetAllCells()
    {
        return gridMap.Values;
    }

    /// <summary>
    /// Gán dữ liệu Block vào Cell tại vị trí chỉ định
    /// </summary>
    public bool PlaceBlock(Block block, Vector2Int gridPosition)
    {
        Cell cell = GetCell(gridPosition);
        if (cell == null)
        {
            Debug.LogWarning($"[GridSystem] Không tìm thấy Cell tại vị trí {gridPosition}");
            return false;
        }

        // Giả sử class Cell của bạn có phương thức nhận Block hoặc điền các Slot
        // Ví dụ: Đặt block vào các sub-slot tương ứng trong Cell
        block.InitializePlacedState(cell);

        return true;
    }
    private void OnDrawGizmos()
    {
        if (gridMap == null) return;

        Gizmos.color = Color.green;
        foreach (var cell in gridMap.Keys)
        {
            Vector3 worldPos = GetWorldPosition(cell);
            Gizmos.DrawWireCube(worldPos, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
        }
    }
}