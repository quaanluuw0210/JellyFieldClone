using UnityEngine;

public class JellySingleBlock : JellyBlockBase
{
    public override int OccupiedSubSlotCount => 1;

    protected override void Awake()
    {
        base.Awake();
        SetScale(new Vector3(0.5f, 0.5f, 0.5f));
    }
}