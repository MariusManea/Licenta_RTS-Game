using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class OilDeposit : Resource
{
    protected override void Start()
    {
        base.Start();
        resourceType = ResourceType.OilDeposit;
    }

    public override bool Harvestable()
    {
        return false;
    }
}
