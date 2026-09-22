using DG.Tweening;
using UnityEngine;

public enum JellyColor
{
    Red,
    Blue,
    Green,
    Yellow,
    Purple,
    Orange
}

public abstract class JellyBlockBase : MonoBehaviour
{
    [Header("Jelly")]
    [SerializeField] private JellyColor color = JellyColor.Red;
    [SerializeField] private Material material;
    [SerializeField] private Vector3 blockScale = Vector3.one;

    public JellyColor Color => color;
    public Material BlockMaterial => material;
    public Vector3 BlockScale => blockScale;
    public abstract int OccupiedSubSlotCount { get; }

    protected virtual void Awake()
    {
        
        ApplyVisuals();
    }

    // Tự động chạy trong Unity Editor ngay khi bạn kéo Material mới vào Inspector
    private void OnValidate()
    {
        ApplyVisuals();
    }

    public JellyColor GetColorAt(int slotIndex)
    {
        return color;
    }

    public virtual void ApplyVisuals()
    {
        transform.localScale = blockScale;

        if (material != null)
        {
            // 1. Gán Material cho Renderer
            Renderer blockRenderer = GetComponentInChildren<Renderer>();
            if (blockRenderer != null)
            {
                blockRenderer.sharedMaterial = material;
            }

            // 2. TỰ ĐỘNG ĐẶT LẠI ENUM 'COLOR' DỰA THEO TÊN MATERIAL
            color = GetColorFromMaterial(material);
        }
    }

    /// <summary>
    /// Hàm đọc tên Material để gán tương ứng cho Enum JellyColor
    /// </summary>
    private JellyColor GetColorFromMaterial(Material mat)
    {
        string matName = mat.name.ToLower();

        if (matName.Contains("red")) return JellyColor.Red;
        if (matName.Contains("blue")) return JellyColor.Blue;
        if (matName.Contains("green")) return JellyColor.Green;
        if (matName.Contains("yellow")) return JellyColor.Yellow;
        if (matName.Contains("purple")) return JellyColor.Purple;
        if (matName.Contains("orange")) return JellyColor.Orange;

        return color; // Trả về màu cũ nếu không khớp tên nào
    }

    public void SetColor(JellyColor newColor)
    {
        color = newColor;
    }

    public void SetScale(Vector3 newScale)
    {
        blockScale = newScale;
        ApplyVisuals();
    }

   

  
}