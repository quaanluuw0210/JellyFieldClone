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
    [SerializeField] private float jiggleDuration = 0.18f;
    [SerializeField] private float jiggleStrength = 0.08f;

    private Vector3 initialScale;
    private Tween jiggleTween;

    public JellyColor Color => color;
    public Material BlockMaterial => material;
    public Vector3 BlockScale => blockScale;
    public abstract int OccupiedSubSlotCount { get; }

    protected virtual void Awake()
    {
        initialScale = transform.localScale;
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

        Renderer blockRenderer = GetComponentInChildren<Renderer>();
        if (blockRenderer != null)
        {
            blockRenderer.sharedMaterial = material;
        }
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

    public virtual void PlayJiggle()
    {
        jiggleTween?.Kill();
        Vector3 targetScale = blockScale == Vector3.zero ? initialScale : blockScale;
        transform.localScale = targetScale;
        jiggleTween = transform
            .DOShakeScale(jiggleDuration, jiggleStrength, 8, 90f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => transform.localScale = targetScale);
    }

    protected virtual void OnDestroy()
    {
        jiggleTween?.Kill();
    }
}