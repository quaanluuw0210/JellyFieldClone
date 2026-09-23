using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct ColorSpriteMapping
{
    public JellyColor color;
    public Sprite iconSprite;
}

public class TargetUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform containerTransform; // Nơi chứa các TargetUIItem
    [SerializeField] private TargetUIItem targetItemPrefab;

    [Header("Config Icons")]
    [SerializeField] private List<ColorSpriteMapping> colorSprites;

    private readonly Dictionary<JellyColor, TargetUIItem> activeUIItems = new Dictionary<JellyColor, TargetUIItem>();

   
    public void InitializeUI(LevelData levelData)
    {
        ClearUI();

        if (levelData == null || levelData.targetScores == null) return;

        foreach (var target in levelData.targetScores)
        {
            Sprite icon = GetSpriteForColor(target.color);
            TargetUIItem itemInstance = Instantiate(targetItemPrefab, containerTransform);
            itemInstance.Setup(target.color, icon, target.requiredScore);

            activeUIItems[target.color] = itemInstance;
        }

        Canvas.ForceUpdateCanvases(); 

        LayoutRebuilder.ForceRebuildLayoutImmediate(containerTransform as RectTransform);
    }

    public void UpdateTargetScore(JellyColor color, int remainingScore)
    {
        if (activeUIItems.TryGetValue(color, out TargetUIItem uiItem))
        {
            uiItem.UpdateScore(remainingScore);
        }
    }

    private void ClearUI()
    {
        foreach (Transform child in containerTransform)
        {
            Destroy(child.gameObject);
        }
        activeUIItems.Clear();
    }

    private Sprite GetSpriteForColor(JellyColor color)
    {
        foreach (var mapping in colorSprites)
        {
            if (mapping.color == color) return mapping.iconSprite;
        }
        return null;
    }
}