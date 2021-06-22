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
        potentialActions = System.Enum.GetNames(typeof(UpgradeableObjects));
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
            objectName = GetObjectName() + " (" + ResourceManager.GetLevelAlias(player.GetLevel(UpgradeableObjects.University)) + ")";
        }
    }

    private void Research()
    {
        currentTime += Time.deltaTime;
        if (currentTime > researchTime + (1 - player.GetLevel(UpgradeableObjects.University)))
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

    private void UpgradeObject(string upgradeName)
    {
        if (player && buildQueue.Count == 0)
        {
            int required = ResourceManager.GetResearchPoints(upgradeName, player.GetLevel((UpgradeableObjects)System.Enum.Parse(typeof(UpgradeableObjects), upgradeName.Replace(" ", string.Empty))));
            if (player.GetResourceAmount(ResourceType.ResearchPoint) >= required)
            {
                buildQueue.Enqueue(upgradeName);
                player.SetUpgradedObject(upgradeName);
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

    public override string GetObjectName()
    {
        return "University";
    }
    public void OnDestroy()
    {
        if (!Ghost)
        {
            player.DecreaseObjectCount("University");
        }
    }
}
