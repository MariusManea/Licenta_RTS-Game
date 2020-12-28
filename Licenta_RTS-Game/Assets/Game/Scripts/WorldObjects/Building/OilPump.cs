using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class OilPump : Building
{
    private int upgradeLevel;
    private int upgradedToLevel;
    private Dictionary<int, string> levelName;
    private float extractingTime;

    protected override void Start()
    {
        base.Start();
        actions = new string[] { "OilPump" };
        upgradeLevel = 1;
        upgradedToLevel = 1;
        extractingTime = 0;
        levelName = new Dictionary<int, string>
        {
            { 1, "Oil Pump I" },
            { 2, "Oil Pump II" },
            { 3, "Oil Pump III" },
            { 4, "Oil Pump IV" },
            { 5, "Oil Pump V" }
        };
        objectName = levelName[upgradeLevel];
    }

    public override void PerformAction(string actionToPerform)
    {
        UpgradePump(actionToPerform);
    }
    protected override void Update()
    {
        base.Update();
        if (!UnderConstruction() && !Ghost)
        {
            ProcessUpgradeQueue();
            ExtractOil();
        }
    }
    private void UpgradePump(string action)
    {
        if (upgradedToLevel < 5)
        {
            if (player)
            {
                ResourceManager.Cost cost = ResourceManager.GetCost(action);
                if (ResourceManager.Affordable(cost, player.AvailableResources()))
                {
                    upgradedToLevel++;
                    buildQueue.Enqueue(action);
                    player.AddResource(ResourceType.Spacing, cost.spacing);
                    player.RemoveResource(ResourceType.Copper, cost.copper);
                    player.RemoveResource(ResourceType.Iron, cost.iron);
                    player.RemoveResource(ResourceType.Oil, cost.oil);
                    player.RemoveResource(ResourceType.Gold, cost.gold);
                }
            }
        }
    }

    private void ProcessUpgradeQueue()
    {
        if (buildQueue.Count > 0)
        {
            currentBuildProgress += Time.deltaTime * ResourceManager.BuildSpeed;
            if (currentBuildProgress > maxBuildProgress)
            {
                if (player)
                {
                    upgradeLevel++;
                    objectName = levelName[upgradeLevel];
                    buildQueue.Dequeue();
                    if (audioElement != null) audioElement.Play(finishedJobSound);
                }
                currentBuildProgress = 0.0f;
            }
        }
    }

    private void ExtractOil()
    {
        extractingTime += Time.deltaTime;
        if (extractingTime > 1)
        {
            int rate = 3 * upgradeLevel;
            int times = Mathf.FloorToInt(extractingTime);
            rate *= times;
            player.AddResource(ResourceType.Oil, rate);
            extractingTime = 0;
        }
    }

    public override string GetObjectName()
    {
        return "Oil Pump";
    }
}
