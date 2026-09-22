using UnityEngine;

public class JellyFullBlock : JellyBlockBase
{
    public override int OccupiedSubSlotCount => 4;

    protected override void Awake()
    {
        base.Awake();
        SetScale(new Vector3(1,0.5f,1));
    }
}