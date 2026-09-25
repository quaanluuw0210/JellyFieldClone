using System;
using System.Collections;
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

    public IEnumerator RecoverShapeRoutine(List<JellyBlockBase> livingSubBlocks, Action<List<JellyBlockBase>> onAllComplete)
    {
        ClearGrid();
        RegisterAll(livingSubBlocks);

        List<JellyBlockBase> result = new List<JellyBlockBase>(CopyValidBlocks(livingSubBlocks));

        int maxIterations = 10;
        int step = 0;

        while (step < maxIterations)
        {
            int emptySlots = FindEmptySlot(livingSubBlocks);
            if (emptySlots == 0) break;

            bool isStepFinished = false;
            bool expanded = false;

            if (TryExpandSingleToDouble(result, () => isStepFinished = true, out bool isAnimTriggered))
            {

                expanded = true;
                if (isAnimTriggered)
                {
                    yield return new WaitUntil(() => isStepFinished);
                    yield return new WaitForSeconds(0.05f);
                }
            }
            else if (emptySlots == 2 && TryExpandDoubleToFull(result, () => isStepFinished = true, out isAnimTriggered))
            {
                expanded = true;
                if (isAnimTriggered)
                {
                    yield return new WaitUntil(() => isStepFinished);
                    yield return new WaitForSeconds(0.05f);
                }
            }



            if (!expanded) break;
            step++;
        }
       
        onAllComplete?.Invoke(result);
    }





    // --- EXPAND: SINGLE -> DOUBLE (ĐÃ SỬA: ƯU TIÊN THEO THỨ TỰ CỦA LIST BLOCKS) ---
    private bool TryExpandSingleToDouble(List<JellyBlockBase> blocks, Action onStepComplete, out bool animTriggered)
    {
        animTriggered = false;

        // BỚT VÒNG LẶP FOR CỐ ĐỊNH -> DUYỆT TRỰC TIẾP THEO THỨ TỰ TRONG LIST BLOCKS ĐÃ DỰNG/SORT!
        foreach (JellyBlockBase jelly in blocks)
        {
            if (jelly is JellySingleBlock single && single != null)
            {
                // Lấy vị trí tọa độ Slot (x, y) của single block này trong gridSlots
                Vector2Int slot = GetNearestSlot(single.transform.localPosition);
                int x = slot.x;
                int y = slot.y;

                // Kiểm tra xem ô này có thực sự chứa single block này không
                if (gridSlots[x, y] != single) continue;

                foreach (Vector2Int dir in GetPriorityDirections())
                {
                    Vector2Int target = new Vector2Int(x, y) + dir;
                    if (IsValidSlot(target.x, target.y) && gridSlots[target.x, target.y] == null)
                    {
                        bool isHorizontal = dir.x != 0;

                        // 1. TÍNH VỊ TRÍ MIDPOINT CHUẨN ĐỂ ĐẶT KHỐI DOUBLE
                        Vector3 midLocalPos = GetMidSlotLocalPosition(x, y, target.x, target.y);
                        midLocalPos.y += 0.25f; // Offset Y

                        JellyColor color = single.Color;
                        JellyType type = isHorizontal ? JellyType.DoubleHorizontal : JellyType.DoubleVertical;

                        Transform parentTransform = single.transform.parent;
                        Vector3 midWorldPos = parentTransform != null
                            ? parentTransform.TransformPoint(midLocalPos)
                            : transform.TransformPoint(midLocalPos);

                        JellyBlockBase newDouble = JellyFactory.Instance.CreateJelly(
                            type,
                            midWorldPos,
                            single.transform.rotation,
                            parentTransform != null ? parentTransform : transform);

                        if (newDouble == null) return false;

                        newDouble.SetMaterial(JellyFactory.Instance.GetMaterialForColor(color));
                        newDouble.transform.localPosition = midLocalPos;

                        blocks.Remove(single);
                        blocks.Add(newDouble);
                        gridSlots[x, y] = newDouble;
                        gridSlots[target.x, target.y] = newDouble;

                        if (newDouble.SpreadAnim != null)
                        {
                            animTriggered = true;
                            newDouble.SpreadAnim.MorphSingleToDouble(single, newDouble, new Vector2Int(x, y), target, onStepComplete);
                        }
                        else
                        {
                            Destroy(single.gameObject);
                        }

                        return true; // Thực hiện thành công cho block ưu tiên đầu tiên!
                    }
                }
            }
        }

        return false;
    }

    // --- EXPAND: DOUBLE -> FULL ---
    private bool TryExpandDoubleToFull(List<JellyBlockBase> blocks, Action onStepComplete, out bool animTriggered)
    {
        HashSet<JellyBlockBase> checkedBlocks = new HashSet<JellyBlockBase>();
        animTriggered = false;
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if (gridSlots[x, y] is JellyDoubleBlock doubleBlock && checkedBlocks.Add(doubleBlock))
                {
                    List<int> occupiedSlots = GetSlotsForBlock(doubleBlock);
                    if (occupiedSlots.Count != 2) continue;

                    JellyColor color = doubleBlock.Color;
                    Material mat = JellyFactory.Instance.GetMaterialForColor(color);
                    Vector3 centerLocalPos = Vector3.zero;
                    centerLocalPos.y += 0.25f;
                    JellyBlockBase newFull = JellyFactory.Instance.CreateJelly(
                        JellyType.Full,
                        transform.TransformPoint(centerLocalPos),
                        transform.rotation,
                        transform);

                    if (newFull == null) return false;

                    newFull.transform.localPosition = centerLocalPos;
                    newFull.SetMaterial(mat);

                    blocks.Remove(doubleBlock);
                    

                    blocks.Add(newFull);

                    for (int sx = 0; sx < 2; sx++)
                        for (int sy = 0; sy < 2; sy++)
                            gridSlots[sx, sy] = newFull;


                    if (newFull.SpreadAnim != null)
                    {
                        newFull.SpreadAnim.MorphDoubleToFull(doubleBlock,newFull, onStepComplete);
                    }
                    else
                    {
                        Destroy(doubleBlock.gameObject);
                    }

                    return true;
                }
            }
        }
        return false;
    }

    // --- HÀM PHỤ TRỢ CHO SPREAD ---
    private IEnumerable<Vector2Int> GetPriorityDirections()
    {
        if (preferredDirection == SpreadDirection.Horizontal)
        {
            yield return new Vector2Int(1, 0);
            yield return new Vector2Int(-1, 0);
            yield return new Vector2Int(0, 1);
            yield return new Vector2Int(0, -1);
        }
        else
        {
            yield return new Vector2Int(0, 1);
            yield return new Vector2Int(0, -1);
            yield return new Vector2Int(1, 0);
            yield return new Vector2Int(-1, 0);
        }
    }

    private static bool IsValidSlot(int x, int y)
    {
        return x >= 0 && x < 2 && y >= 0 && y < 2;
    }

    private Vector3 GetSlotLocalPosition(int x, int y)
    {
        // Khớp với convention trong GetNearestSlot: x=0→-0.25, x=1→0.25 ; y=0→+0.25(z), y=1→-0.25(z)
        float px = x == 0 ? -0.25f : 0.25f;
        float pz = y == 0 ? 0.25f : -0.25f;
        return new Vector3(px, 0f, pz);
    }

    private Vector3 GetMidSlotLocalPosition(int x1, int y1, int x2, int y2)
    {
        Vector3 pos1 = GetSlotLocalPosition(x1, y1);
        Vector3 pos2 = GetSlotLocalPosition(x2, y2);
        return (pos1 + pos2) * 0.5f;
    }

    // --- LOG / ĐẾM (giữ nguyên từ code cũ) ---

    public int FindEmptySlot(IReadOnlyList<JellyBlockBase> livingSubBlocks)
    {
        int emptyCount = 0;
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if (gridSlots[x, y] == null)
                {
                    emptyCount++;
                }
            }
        }
        return emptyCount;
    }

   

    // --- REGISTER / GRID STATE (giữ nguyên từ code cũ) ---

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
                    int y = pos.z >= 0f ? 0 : 1;
                    gridSlots[0, y] = jelly;
                    gridSlots[1, y] = jelly;
                }
                else
                {
                    int x = pos.x >= 0f ? 1 : 0;
                    gridSlots[x, 0] = jelly;
                    gridSlots[x, 1] = jelly;
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
        int x = localPos.x >= 0f ? 1 : 0;
        int y = localPos.z >= 0f ? 0 : 1;
        return new Vector2Int(x, y);
    }

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
}