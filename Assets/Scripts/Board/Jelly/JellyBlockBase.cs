using DG.Tweening;
using UnityEngine;

public enum JellyColor
{
    Red,
    Blue,
    Green,
    Yellow,
    Purple,
    Pink
}

public static class JellyColorExtensions
{
    public static Color ToUnityColor(this JellyColor jellyColor)
    {
        return jellyColor switch
        {
            JellyColor.Red => Color.red,            
            JellyColor.Blue => Color.blue,            
            JellyColor.Green => Color.green,           
            JellyColor.Yellow => Color.yellow,          
            JellyColor.Purple => new Color(0.6f, 0.1f, 0.8f), 
            JellyColor.Pink => new Color(1.0f, 0.41f, 0.71f), 
            _ => Color.white
        };
    }
}

public abstract class JellyBlockBase : MonoBehaviour
{
    [Header("Jelly")]
    [SerializeField] private JellyColor color = JellyColor.Red;
    [SerializeField] private Material material;
    [SerializeField] private Vector3 blockScale = Vector3.one;
    [SerializeField] private JellySpreadAnim spreadAnim = null;
    public JellyColor Color => color;
    public Material BlockMaterial => material;
    public Vector3 BlockScale => blockScale;
    
    public JellySpreadAnim SpreadAnim => spreadAnim;
    public abstract int OccupiedSubSlotCount { get; }

    protected virtual void Awake()
    {
        if (material != null)
        {
            ApplyVisuals();
        }
    }

    protected virtual void Start()
    {
        if(spreadAnim==null)
        {
            spreadAnim=GetComponent<JellySpreadAnim>();
        }    
    }    

    
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

        if (material == null) return;

     
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            if (rend != null)
            {
                rend.sharedMaterial = material;
                rend.enabled = true; 
            }
        }

        color = GetColorFromMaterial(material);
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
        if (matName.Contains("pink")) return JellyColor.Pink;

        return color; 
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


    public void SetMaterial(Material newMaterial)
    {
        material = newMaterial; 
        ApplyVisuals();
    }

    protected virtual void OnDestroy()
    {
       
        transform.DOKill();
    }

}