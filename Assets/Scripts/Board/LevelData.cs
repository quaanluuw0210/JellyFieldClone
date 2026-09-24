using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BoardCellData
{
    public int x;
    public int y;
    public string block_type;
    public int TL;
    public int TR;
    public int BL;
    public int BR;
}

[System.Serializable]
public class SpawnBlockData
{
    public string block_type;
    public int TL;
    public int TR;
    public int BL;
    public int BR;
}

[System.Serializable]
public class LevelConfig
{
    public int level;
    public List<BoardCellData> board_setup = new List<BoardCellData>();
    public List<SpawnBlockData> spawn_queue = new List<SpawnBlockData>();
}

[System.Serializable]
public struct LevelTargetScore
{
    public JellyColor color;
    public int requiredScore;
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

    [Header("Level Target Scores")]
    [Tooltip("Danh sách màu và số điểm cần đạt để thắng màn này")]
    public List<LevelTargetScore> targetScores = new List<LevelTargetScore>();
}