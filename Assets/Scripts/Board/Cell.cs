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

    // Mảng chứa tối đa 4 khối nhỏ tại 4 góc
    private JellyBlockBase[] subSlots = new JellyBlockBase[4];

    public Cell(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
    }

    // Kiểm tra toàn bộ Cell có trống hoàn toàn không
    public bool IsEmpty()
    {
        for (int i = 0; i < 4; i++)
        {
            if (subSlots[i] != null) return false;
        }
        return true;
    }

    // Kiểm tra cả 4 SubSlot đã đầy chưa
    public bool IsFull()
    {
        for (int i = 0; i < 4; i++)
        {
            if (subSlots[i] == null) return false;
        }
        return true;
    }

    // Kiểm tra xem SubSlot cụ thể có đang trống không
    public bool IsSlotEmpty(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return false;
        return subSlots[slotIndex] == null;
    }

    // Lấy khối Jelly tại vị trí SubSlot
    public JellyBlockBase GetBlockAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return null;
        return subSlots[slotIndex];
    }

    // Đặt khối Jelly vào SubSlot
    public bool PlaceBlock(int slotIndex, JellyBlockBase block)
    {
        if (!IsSlotEmpty(slotIndex)) return false;

        subSlots[slotIndex] = block;
        return true;
    }

    // Xóa khối Jelly khỏi SubSlot
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < 4)
        {
            subSlots[slotIndex] = null;
        }
    }

    // Kiểm tra điều kiện gộp khối (Cả 4 vị trí đều đầy VÀ cùng 1 màu)
    public bool CanMerge(out JellyColor matchedColor)
    {
        matchedColor = JellyColor.Red; // Giá trị mặc định

        if (!IsFull()) return false;

        // Lấy màu của khối ở vị trí đầu tiên làm chuẩn
        JellyColor firstColor = subSlots[0].GetColorAt(0);

        for (int i = 1; i < 4; i++)
        {
            if (subSlots[i].GetColorAt(0) != firstColor)
            {
                return false;
            }
        }

        matchedColor = firstColor;
        return true;
    }

    // Xóa toàn bộ SubSlots trong Cell
    public void ClearAll()
    {
        for (int i = 0; i < 4; i++)
        {
            subSlots[i] = null;
        }
    }
}