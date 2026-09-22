using UnityEngine;
public enum JellyDoubleOrientation
{
    Horizontal,
    Vertical    
}
public class JellyDoubleBlock : JellyBlockBase
{
    [SerializeField] private JellyDoubleOrientation orientation = JellyDoubleOrientation.Horizontal;

    public JellyDoubleOrientation Orientation => orientation;
    public override int OccupiedSubSlotCount => 2;

    protected override void Awake()
    {
       
        base.Awake();

        
    }

   
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            Vector3 targetScale = orientation == JellyDoubleOrientation.Horizontal
                ? new Vector3(1f, 0.5f, 0.5f)
                : new Vector3(0.5f, 0.5f, 1f);

            SetScale(targetScale);
        }
    }
}