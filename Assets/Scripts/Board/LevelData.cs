using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelData", menuName = "JellyGame/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Board Config")]
    public List<Vector2Int> validCellPositions = new List<Vector2Int>();

    [Header("Spawn Area Config")]
    [Tooltip("Số lượng Slot hiển thị ở khay spawn")]
    [Range(1, 4)] public int activeSlotCount = 3;

    [Header("Block Prefabs Sequence")]
    [Tooltip("Kéo trực tiếp các Prefab Block vào đây theo thứ tự sẽ xuất hiện")]
    public List<GameObject> blockPrefabsSequence = new List<GameObject>();
}