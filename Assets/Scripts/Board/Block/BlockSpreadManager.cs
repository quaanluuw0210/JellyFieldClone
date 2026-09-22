using System.Collections.Generic;
using UnityEngine;

public enum SpreadDirection
{
    Horizontal,
    Vertical
}

public class BlockSpreadManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private SpreadDirection preferredDirection = SpreadDirection.Horizontal;


    private readonly JellyBlockBase[,] gridSlots = new JellyBlockBase[2, 2];

    public List<JellyBlockBase> RecoverShape(IReadOnlyList<JellyBlockBase> livingSubBlocks)
    {
        ClearGrid();
        RegisterAll(livingSubBlocks);

        // Gọi log kiểm tra
        FindEmptySlot(livingSubBlocks);
        FindSpreadedJellyBlock();


        SimulateSpreadSteps(livingSubBlocks);

        // Trả về danh sách tạm thời (chưa xử lý dãn)
        return CopyValidBlocks(livingSubBlocks);
    }

    /// <summary>
    /// Tìm và Log ra các vị trí ô trống (1: [0,0], 2: [0,1], 3: [1,0], 4: [1,1])
    /// </summary>
    public int FindEmptySlot(IReadOnlyList<JellyBlockBase> livingSubBlocks)
    {
        int emptyCount = 0;

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if (gridSlots[x, y] == null)
                {
                    int slotIndex = GetSlotIndex(x, y);
                    Debug.Log($"[FindEmptySlot] Ô trống tại Vị trí {slotIndex} -> gridSlots[{x},{y}]");
                    emptyCount++;
                }
            }
        }

        return emptyCount;
    }

    /// <summary>
    /// Log ra vị trí và thông tin của các khối Jelly đang chiếm giữ trong Grid
    /// </summary>
    public int FindSpreadedJellyBlock()
    {
        HashSet<JellyBlockBase> loggedBlocks = new HashSet<JellyBlockBase>();
        int count = 0;

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                JellyBlockBase jelly = gridSlots[x, y];
                if (jelly != null && loggedBlocks.Add(jelly))
                {
                    count++;
                    if (jelly is JellySingleBlock)
                    {
                        int slotIndex = GetSlotIndex(x, y);
                        Debug.Log($"[FindSpreadedJellyBlock] Single Block '{jelly.name}' đang ở Vị trí {slotIndex} -> [{x},{y}]");
                    }
                    else if (jelly is JellyDoubleBlock doubleBlock)
                    {
                        bool isHorizontal = doubleBlock.BlockScale.x >= doubleBlock.BlockScale.z;
                        string orientation = isHorizontal ? "Ngang" : "Dọc";

                        // Tìm 2 ô mà Double Block này đang chiếm
                        List<int> slots = GetSlotsForBlock(jelly);
                        Debug.Log($"[FindSpreadedJellyBlock] Double Block ({orientation}) '{jelly.name}' đang chiếm các Vị trí: {string.Join(", ", slots)}");
                    }
                    else if (jelly is JellyFullBlock)
                    {
                        Debug.Log($"[FindSpreadedJellyBlock] Full Block '{jelly.name}' đang chiếm toàn bộ 4 Vị trí (1, 2, 3, 4)");
                    }
                }
            }
        }

        return count;
    }

    // --- CÁC HÀM BỔ TRỢ ĐÃ CHUẨN HÓA THEO TRỤC X (-0.25 / 0.25) VÀ Z (0.25 / -0.25) ---

    private void RegisterAll(IReadOnlyList<JellyBlockBase> blocks)
    {
        if (blocks == null) return;

        foreach (JellyBlockBase jelly in blocks)
        {
            if (jelly == null) continue;

            if (jelly is JellyFullBlock)
            {
                for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++) gridSlots[x, y] = jelly;
            }
            else if (jelly is JellyDoubleBlock doubleBlock)
            {
                bool isHorizontal = doubleBlock.BlockScale.x >= doubleBlock.BlockScale.z;
                Vector3 pos = jelly.transform.localPosition;

                if (isHorizontal)
                {
                    // Ngang: pos.z > 0 là Hàng trên (y=0, ô 1 & 3), pos.z < 0 là Hàng dưới (y=1, ô 2 & 4)
                    int y = pos.z >= 0f ? 0 : 1;
                    gridSlots[0, y] = jelly; // Trái
                    gridSlots[1, y] = jelly; // Phải
                }
                else
                {
                    // Dọc: pos.x < 0 là Cột trái (x=0, ô 1 & 2), pos.x > 0 là Cột phải (x=1, ô 3 & 4)
                    int x = pos.x >= 0f ? 1 : 0;
                    gridSlots[x, 0] = jelly; // Trên
                    gridSlots[x, 1] = jelly; // Dưới
                }
            }
            else // JellySingleBlock
            {
                Vector2Int slot = GetNearestSlot(jelly.transform.localPosition);
                gridSlots[slot.x, slot.y] = jelly;
            }
        }
    }

    private Vector2Int GetNearestSlot(Vector3 localPos)
    {
        // x < 0 là bên trái (x=0), x >= 0 là bên phải (x=1)
        int x = localPos.x >= 0f ? 1 : 0;
        // z >= 0 là hàng trên (y=0), z < 0 là hàng dưới (y=1)
        int y = localPos.z >= 0f ? 0 : 1;
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Chuyển đổi [x,y] sang số thứ tự 1, 2, 3, 4 theo quy ước:
    /// [0,0] -> 1 (Top-Left)
    /// [0,1] -> 2 (Bottom-Left)
    /// [1,0] -> 3 (Top-Right)
    /// [1,1] -> 4 (Bottom-Right)
    /// </summary>
    private int GetSlotIndex(int x, int y)
    {
        if (x == 0 && y == 0) return 1;
        if (x == 0 && y == 1) return 2;
        if (x == 1 && y == 0) return 3;
        if (x == 1 && y == 1) return 4;
        return -1;
    }

    private List<int> GetSlotsForBlock(JellyBlockBase target)
    {
        List<int> slots = new List<int>();
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if (gridSlots[x, y] == target)
                {
                    slots.Add(GetSlotIndex(x, y));
                }
            }
        }
        return slots;
    }

    private void ClearGrid()
    {
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                gridSlots[x, y] = null;
    }

    private List<JellyBlockBase> CopyValidBlocks(IReadOnlyList<JellyBlockBase> source)
    {
        List<JellyBlockBase> result = new List<JellyBlockBase>();
        if (source == null) return result;
        HashSet<JellyBlockBase> unique = new HashSet<JellyBlockBase>();
        foreach (var j in source)
        {
            if (j != null && unique.Add(j)) result.Add(j);
        }
        return result;
    }



    ///////////
    ///

    /// <summary>
    /// Vòng lặp mô phỏng quá trình dãn (Spread) từng bước cho đến khi lấp đầy hoặc không thể dãn tiếp.
    /// Hàm chỉ Log ra các bước dự kiến, KHÔNG thay đổi dữ liệu thật hay khởi tạo GameObject.
    /// </summary>
    public void SimulateSpreadSteps(IReadOnlyList<JellyBlockBase> livingSubBlocks)
    {
        // 1. Khởi tạo mảng giả lập để mô phỏng
        JellyBlockBase[,] simGrid = new JellyBlockBase[2, 2];
        ClearGrid();
        RegisterAll(livingSubBlocks);

        // Copy dữ liệu hiện tại từ gridSlots sang simGrid
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                simGrid[x, y] = gridSlots[x, y];

        int step = 1;
        int maxIterations = 10; // Giới hạn chống lặp vô tận

        Debug.Log("=== BẮT ĐẦU MÔ PHỎNG TIẾN TRÌNH LAN (SPREAD SIMULATION) ===");

        while (step <= maxIterations)
        {
            int emptySlots = CountSimEmptySlots(simGrid);
            Debug.Log($"--- [Bước {step}] Ô trống còn lại: {emptySlots}/4 ---");

            if (emptySlots == 0)
            {
                Debug.Log($"[Bước {step}] -> Khung đã ĐẦY (4/4 ô). Kết thúc mô phỏng!");
                break;
            }

            bool expandedInThisStep = false;

            // --- ƯU TIÊN 1: Tìm Single Block có ô trống kế bên để dãn thành Double ---
            for (int x = 0; x < 2 && !expandedInThisStep; x++)
            {
                for (int y = 0; y < 2 && !expandedInThisStep; y++)
                {
                    JellyBlockBase block = simGrid[x, y];
                    if (block is JellySingleBlock)
                    {
                        Vector2Int sourcePos = new Vector2Int(x, y);

                        foreach (Vector2Int dir in GetPriorityDirections())
                        {
                            Vector2Int targetPos = sourcePos + dir;

                            if (IsValidSlot(targetPos.x, targetPos.y) && simGrid[targetPos.x, targetPos.y] == null)
                            {
                                int fromSlot = GetSlotIndex(sourcePos.x, sourcePos.y);
                                int toSlot = GetSlotIndex(targetPos.x, targetPos.y);

                                Debug.Log($"[Mô phỏng Bước {step}] CHỌN LAN: Single Block '{block.name}' tại Vị trí {fromSlot} [{sourcePos.x},{sourcePos.y}] " +
                                          $"---> Dãn sang Vị trí {toSlot} [{targetPos.x},{targetPos.y}] (Trở thành Double Block)");

                                // Cập nhật ma trận mô phỏng
                                simGrid[targetPos.x, targetPos.y] = block;
                                expandedInThisStep = true;
                                break;
                            }
                        }
                    }
                }
            }

            // --- ƯU TIÊN 2: Nếu không có Single dãn được, tìm Double Block để dãn thành Full (khi có 2 ô trống) ---
            if (!expandedInThisStep && emptySlots == 2)
            {
                for (int x = 0; x < 2 && !expandedInThisStep; x++)
                {
                    for (int y = 0; y < 2 && !expandedInThisStep; y++)
                    {
                        JellyBlockBase block = simGrid[x, y];
                        if (block is JellyDoubleBlock)
                        {
                            List<int> currentSlots = GetSimSlotsForBlock(simGrid, block);
                            Debug.Log($"[Mô phỏng Bước {step}] CHỌN LAN: Double Block '{block.name}' đang ở các Vị trí {string.Join(", ", currentSlots)} " +
                                      $"---> Dãn lấp đầy toàn bộ 4 ô (Trở thành Full Block)");

                            // Đánh dấu lấp đầy toàn bộ ma trận mô phỏng
                            for (int sx = 0; sx < 2; sx++)
                                for (int sy = 0; sy < 2; sy++)
                                    simGrid[sx, sy] = block;

                            expandedInThisStep = true;
                        }
                    }
                }
            }

            // Nếu không có khối nào thỏa điều kiện dãn thêm
            if (!expandedInThisStep)
            {
                Debug.Log($"[Bước {step}] -> Không còn khối nào có thể lan tiếp. Dừng mô phỏng!");
                break;
            }

            step++;
        }

        Debug.Log("=== KẾT THÚC MÔ PHỎNG LAN ===");
    }

    // --- CÁC HÀM BỔ TRỢ MÔ PHỎNG ---

    private IEnumerable<Vector2Int> GetPriorityDirections()
    {
        if (preferredDirection == SpreadDirection.Horizontal)
        {
            yield return new Vector2Int(1, 0);  // Phải
            yield return new Vector2Int(-1, 0); // Trái
            yield return new Vector2Int(0, 1);  // Dưới
            yield return new Vector2Int(0, -1); // Trên
        }
        else
        {
            yield return new Vector2Int(0, 1);  // Dưới
            yield return new Vector2Int(0, -1); // Trên
            yield return new Vector2Int(1, 0);  // Phải
            yield return new Vector2Int(-1, 0); // Trái
        }
    }

    private int CountSimEmptySlots(JellyBlockBase[,] simGrid)
    {
        int count = 0;
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                if (simGrid[x, y] == null) count++;
        return count;
    }

    private List<int> GetSimSlotsForBlock(JellyBlockBase[,] simGrid, JellyBlockBase target)
    {
        List<int> slots = new List<int>();
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if (simGrid[x, y] == target)
                {
                    slots.Add(GetSlotIndex(x, y));
                }
            }
        }
        return slots;
    }

    private static bool IsValidSlot(int x, int y)
    {
        return x >= 0 && x < 2 && y >= 0 && y < 2;
    }
}