using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class CityHall : Building
{
    public int territory = -1;
    public int ownerID = -1;
    private bool once;
    protected override void Start()
    {
        base.Start();
        actions = new string[] { "Worker", "RustyHarvester" };
        potentialActions = new string[] { "Worker", "RustyHarvester" };
        once = true;
    }

    protected override void Update()
    {
        base.Update();
        if (needsBuilding)
        {
            if (((GameManager)FindObjectOfType(typeof(GameManager))).GetOwner(territory) != -1)
            {
                Destroy(gameObject);
            }
            once = false;
        }
        else
        {
            if (!once)
            {
                once = true;
                ((GameManager)FindObjectOfType(typeof(GameManager))).SetOwner(territory, ownerID);
                ((LevelLoader)FindObjectOfType(typeof(LevelLoader))).ChangeBorder(territory, ownerID);
            }
            if (!Ghost)
            {
                objectName = GetObjectName() + " (" + ResourceManager.GetLevelAlias(player.GetLevel(UpgradeableObjects.CityHall)) + ")";
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

    public void SetStats(int owner, int terr)
    {
        ownerID = owner;
        territory = terr;
    }

    public void OnDestroy()
    {
        try
        {
            if (!UnderConstruction() && !Ghost)
            {
                player.IncrementResourceLimit(ResourceType.Spacing, -25);
                ((GameManager)FindObjectOfType(typeof(GameManager))).SetOwner(territory, -1);
                ((LevelLoader)FindObjectOfType(typeof(LevelLoader))).ChangeBorder(territory, -1);
            }
        }
        catch
        {
            // Nu le gaseste la iesire din scena
        }
    }

    public override void SaveDetails(JsonWriter writer)
    {
        base.SaveDetails(writer);
        SaveManager.WriteFloat(writer, "TerrytoryID", territory);
        SaveManager.WriteFloat(writer, "OwnerID", ownerID);
    }

    protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
    {
        base.HandleLoadedProperty(reader, propertyName, readValue);
        switch (propertyName)
        {
            case "TerrytoryID": territory = (int)(double)readValue; break;
            case "OwnerID":
                ownerID = (int)(double)readValue;
                if (!needsBuilding)
                {
                    ((GameManager)FindObjectOfType(typeof(GameManager))).SetOwner(territory, ownerID);
                    ((LevelLoader)FindObjectOfType(typeof(LevelLoader))).ChangeBorder(territory, ownerID);
                }
                break;
        }
    }
    public override string GetObjectName()
    {
        return "City Hall";
    }
}