using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownCenter : Building
{

    protected override void Start()
    {
        base.Start();
        actions = new string[] { "Worker" };
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
        }
    }
}