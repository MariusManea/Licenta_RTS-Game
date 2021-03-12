using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using RTS;

public class OilPump : Building
{
    private int upgradeLevel;
    private int upgradedToLevel;
    private Dictionary<int, string> levelName;
    private float extractingTime;
    private GameObject oilPileSupport;
    private int loadedOilPileId;
    protected override void Start()
    {
        base.Start();
        actions = new string[] { "OilPump" };
        
        extractingTime = 0;
        levelName = new Dictionary<int, string>
        {
            { 1, "Oil Pump I" },
            { 2, "Oil Pump II" },
            { 3, "Oil Pump III" },
            { 4, "Oil Pump IV" },
            { 5, "Oil Pump V" }
        };

        if (player && loadedSavedValues && loadedOilPileId >= 0)
        {
            WorldObjects obj = player.GetObjectForId(loadedOilPileId);
            if (obj.GetType().IsSubclassOf(typeof(Resource)))
            {
                oilPileSupport = obj.gameObject;
                foreach (Renderer renderer in oilPileSupport.GetComponentsInChildren<MeshRenderer>())
                {
                    renderer.enabled = false;
                }
                foreach (Collider collider in oilPileSupport.GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }
            }
        }
        if (!loadedSavedValues)
        {
            upgradeLevel = 1;
            upgradedToLevel = 1;
        }
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

    public void SetPile(GameObject oilPile)
    {
        oilPileSupport = oilPile;
    }

    public void OnDestroy()
    {
        if (oilPileSupport)
        {
            foreach (Renderer renderer in oilPileSupport.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.enabled = true;
            }
            foreach (Collider collider in oilPileSupport.GetComponentsInChildren<Collider>())
            {
                collider.enabled = true;
            }
        }
    }

    public override void SaveDetails(JsonWriter writer)
    {
        base.SaveDetails(writer);
        SaveManager.WriteInt(writer, "LevelUpgraded", upgradedToLevel);
        SaveManager.WriteInt(writer, "Level", upgradeLevel);
        SaveManager.WriteInt(writer, "OilPileId", oilPileSupport.GetComponent<WorldObjects>().ObjectId);
    }
    protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
    {
        base.HandleLoadedProperty(reader, propertyName, readValue);
        switch (propertyName)
        {
            case "LevelUpgraded": upgradedToLevel = (int)(System.Int64)readValue; break;
            case "Level": upgradeLevel = (int)(System.Int64)readValue; break;
            case "OilPileId": loadedOilPileId = (int)(System.Int64)readValue; break;
            default: break;
        }
    }
}
