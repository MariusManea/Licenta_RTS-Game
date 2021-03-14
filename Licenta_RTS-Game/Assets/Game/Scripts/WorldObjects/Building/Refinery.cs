using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class Refinery : Building
{

    protected override void Start()
    {
        base.Start();
        actions = new string[] { "Harvester" };
    }

    protected override void Update()
    {
        base.Update();
        if (!needsBuilding && !Ghost)
        {
            objectName = GetObjectName() + " (" + ResourceManager.GetLevelAlias(player.GetLevel(UpgradeableObjects.Refinery)) + ")";
        }
    }

    public override void PerformAction(string actionToPerform)
    {
        base.PerformAction(actionToPerform);
        CreateUnit(actionToPerform);
    }

    protected override bool ShouldMakeDecision()
    {
        return false;
    }

    public override string GetObjectName()
    {
        return "Refinery";
    }
}
