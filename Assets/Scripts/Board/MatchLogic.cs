using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Domain service cho match giữa các JellyBlockBase nằm trong hai Block.
/// Block là container 1x1; JellyBlockBase mới là đơn vị màu được match.
/// </summary>
public static class MatchLogic
{
    public static event Action<JellyBlockBase, Block> OnSubBlockMatched;
    public static event Action<Block> OnBlockMatched;

    /// <summary>
    /// Horizontal được xử lý trước Vertical. Nếu cả hai trục có match ở cùng
    /// lượt, chỉ match ngang được xóa; lượt quét tiếp theo xử lý chain dọc.
    /// </summary>
    public static bool ProcessMatchAndMerge(GridSystem gridSystem, Vector2Int placedPos)
    {
        if (gridSystem == null) return false;

        Cell placedCell = gridSystem.GetCell(placedPos);
        if (placedCell == null || !placedCell.HasBlock()) return false;

        bool hasAnyMatch = false;
        Queue<Vector2Int> positionsToCheck = new Queue<Vector2Int>();
        HashSet<Vector2Int> queuedPositions = new HashSet<Vector2Int>();
        EnqueuePosition(placedPos, positionsToCheck, queuedPositions);

        while (positionsToCheck.Count > 0)
        {
            Vector2Int position = positionsToCheck.Dequeue();
            queuedPositions.Remove(position);
            Cell sourceCell = gridSystem.GetCell(position);
            if (sourceCell == null || !sourceCell.HasBlock()) continue;

            bool horizontalMatch = ProcessDirection(
                gridSystem, sourceCell, Vector2Int.left, positionsToCheck, queuedPositions);
            horizontalMatch |= ProcessDirection(
                gridSystem, sourceCell, Vector2Int.right, positionsToCheck, queuedPositions);

            if (horizontalMatch)
            {
                hasAnyMatch = true;
                EnqueueAffectedArea(gridSystem, sourceCell, positionsToCheck, queuedPositions);
                continue;
            }

            bool verticalMatch = ProcessDirection(
                gridSystem, sourceCell, Vector2Int.down, positionsToCheck, queuedPositions);
            verticalMatch |= ProcessDirection(
                gridSystem, sourceCell, Vector2Int.up, positionsToCheck, queuedPositions);

            if (verticalMatch)
            {
                hasAnyMatch = true;
                EnqueueAffectedArea(gridSystem, sourceCell, positionsToCheck, queuedPositions);
            }
        }

        return hasAnyMatch;
    }

    private static bool ProcessDirection(
        GridSystem gridSystem,
        Cell sourceCell,
        Vector2Int direction,
        Queue<Vector2Int> positionsToCheck,
        HashSet<Vector2Int> queuedPositions)
    {
        Vector2Int neighborPosition = sourceCell.GridPosition + direction;
        Cell neighborCell = gridSystem.GetCell(neighborPosition);
        if (neighborCell == null || !neighborCell.HasBlock()) return false;

        Block sourceBlock = sourceCell.Block;
        Block neighborBlock = neighborCell.Block;
        List<JellyBlockBase> sourceEdge = sourceBlock.GetSubBlocksTouchingEdge(direction);
        List<JellyBlockBase> neighborEdge = neighborBlock.GetSubBlocksTouchingEdge(-direction);
        if (sourceEdge.Count == 0 || neighborEdge.Count == 0) return false;

        List<JellyBlockBase> sourceMatches = new List<JellyBlockBase>();
        List<JellyBlockBase> neighborMatches = new List<JellyBlockBase>();

        foreach (JellyBlockBase sourceSubBlock in sourceEdge)
        {
            foreach (JellyBlockBase neighborSubBlock in neighborEdge)
            {
                if (sourceSubBlock == null || neighborSubBlock == null) continue;

                // --- THÊM DEBUG LOG Ở ĐÂY ---
                Debug.Log($"[MatchCheck] Hướng: {direction} | " +
                          $"Source ({sourceCell.GridPosition}): {sourceSubBlock.name} [Color: {sourceSubBlock.Color}] VS " +
                          $"Neighbor ({neighborCell.GridPosition}): {neighborSubBlock.name} [Color: {neighborSubBlock.Color}]");

                if (sourceSubBlock.Color != neighborSubBlock.Color)
                {
                    Debug.Log($"---> Không khớp màu ({sourceSubBlock.Color} != {neighborSubBlock.Color})");
                    continue;
                }

                if (!sourceBlock.IsSubBlockAlignedAcrossEdge(
                    sourceSubBlock,
                    neighborBlock,
                    neighborSubBlock,
                    direction))
                {
                    Debug.Log($"---> Cùng màu nhưng KHÔNG căn chỉnh khớp vị trí cạnh!");
                    continue;
                }

                Debug.Log($"===> KHỚP MÀU VÀ VỊ TRÍ: {sourceSubBlock.Color}!");

                if (!sourceMatches.Contains(sourceSubBlock)) sourceMatches.Add(sourceSubBlock);
                if (!neighborMatches.Contains(neighborSubBlock)) neighborMatches.Add(neighborSubBlock);
                break;
            }
        }

        if (sourceMatches.Count == 0) return false;

        foreach (JellyBlockBase matchedSubBlock in sourceMatches)
        {
            OnSubBlockMatched?.Invoke(matchedSubBlock, sourceBlock);
        }

        foreach (JellyBlockBase matchedSubBlock in neighborMatches)
        {
            OnSubBlockMatched?.Invoke(matchedSubBlock, neighborBlock);
        }

        sourceBlock.RemoveSubBlocks(sourceMatches);
        neighborBlock.RemoveSubBlocks(neighborMatches);
        OnBlockMatched?.Invoke(sourceBlock);
        OnBlockMatched?.Invoke(neighborBlock);

        EnqueueAffectedArea(gridSystem, sourceCell, positionsToCheck, queuedPositions);
        EnqueueAffectedArea(gridSystem, neighborCell, positionsToCheck, queuedPositions);
        return true;
    }

    private static void EnqueueAffectedArea(
        GridSystem gridSystem,
        Cell cell,
        Queue<Vector2Int> positionsToCheck,
        HashSet<Vector2Int> queuedPositions)
    {
        if (cell == null) return;
        EnqueuePosition(cell.GridPosition, positionsToCheck, queuedPositions);

        foreach (Cell neighbor in gridSystem.GetNeighbors(cell.GridPosition))
        {
            if (neighbor != null)
            {
                EnqueuePosition(neighbor.GridPosition, positionsToCheck, queuedPositions);
            }
        }
    }

    private static void EnqueuePosition(
        Vector2Int position,
        Queue<Vector2Int> positionsToCheck,
        HashSet<Vector2Int> queuedPositions)
    {
        if (queuedPositions.Add(position))
        {
            positionsToCheck.Enqueue(position);
        }
    }
}
