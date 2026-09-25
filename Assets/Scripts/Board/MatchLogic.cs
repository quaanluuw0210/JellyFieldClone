using System;
using System.Collections.Generic;
using UnityEngine;

public static class MatchLogic
{
    public static event Action<JellyBlockBase, Block> OnSubBlockMatched;
    public static event Action<Block> OnBlockMatched;

    private struct PendingMatchData
    {
        public Cell sourceCell;
        public Cell neighborCell;
        public Block sourceBlock;
        public Block neighborBlock;
        public List<JellyBlockBase> sourceMatches;
        public List<JellyBlockBase> neighborMatches;
    }

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

        List<PendingMatchData> pendingMatches = new List<PendingMatchData>();
        Vector2Int[] directions = new Vector2Int[] { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };

        while (positionsToCheck.Count > 0)
        {
            Vector2Int position = positionsToCheck.Dequeue();
            queuedPositions.Remove(position);
            Cell sourceCell = gridSystem.GetCell(position);
            if (sourceCell == null || !sourceCell.HasBlock()) continue;

            bool horizontalMatch = ProcessDirection(
                gridSystem, sourceCell, Vector2Int.left, affectedBlocks, nextWaveSeeds,pendingMatches);
            horizontalMatch |= ProcessDirection(
                gridSystem, sourceCell, Vector2Int.right, affectedBlocks, nextWaveSeeds,pendingMatches);

            bool verticalMatch = ProcessDirection(
                gridSystem, sourceCell, Vector2Int.down, affectedBlocks, nextWaveSeeds, pendingMatches);
            verticalMatch |= ProcessDirection(
                gridSystem, sourceCell, Vector2Int.up, affectedBlocks, nextWaveSeeds, pendingMatches);

            if (verticalMatch||horizontalMatch)
            {
                hasAnyMatch = true;
               
            }
        }
        if(hasAnyMatch)
        {
            ExecuteMatch(gridSystem, pendingMatches, affectedBlocks, nextWaveSeeds);
        }    

        return hasAnyMatch;
    }

    private static bool ProcessDirection(
        GridSystem gridSystem,
        Cell sourceCell,
        Vector2Int direction,
        HashSet<Block> affectedBlocks,
        HashSet<Vector2Int> nextWaveSeeds, List<PendingMatchData> pendingMatches)
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


        pendingMatches.Add(new PendingMatchData
            {
                sourceCell = sourceCell,
                neighborCell= neighborCell,
                sourceBlock= sourceBlock,
                neighborBlock= neighborBlock,
                sourceMatches = sourceMatches,
                neighborMatches = neighborMatches
            }
        );
       
        return true;
    }

    private static void ExecuteMatch(
     GridSystem gridSystem,
     List<PendingMatchData> listPendingMatch,
     HashSet<Block> affectedBlocks,
     HashSet<Vector2Int> nextWaveSeeds)
    {
        // 1. Dùng Dictionary để GOM TẤT CẢ sub-block cần xóa của từng Block
        Dictionary<Block, HashSet<JellyBlockBase>> blocksToMatchMap = new Dictionary<Block, HashSet<JellyBlockBase>>();

        foreach (PendingMatchData pendingMatch in listPendingMatch)
        {
            Block sourceBlock = pendingMatch.sourceBlock;
            Block neighborBlock = pendingMatch.neighborBlock;

            // Gom cho sourceBlock
            if (!blocksToMatchMap.ContainsKey(sourceBlock))
                blocksToMatchMap[sourceBlock] = new HashSet<JellyBlockBase>();
            foreach (var sub in pendingMatch.sourceMatches)
                blocksToMatchMap[sourceBlock].Add(sub);

            // Gom cho neighborBlock
            if (!blocksToMatchMap.ContainsKey(neighborBlock))
                blocksToMatchMap[neighborBlock] = new HashSet<JellyBlockBase>();
            foreach (var sub in pendingMatch.neighborMatches)
                blocksToMatchMap[neighborBlock].Add(sub);

            // Ghi nhận vùng bị ảnh hưởng
            AddAffectedAreaSeeds(gridSystem, pendingMatch.sourceCell, nextWaveSeeds);
            AddAffectedAreaSeeds(gridSystem, pendingMatch.neighborCell, nextWaveSeeds);
        }

        // 2. THỰC THI THẬT: Mỗi Block chỉ gọi RemoveSubBlocks ĐÚNG 1 LẦN cho toàn bộ sub-block bị trùng!
        foreach (var kvp in blocksToMatchMap)
        {
            Block block = kvp.Key;
            List<JellyBlockBase> uniqueMatches = new List<JellyBlockBase>(kvp.Value);

            foreach (JellyBlockBase matchedSubBlock in uniqueMatches)
            {
                OnSubBlockMatched?.Invoke(matchedSubBlock, block);
            }

            // Gọi xóa duy nhất 1 lần cho cả khối!
            block.RemoveSubBlocks(uniqueMatches);
            OnBlockMatched?.Invoke(block);

            affectedBlocks.Add(block);
        }
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