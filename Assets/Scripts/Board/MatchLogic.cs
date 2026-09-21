using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Domain service xử lý match giữa các Cell trên GridSystem.
///
/// Class này không giữ state của một level. Mọi state vẫn nằm trong Cell và
/// GridSystem; MatchLogic chỉ đọc, xóa slot, phát thông báo visual và lặp
/// chain reaction cho tới khi bàn ổn định.
/// </summary>
public static class MatchLogic
{
    /// <summary>
    /// Được gọi sau khi một slot bị xóa khỏi Cell.
    /// </summary>
    public static event Action<JellyBlockBase, Cell, int> OnSubBlockCleared;

    /// <summary>
    /// Được gọi sau khi Cell được tái cấu trúc. Danh sách slot là các slot
    /// hiện còn chứa block sau khi match; visual layer dùng event này để nở
    /// hoặc căn lại hình dạng block.
    /// </summary>
    public static event Action<JellyBlockBase, Cell, IReadOnlyList<int>> OnBlockExpanded;

    /// <summary>
    /// Được gọi trước khi GameObject của block bị Destroy.
    /// </summary>
    public static event Action<JellyBlockBase> OnBlockDestroyed;

    /// <summary>
    /// Kiểm tra match bắt đầu từ Cell vừa đặt, sau đó tiếp tục xử lý các
    /// Cell bị ảnh hưởng cho tới khi không còn cặp màu nào giáp cạnh.
    /// </summary>
    public static bool ProcessMatchAndMerge(GridSystem gridSystem, Vector2Int currentGridPos)
    {
        if (gridSystem == null) return false;

        Cell currentCell = gridSystem.GetCell(currentGridPos);
        if (currentCell == null) return false;

        bool hasMatch = false;
        Queue<Cell> cellsToCheck = new Queue<Cell>();
        HashSet<Cell> queuedCells = new HashSet<Cell>();
        HashSet<JellyBlockBase> destroyedBlocks = new HashSet<JellyBlockBase>();

        EnqueueCell(currentCell, cellsToCheck, queuedCells);
        EnqueueNeighbors(gridSystem, currentCell, cellsToCheck, queuedCells);

        while (cellsToCheck.Count > 0)
        {
            Cell cell = cellsToCheck.Dequeue();
            queuedCells.Remove(cell);

            if (cell == null) continue;

            foreach (Cell neighbor in gridSystem.GetNeighbors(cell.GridPosition))
            {
                if (neighbor == null) continue;

                bool matched = CheckAndClearAdjacents(
                    cell,
                    neighbor,
                    gridSystem,
                    destroyedBlocks,
                    out bool cellChanged,
                    out bool neighborChanged);

                if (!matched) continue;

                hasMatch = true;

                if (cellChanged)
                {
                    RestructureCell(cell);
                }

                if (neighborChanged)
                {
                    RestructureCell(neighbor);
                }

                // Sau khi clear/expand, cả hai Cell và hàng xóm của chúng
                // phải được quét lại để bắt chain reaction mới.
                EnqueueAffectedArea(gridSystem, cell, cellsToCheck, queuedCells);
                EnqueueAffectedArea(gridSystem, neighbor, cellsToCheck, queuedCells);
            }
        }

        return hasMatch;
    }

    /// <summary>
    /// So sánh đúng hai slot nằm trên cùng đường biên giữa hai Cell.
    /// Không bao giờ so sánh slot chéo hoặc slot nằm bên trong Cell.
    /// </summary>
    private static bool CheckAndClearAdjacents(
        Cell cellA,
        Cell cellB,
        GridSystem gridSystem,
        HashSet<JellyBlockBase> destroyedBlocks,
        out bool cellAChanged,
        out bool cellBChanged)
    {
        cellAChanged = false;
        cellBChanged = false;

        if (cellA == null || cellB == null) return false;

        Vector2Int difference = cellB.GridPosition - cellA.GridPosition;
        bool matched = false;

        if (difference == Vector2Int.right)
        {
            matched |= CompareAndClear(cellA, (int)SubSlotIndex.TopRight,
                cellB, (int)SubSlotIndex.TopLeft, gridSystem, destroyedBlocks,
                ref cellAChanged, ref cellBChanged);
            matched |= CompareAndClear(cellA, (int)SubSlotIndex.BottomRight,
                cellB, (int)SubSlotIndex.BottomLeft, gridSystem, destroyedBlocks,
                ref cellAChanged, ref cellBChanged);
        }
        else if (difference == Vector2Int.left)
        {
            matched |= CompareAndClear(cellA, (int)SubSlotIndex.TopLeft,
                cellB, (int)SubSlotIndex.TopRight, gridSystem, destroyedBlocks,
                ref cellAChanged, ref cellBChanged);
            matched |= CompareAndClear(cellA, (int)SubSlotIndex.BottomLeft,
                cellB, (int)SubSlotIndex.BottomRight, gridSystem, destroyedBlocks,
                ref cellAChanged, ref cellBChanged);
        }
        else if (difference == Vector2Int.up)
        {
            matched |= CompareAndClear(cellA, (int)SubSlotIndex.TopLeft,
                cellB, (int)SubSlotIndex.BottomLeft, gridSystem, destroyedBlocks,
                ref cellAChanged, ref cellBChanged);
            matched |= CompareAndClear(cellA, (int)SubSlotIndex.TopRight,
                cellB, (int)SubSlotIndex.BottomRight, gridSystem, destroyedBlocks,
                ref cellAChanged, ref cellBChanged);
        }
        else if (difference == Vector2Int.down)
        {
            matched |= CompareAndClear(cellA, (int)SubSlotIndex.BottomLeft,
                cellB, (int)SubSlotIndex.TopLeft, gridSystem, destroyedBlocks,
                ref cellAChanged, ref cellBChanged);
            matched |= CompareAndClear(cellA, (int)SubSlotIndex.BottomRight,
                cellB, (int)SubSlotIndex.TopRight, gridSystem, destroyedBlocks,
                ref cellAChanged, ref cellBChanged);
        }

        return matched;
    }

    /// <summary>
    /// So sánh màu của một cặp slot đối ứng rồi xóa cả hai nếu cùng màu.
    /// </summary>
    private static bool CompareAndClear(
        Cell cellA,
        int slotA,
        Cell cellB,
        int slotB,
        GridSystem gridSystem,
        HashSet<JellyBlockBase> destroyedBlocks,
        ref bool cellAChanged,
        ref bool cellBChanged)
    {
        if (cellA == null || cellB == null) return false;

        JellyBlockBase blockA = cellA.GetBlockAt(slotA);
        JellyBlockBase blockB = cellB.GetBlockAt(slotB);
        if (blockA == null || blockB == null) return false;

        if (blockA.GetColorAt(slotA) != blockB.GetColorAt(slotB)) return false;

        // Đọc thông tin trước khi clear vì ClearSlot làm mất reference trong
        // Cell, trong khi visual callback vẫn cần biết block vừa bị tác động.
        cellA.ClearSlot(slotA);
        cellB.ClearSlot(slotB);
        cellAChanged = true;
        cellBChanged = true;

        OnSubBlockCleared?.Invoke(blockA, cellA, slotA);
        OnSubBlockCleared?.Invoke(blockB, cellB, slotB);

        DestroyIfNoLongerOnGrid(blockA, gridSystem, destroyedBlocks);
        DestroyIfNoLongerOnGrid(blockB, gridSystem, destroyedBlocks);
        return true;
    }

    /// <summary>
    /// Phát thông báo restructure cho từng block còn sống trong Cell.
    /// Thứ tự slot được duyệt theo chiều dọc trước, sau đó chiều ngang.
    /// </summary>
    private static void RestructureCell(Cell cell)
    {
        if (cell == null) return;

        int[] verticalFirstOrder =
        {
            (int)SubSlotIndex.TopLeft,
            (int)SubSlotIndex.BottomLeft,
            (int)SubSlotIndex.TopRight,
            (int)SubSlotIndex.BottomRight
        };

        Dictionary<JellyBlockBase, List<int>> blockSlots =
            new Dictionary<JellyBlockBase, List<int>>();

        foreach (int slot in verticalFirstOrder)
        {
            JellyBlockBase block = cell.GetBlockAt(slot);
            if (block == null) continue;

            if (!blockSlots.TryGetValue(block, out List<int> slots))
            {
                slots = new List<int>();
                blockSlots.Add(block, slots);
            }

            slots.Add(slot);
        }

        foreach (KeyValuePair<JellyBlockBase, List<int>> entry in blockSlots)
        {
            if (entry.Key == null) continue;

            entry.Key.PlayJiggle();
            OnBlockExpanded?.Invoke(entry.Key, cell, entry.Value.AsReadOnly());
        }
    }

    private static void DestroyIfNoLongerOnGrid(
        JellyBlockBase block,
        GridSystem gridSystem,
        HashSet<JellyBlockBase> destroyedBlocks)
    {
        if (block == null || destroyedBlocks.Contains(block)) return;

        // Một JellyBlock có thể chiếm nhiều slot, vì vậy chỉ destroy khi nó
        // không còn được Cell nào tham chiếu.
        if (IsBlockStillOnGrid(block, gridSystem)) return;

        destroyedBlocks.Add(block);
        OnBlockDestroyed?.Invoke(block);
        UnityEngine.Object.Destroy(block.gameObject);
    }

    private static bool IsBlockStillOnGrid(JellyBlockBase targetBlock, GridSystem gridSystem)
    {
        if (targetBlock == null || gridSystem == null) return false;

        foreach (Cell cell in gridSystem.GetAllCells())
        {
            if (cell == null) continue;

            for (int slot = 0; slot < 4; slot++)
            {
                if (cell.GetBlockAt(slot) == targetBlock) return true;
            }
        }

        return false;
    }

    private static void EnqueueAffectedArea(
        GridSystem gridSystem,
        Cell cell,
        Queue<Cell> cellsToCheck,
        HashSet<Cell> queuedCells)
    {
        if (cell == null) return;

        EnqueueCell(cell, cellsToCheck, queuedCells);
        EnqueueNeighbors(gridSystem, cell, cellsToCheck, queuedCells);
    }

    private static void EnqueueNeighbors(
        GridSystem gridSystem,
        Cell cell,
        Queue<Cell> cellsToCheck,
        HashSet<Cell> queuedCells)
    {
        if (gridSystem == null || cell == null) return;

        foreach (Cell neighbor in gridSystem.GetNeighbors(cell.GridPosition))
        {
            EnqueueCell(neighbor, cellsToCheck, queuedCells);
        }
    }

    private static void EnqueueCell(
        Cell cell,
        Queue<Cell> cellsToCheck,
        HashSet<Cell> queuedCells)
    {
        if (cell == null || queuedCells.Contains(cell)) return;

        queuedCells.Add(cell);
        cellsToCheck.Enqueue(cell);
    }
}