using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Refinery : Building
{

    protected override void Start()
    {
        base.Start();
        actions = new string[] { "Harvester" };
    }

    public override void PerformAction(string actionToPerform)
    {
        base.PerformAction(actionToPerform);
        CreateUnit(actionToPerform);
    }
}
