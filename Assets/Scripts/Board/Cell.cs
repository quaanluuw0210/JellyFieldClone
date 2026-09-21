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

    // Mảng 4 sub-slot đại diện cho 4 góc của ô 1x1
    private JellyBlockBase[] subSlots = new JellyBlockBase[4];

    public Cell(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
    }

    public bool IsEmpty()
    {
        for (int i = 0; i < 4; i++)
        {
            if (subSlots[i] != null) return false;
        }
        return true;
    }

    public bool IsFull()
    {
        for (int i = 0; i < 4; i++)
        {
            if (subSlots[i] == null) return false;
        }
        return true;
    }

    public bool IsSlotEmpty(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return false;
        return subSlots[slotIndex] == null;
    }

    public JellyBlockBase GetBlockAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return null;
        return subSlots[slotIndex];
    }

    public bool PlaceBlock(int slotIndex, JellyBlockBase block)
    {
        if (!IsSlotEmpty(slotIndex)) return false;

        subSlots[slotIndex] = block;
        return true;
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < 4)
        {
            subSlots[slotIndex] = null;
        }
    }

    public void ClearAll()
    {
        for (int i = 0; i < 4; i++)
        {
            subSlots[i] = null;
        }
    }
}