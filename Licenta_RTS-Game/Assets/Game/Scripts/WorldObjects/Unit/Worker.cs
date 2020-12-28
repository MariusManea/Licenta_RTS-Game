using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class Worker : Unit
{
    private bool newSpawn;
    public int buildSpeed;

    private Building currentProject;
    private bool building = false;
    private float amountBuilt = 0.0f;
    private int loadedProjectId = -1;

    public AudioClip finishedJobSound;
    public float finishedJobVolume = 1.0f;

    /*** Game Engine methods, all can be overridden by subclass ***/
    protected override void Awake()
    {
        base.Awake();
        newSpawn = true;
    }
    protected override void Start()
    {
        base.Start();
        actions = new string[] { "CityHall", "Refinery", "OilPump", "WarFactory", "Turret", "Wonder", "Dock" };
        building = false;
        currentProject = null;
        if (loadedSavedValues)
        {
            newSpawn = false;
        }
        if (player && loadedSavedValues && loadedProjectId >= 0)
        {
            WorldObjects obj = player.GetObjectForId(loadedProjectId);
            if (obj.GetType().IsSubclassOf(typeof(Building)))
            {
                building = true;
                currentProject = (Building)obj;
                SetBuilding(currentProject);
            }
        }
    }

    protected override void Update()
    {
        base.Update();
        if (!moving && !rotating)
        {
            if (building && currentProject && currentProject.UnderConstruction())
            {
                amountBuilt += buildSpeed * Time.deltaTime;
                int amount = Mathf.FloorToInt(amountBuilt);
                if (amount > 0)
                {
                    amountBuilt -= amount;
                    currentProject.Construct(amount);
                    if (!currentProject.UnderConstruction())
                    {
                        building = false;
                        if (currentProject.GetComponent<CityHall>() != null)
                        {
                            player.IncrementResourceLimit(ResourceType.Spacing, 25);
                        }
                        currentProject = null;
                        if (audioElement != null) audioElement.Play(finishedJobSound);
                    }
                }
            }
            else
            {
                building = false;
                currentProject = null;
            }
        }
    }

    protected override void InitialiseAudio()
    {
        base.InitialiseAudio();
        if (finishedJobVolume < 0.0f) finishedJobVolume = 0.0f;
        if (finishedJobVolume > 1.0f) finishedJobVolume = 1.0f;
        List<AudioClip> sounds = new List<AudioClip>();
        List<float> volumes = new List<float>();
        sounds.Add(finishedJobSound);
        volumes.Add(finishedJobVolume);
        audioElement.Add(sounds, volumes);
    }

    protected override void EnterCargo()
    {
        currentProject = null;
        building = false;
        amountBuilt = 0.0f;
        base.EnterCargo();
    }

    /*** Public Methods ***/

    public override void SetBuilding(Building project)
    {
        base.SetBuilding(project);
        if (!newSpawn)
        {
            StartMove(project.transform.position, project.gameObject);
            currentProject = project;
            building = true;
        }
        else
        {
            newSpawn = false;
        }
    }

    public override void PerformAction(string actionToPerform)
    {
        base.PerformAction(actionToPerform);
        CreateBuilding(actionToPerform);
    }

    public override void StartMove(Vector3 destination)
    {
        base.StartMove(destination);
        amountBuilt = 0.0f;
        building = false;
        currentProject = null;
    }

    private void CreateBuilding(string buildingName)
    {
        ResourceManager.Cost cost = ResourceManager.GetCost(buildingName);
        if (ResourceManager.Affordable(cost, player.AvailableResources()))
        {
            Vector3 buildPoint = new Vector3(transform.position.x, transform.position.y, transform.position.z + 10);
            if (player) player.CreateBuilding(buildingName, buildPoint, this, playingArea);
        }
    }

    public override void MouseClick(GameObject hitObject, Vector3 hitPoint, Player controller)
    {
        bool doBase = true;
        //only handle input if owned by a human player and currently selected
        if (player && player.isHuman && currentlySelected && hitObject && !WorkManager.ObjectIsGround(hitObject))
        {
            Building building = hitObject.transform.parent.GetComponent<Building>();
            if (building)
            {
                if (building.UnderConstruction())
                {
                    SetBuilding(building);
                    doBase = false;
                }
            }
        }
        if (doBase) base.MouseClick(hitObject, hitPoint, controller);
    }

    public override void SaveDetails(JsonWriter writer)
    {
        base.SaveDetails(writer);
        SaveManager.WriteBoolean(writer, "Building", building);
        SaveManager.WriteFloat(writer, "AmountBuilt", amountBuilt);
        if (currentProject) SaveManager.WriteInt(writer, "CurrentProjectId", currentProject.ObjectId);
    }
    protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
    {
        base.HandleLoadedProperty(reader, propertyName, readValue);
        switch (propertyName)
        {
            case "Building": building = (bool)readValue; break;
            case "AmountBuilt": amountBuilt = (float)(double)readValue; break;
            case "CurrentProjectId": loadedProjectId = (int)(System.Int64)readValue; break;
            default: break;
        }
    }

    protected override bool ShouldMakeDecision()
    {
        if (building) return false;
        return base.ShouldMakeDecision();
    }

    protected override void DecideWhatToDo()
    {
        base.DecideWhatToDo();
        List<WorldObjects> buildings = new List<WorldObjects>();
        foreach (WorldObjects nearbyObject in nearbyObjects)
        {
            if (nearbyObject.GetPlayer() != player) continue;
            Building nearbyBuilding = nearbyObject.GetComponent<Building>();
            if (nearbyBuilding && nearbyBuilding.UnderConstruction()) buildings.Add(nearbyObject);
        }
        WorldObjects nearestObject = WorkManager.FindNearestWorldObjectInListToPosition(buildings, transform.position);
        if (nearestObject)
        {
            Building closestBuilding = nearestObject.GetComponent<Building>();
            if (closestBuilding) SetBuilding(closestBuilding);
        }
    }
}