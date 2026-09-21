using UnityEngine;

public class JellyFullBlock : JellyBlockBase
{
    public override int OccupiedSubSlotCount => 4;

    protected override void Awake()
    {
        base.Awake();
        SetScale(Vector3.one);
    }
}