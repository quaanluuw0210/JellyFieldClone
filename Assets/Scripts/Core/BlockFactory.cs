using System;
using System.Collections.Generic;
using UnityEngine;

public enum BlockType
{
    FourBlock,
    FullBlock,
    DoubleHor,
    DoubleVer,
    OneDTwoS_Hor_Top,
    OneDTwoS_Hor_Bottom,
    OneDTwoS_Ver_Left,
    OneDTwoS_Ver_Right
}

/// <summary>
/// Tạo Block từ dữ liệu JSON. Factory chỉ instantiate và cấu hình màu;
/// SpawnView/BoardView quyết định parent, vị trí và đăng ký vào board.
/// </summary>
public class BlockFactory : MonoBehaviour
{
    [Header("Block Prefabs")]
    [SerializeField] private Block fullBlockPrefab;
    [SerializeField] private Block doubleHorizontalPrefab;
    [SerializeField] private Block doubleVerticalPrefab;
    [SerializeField] private Block fourBlockPrefab;
    [SerializeField] private Block oneDTwoSHorizontalTopPrefab;
    [SerializeField] private Block oneDTwoSHorizontalBottomPrefab;
    [SerializeField] private Block oneDTwoSVerticalLeftPrefab;
    [SerializeField] private Block oneDTwoSVerticalRightPrefab;

    public Block Create(SpawnBlockData data)
    {
        if (data == null) return null;
        return CreateInternal(data.block_type, data.TL, data.TR, data.BL, data.BR);
    }

    public Block Create(BoardCellData data)
    {
        if (data == null) return null;
        return CreateInternal(data.block_type, data.TL, data.TR, data.BL, data.BR);
    }

    private Block CreateInternal(string blockType, int topLeft, int topRight, int bottomLeft, int bottomRight)
    {
        Block prefab = GetPrefab(blockType);
        if (prefab == null)
        {
            Debug.LogError(string.Format("[BlockFactory] Chưa gán prefab cho block_type '{0}'.", blockType));
            return null;
        }

        Block block = Instantiate(prefab);
        ApplyColors(block, topLeft, topRight, bottomLeft, bottomRight);
        return block;
    }

    private Block GetPrefab(string blockType)
    {
        if (string.IsNullOrEmpty(blockType)) return null;

        switch (blockType.Trim())
        {
            case "FullBlock": return fullBlockPrefab;
            case "DoubleHor": return doubleHorizontalPrefab;
            case "DoubleVer": return doubleVerticalPrefab;
            case "FourBlock": return fourBlockPrefab;
            case "OneDTwoS_Hor_Top": return oneDTwoSHorizontalTopPrefab;
            case "OneDTwoS_Hor_Bottom": return oneDTwoSHorizontalBottomPrefab;
            case "OneDTwoS_Ver_Left": return oneDTwoSVerticalLeftPrefab;
            case "OneDTwoS_Ver_Right": return oneDTwoSVerticalRightPrefab;
            default:
                Debug.LogWarning(string.Format("[BlockFactory] Block type chưa hỗ trợ: {0}.", blockType));
                return null;
        }
    }

    private void ApplyColors(Block block, int topLeft, int topRight, int bottomLeft, int bottomRight)
    {
        if (block == null) return;

        int[] colors = { topLeft, topRight, bottomLeft, bottomRight };
        List<JellyBlockBase> children = new List<JellyBlockBase>();
        foreach (JellyBlockBase child in block.GetComponentsInChildren<JellyBlockBase>(true))
        {
            if (child != null && child.transform != block.transform) children.Add(child);
        }

        children.Sort(CompareLocalPosition);
        for (int index = 0; index < children.Count; index++)
        {
            JellyBlockBase child = children[index];
            int slotIndex = GetNearestSlotIndex(block.transform.InverseTransformPoint(child.transform.position));
            slotIndex = Mathf.Clamp(slotIndex, 0, colors.Length - 1);

            if (!TryMapColor(colors[slotIndex], out JellyColor color))
            {
                Debug.LogWarning(string.Format("[BlockFactory] Mã màu không hợp lệ: {0}.", colors[slotIndex]));
                continue;
            }

            child.SetColor(color);
            Material material = JellyFactory.Instance == null
                ? null
                : JellyFactory.Instance.GetMaterialForColor(color);
            if (material != null) child.SetMaterial(material);
        }
    }

    private static int CompareLocalPosition(JellyBlockBase left, JellyBlockBase right)
    {
        if (left == null || right == null) return 0;
        Vector3 leftPosition = left.transform.localPosition;
        Vector3 rightPosition = right.transform.localPosition;
        int zOrder = rightPosition.z.CompareTo(leftPosition.z);
        return zOrder != 0 ? zOrder : leftPosition.x.CompareTo(rightPosition.x);
    }

    private static int GetNearestSlotIndex(Vector3 localPosition)
    {
        bool right = localPosition.x >= 0f;
        bool bottom = localPosition.z <= 0f;
        if (!right && !bottom) return 0;
        if (right && !bottom) return 1;
        if (!right) return 2;
        return 3;
    }

    private static bool TryMapColor(int value, out JellyColor color)
    {
        if (value < 0 || value > 5)
        {
            color = default;
            return false;
        }

        // Mapping cố định khớp enum JellyColor: Red=0 ... Pink=5.
        color = (JellyColor)value;
        return true;
    }
}