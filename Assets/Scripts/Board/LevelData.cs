using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelData", menuName = "JellyGame/Level Data")]
public class LevelData : ScriptableObject
{
    public List<Vector2Int> validCellPositions = new List<Vector2Int>();
}