using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class TownCenter : Building
{

    protected override void Start()
    {
        base.Start();
        actions = new string[] { "Worker", "RustyHarvester" };
        potentialActions = new string[] { "Worker", "RustyHarvester" };
    }
    protected override void Update()
    {
        base.Update();
        objectName = GetObjectName() + " (" + ResourceManager.GetLevelAlias(player.GetLevel(UpgradeableObjects.CityHall)) + ")";
    }
    public override void PerformAction(string actionToPerform)
    {
        base.PerformAction(actionToPerform);
        CreateUnit(actionToPerform);
    }

    public override bool Sellable()
    {
        return false;
    }

    public void FirstUnits(int numberOfUnits)
    {
        if (player)
        {
            for (int i = 0; i < numberOfUnits; ++i) {
                player.AddUnit("Worker", spawnPoint, rallyPoint, transform.rotation, this);
            }
            player.AddResource(RTS.ResourceType.Spacing, numberOfUnits);
        }
    }
    protected override bool ShouldMakeDecision()
    {
        return false;
    }

    private void OnDestroy()
    {
        if (player) player.GameLost();
    }

    public override string GetObjectName()
    {
        return "Town Center";
    }
}