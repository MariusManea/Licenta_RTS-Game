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
        try
        {
            if (!UnderConstruction() && !Ghost)
            {
                player.IncrementResourceLimit(ResourceType.Spacing, -25);
                if (player && player.AITrainGameManager != null)
                {
                    player.AITrainGameManager.SetOwner(player.playerID, -1);
                } 
                else 
                {
                    ((GameManager)FindObjectOfType(typeof(GameManager))).SetOwner(player.playerID, -1);
                    ((LevelLoader)FindObjectOfType(typeof(LevelLoader))).ChangeBorder(player.playerID, -1);
                }
            }
        }
        catch
        {
            // Nu le gaseste la iesire din scena
        }
        if (!Ghost)
        {
            player.DecreaseObjectCount("CityHall");
        }
    }

    public override string GetObjectName()
    {
        return "Town Center";
    }
}