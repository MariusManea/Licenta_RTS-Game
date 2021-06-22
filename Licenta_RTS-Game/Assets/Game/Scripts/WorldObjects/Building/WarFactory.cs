using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class WarFactory : Building
{
    protected override void Start()
    {
        base.Start();
        actions = new string[] { "Tank" };
        potentialActions = new string[] { "Tank", "BatteringRam", "ConvoyTruck" };
    }

    protected override void Update()
    {
        base.Update();
        if (!needsBuilding && !Ghost)
        {
            objectName = GetObjectName() + " (" + ResourceManager.GetLevelAlias(player.GetLevel(UpgradeableObjects.WarFactory)) + ")";
            if (currentlySelected)
            {
                actions = player.GetResearchedObjects(potentialActions);
            }
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
        return "War Factory";
    }
    public void OnDestroy()
    {
        if (!Ghost)
        {
            player.DecreaseObjectCount("WarFactory");
        }
    }
}
