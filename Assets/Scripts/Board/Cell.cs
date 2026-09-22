using System;
using UnityEngine;

public enum SubSlotIndex
{
    TopLeft = 0,
    TopRight = 1,
    BottomLeft = 2,
    BottomRight = 3
}

[Serializable]
public class Cell
{
    public Vector2Int GridPosition { get; private set; }

    // Mỗi Cell chỉ nhận một Block container kích thước 1x1.
    private Block block;


    public Cell(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
    }

    public Block Block => block;

    public bool HasBlock()
    {
        return block != null;
    }

    public bool PlaceBlock(Block newBlock)
    {
        if (newBlock == null || block != null) return false;

        block = newBlock;
        return true;
    }

    public void ClearBlock(Block targetBlock = null)
    {
        if (targetBlock == null || block == targetBlock)
        {
            block = null;
        }
    }
}