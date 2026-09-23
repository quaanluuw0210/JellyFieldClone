using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct LevelTargetScore
{
    public JellyColor color;
    public int requiredScore;
}

[System.Serializable]
public struct PlacedBlockData
{
    [Tooltip("Vị trí đặt Block trên bàn cờ")]
    public Vector2Int gridPosition;

    [Tooltip("Prefab Block tương ứng sẽ xuất hiện tại vị trí này")]
    public Block blockPrefab;
}

[CreateAssetMenu(fileName = "Level_01", menuName = "Jelly Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Board Config")]
    public List<Vector2Int> validCellPositions = new List<Vector2Int>();

    [Header("Spawn Area Config")]
    [Tooltip("Số lượng Slot hiển thị ở khay spawn")]
    [Range(1, 4)]
    public int activeSlotCount = 3;

    [Header("Block Prefabs Sequence")]
    [Tooltip("Kéo trực tiếp các Prefab Block vào đây theo thứ tự sẽ xuất hiện")]
    public List<GameObject> blockPrefabsSequence = new List<GameObject>();

    [Header("Pre-placed Blocks")]
    [Tooltip("Danh sách các Block được sinh sẵn trên bàn cờ khi bắt đầu màn")]
    public List<PlacedBlockData> placedBlocks = new List<PlacedBlockData>();

    [Header("Level Target Scores")]
    [Tooltip("Danh sách màu và số điểm cần đạt để thắng màn này")]
    public List<LevelTargetScore> targetScores = new List<LevelTargetScore>();
}