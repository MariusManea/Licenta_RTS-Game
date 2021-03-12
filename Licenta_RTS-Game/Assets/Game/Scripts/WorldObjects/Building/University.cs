using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;
using Newtonsoft.Json;

public class University : Building
{
    private float researchTime;
    private float currentTime;
    private bool upgradeInitiator;

    protected override void Start()
    {
        base.Start();
        researchTime = 5;
        currentTime = 0;
    }
    protected override void Update()
    {
        base.Update();
        if (!UnderConstruction() && !Ghost)
        {
            actions = player.GetResearchableObjects().ToArray();
            ProcessUpgradeQueue();
            Research();
            if (player.GetUpgradedObject() != "" && buildQueue.Count == 0)
            {
                buildQueue.Enqueue(player.GetUpgradedObject());
            }
        }
    }

    private void Research()
    {
        currentTime += Time.deltaTime;
        if (currentTime > researchTime)
        {
            currentTime = 0;
            player.AddResource(ResourceType.ResearchPoint, 1);
        }
    }

    public override void PerformAction(string actionToPerform)
    {
        base.PerformAction(actionToPerform);
        UpgradeObject(actionToPerform);
    }

    private void ProcessUpgradeQueue()
    {
        if (buildQueue.Count > 0)
        {
            if (upgradeInitiator)
            {
                player.IncreaseUniversalResearchTime(Time.deltaTime * ResourceManager.BuildSpeed);
            }
            currentBuildProgress = player.GetUniversalResearchTime();
            if (currentBuildProgress > maxBuildProgress || player.GetUniversalResearchTime() == 0.0f)
            {
                if (player)
                {
                    string upgrade = buildQueue.Peek();
                    if (upgradeInitiator == true)
                    {
                        player.UpgradeObject((UpgradeableObjects)System.Enum.Parse(typeof(UpgradeableObjects), upgrade));
                        player.SetUpgradedObject("");
                        player.ResetUniversalResearchTime();
                    }
                    buildQueue.Dequeue();
                    upgradeInitiator = false;
                    if (audioElement != null) audioElement.Play(finishedJobSound);
                }
                currentBuildProgress = 0.0f;
            }
        }
    }

    private void UpgradeObject(string objectName)
    {
        UpgradeableObjects objectToUpgrade = (UpgradeableObjects)System.Enum.Parse(typeof(UpgradeableObjects), objectName);
        if (player && buildQueue.Count == 0)
        {
            int required = ResourceManager.GetResearchPoints(objectName);
            if (player.GetResourceAmount(ResourceType.ResearchPoint) >= required)
            {
                buildQueue.Enqueue(objectName);
                player.SetUpgradedObject(objectName);
                upgradeInitiator = true;
                player.RemoveResource(ResourceType.ResearchPoint, required);
            }
        }
    }

    public override void SaveDetails(JsonWriter writer)
    {
        base.SaveDetails(writer);
        SaveManager.WriteFloat(writer, "ResearchTime", researchTime);
        SaveManager.WriteFloat(writer, "CurrentTime", currentTime);
        SaveManager.WriteBoolean(writer, "Initiator", upgradeInitiator);
    }

    protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
    {
        base.HandleLoadedProperty(reader, propertyName, readValue);
        switch (propertyName)
        {
            case "ResearchTime": researchTime = (float)(double)readValue; break;
            case "CurrentTime": currentTime = (float)(double)readValue; break;
            case "Initiator": upgradeInitiator = (bool)readValue; break;
            default: break;
        }
    }
}
