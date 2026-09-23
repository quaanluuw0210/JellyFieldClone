using System;
using System.Collections.Generic;
using UnityEngine;

public static class MatchLogic
{
    public static event Action<JellyBlockBase, Block> OnSubBlockMatched;
    public static event Action<Block> OnBlockMatched;

    /// <summary>
    /// Xử lý match cho MỘT đợt (wave) bắt đầu từ 1 tập vị trí seed.
    /// Không tự lặp chain bên trong nữa — chỉ quét các vị trí trong 'seedPositions'
    /// (và các vị trí bị ảnh hưởng trực tiếp phát sinh trong CHÍNH wave này),
    /// trả về danh sách Block bị ảnh hưởng để caller chờ animation xong rồi tự gọi lại cho wave kế tiếp.
    /// </summary>
    public static bool ProcessMatchWave(
        GridSystem gridSystem,
        IEnumerable<Vector2Int> seedPositions,
        out HashSet<Block> affectedBlocks,
        out HashSet<Vector2Int> nextWaveSeeds)
    {
        affectedBlocks = new HashSet<Block>();
        nextWaveSeeds = new HashSet<Vector2Int>();
        bool hasAnyMatch = false;

        if (gridSystem == null) return false;

        Queue<Vector2Int> positionsToCheck = new Queue<Vector2Int>();
        HashSet<Vector2Int> queuedPositions = new HashSet<Vector2Int>();
        foreach (var pos in seedPositions)
        {
            EnqueuePosition(pos, positionsToCheck, queuedPositions);
        }

        while (positionsToCheck.Count > 0)
        {
            Vector2Int position = positionsToCheck.Dequeue();
            queuedPositions.Remove(position);
            Cell sourceCell = gridSystem.GetCell(position);
            if (sourceCell == null || !sourceCell.HasBlock()) continue;

            bool horizontalMatch = ProcessDirection(
                gridSystem, sourceCell, Vector2Int.left, affectedBlocks, nextWaveSeeds);
            horizontalMatch |= ProcessDirection(
                gridSystem, sourceCell, Vector2Int.right, affectedBlocks, nextWaveSeeds);

            if (horizontalMatch)
            {
                hasAnyMatch = true;
                continue; // đã match ngang, KHÔNG check dọc thêm ở vị trí này trong cùng wave (giữ đúng rule cũ)
            }

            bool verticalMatch = ProcessDirection(
                gridSystem, sourceCell, Vector2Int.down, affectedBlocks, nextWaveSeeds);
            verticalMatch |= ProcessDirection(
                gridSystem, sourceCell, Vector2Int.up, affectedBlocks, nextWaveSeeds);

            if (verticalMatch)
            {
                hasAnyMatch = true;
            }
        }

        return hasAnyMatch;
    }

    private static bool ProcessDirection(
        GridSystem gridSystem,
        Cell sourceCell,
        Vector2Int direction,
        HashSet<Block> affectedBlocks,
        HashSet<Vector2Int> nextWaveSeeds)
    {
        Vector2Int neighborPosition = sourceCell.GridPosition + direction;
        Cell neighborCell = gridSystem.GetCell(neighborPosition);
        if (neighborCell == null || !neighborCell.HasBlock() || sourceCell == null) return false;

        Block sourceBlock = sourceCell.Block;
        Block neighborBlock = neighborCell.Block;
        List<JellyBlockBase> sourceEdge = sourceBlock.GetSubBlocksTouchingEdge(direction);
        List<JellyBlockBase> neighborEdge = neighborBlock.GetSubBlocksTouchingEdge(-direction);

        if (sourceEdge == null || neighborEdge == null || sourceEdge.Count == 0 || neighborEdge.Count == 0) return false;

        List<JellyBlockBase> sourceMatches = new List<JellyBlockBase>();
        List<JellyBlockBase> neighborMatches = new List<JellyBlockBase>();

        foreach (JellyBlockBase sourceSubBlock in sourceEdge)
        {
            foreach (JellyBlockBase neighborSubBlock in neighborEdge)
            {
                if (sourceSubBlock == null || neighborSubBlock == null) continue;
                if (sourceSubBlock.Color != neighborSubBlock.Color) continue;

                if (!sourceBlock.IsSubBlockAlignedAcrossEdge(
                    sourceSubBlock, neighborBlock, neighborSubBlock, direction))
                {
                    continue;
                }

                if (!sourceMatches.Contains(sourceSubBlock)) sourceMatches.Add(sourceSubBlock);
                if (!neighborMatches.Contains(neighborSubBlock)) neighborMatches.Add(neighborSubBlock);
                break;
            }
        }

        if (sourceMatches.Count == 0) return false;

        foreach (JellyBlockBase matchedSubBlock in sourceMatches)
            OnSubBlockMatched?.Invoke(matchedSubBlock, sourceBlock);

        foreach (JellyBlockBase matchedSubBlock in neighborMatches)
            OnSubBlockMatched?.Invoke(matchedSubBlock, neighborBlock);

        sourceBlock.RemoveSubBlocks(sourceMatches);
        neighborBlock.RemoveSubBlocks(neighborMatches);
        OnBlockMatched?.Invoke(sourceBlock);
        OnBlockMatched?.Invoke(neighborBlock);

        // Ghi nhận block bị ảnh hưởng để caller chờ animation
        affectedBlocks.Add(sourceBlock);
        affectedBlocks.Add(neighborBlock);

        // Ghi nhận vùng cần quét lại ở wave KẾ TIẾP (sau khi animation xong)
        AddAffectedAreaSeeds(gridSystem, sourceCell, nextWaveSeeds);
        AddAffectedAreaSeeds(gridSystem, neighborCell, nextWaveSeeds);

        return true;
    }

    private static void AddAffectedAreaSeeds(
        GridSystem gridSystem, Cell cell, HashSet<Vector2Int> nextWaveSeeds)
    {
        if (cell == null) return;
        nextWaveSeeds.Add(cell.GridPosition);

        foreach (Cell neighbor in gridSystem.GetNeighbors(cell.GridPosition))
        {
            if (neighbor != null)
            {
                nextWaveSeeds.Add(neighbor.GridPosition);
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