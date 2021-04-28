using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using RTS;
using Unity.MLAgents.Sensors;

public class AgentRTS : Agent
{
    public Color team;
    public Entity entity;

    [HideInInspector]
    public Player owner;
    [HideInInspector]
    public Unit unitController;
    [HideInInspector]
    public Building buildingController;
    private bool moveable;

    private int buildID = 0;

    BehaviorParameters m_BehaviorParameters;

    public void Start()
    {
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        team = FindObjectOfType<LevelLoader>().teamColors[m_BehaviorParameters.TeamId];
        foreach (Player player in FindObjectsOfType<Player>())
        {
            if (player.teamColor == team)
            {
                owner = player;
                break;
            }
        }
        unitController = gameObject.GetComponent<Unit>();
        buildingController = gameObject.GetComponent<Building>();

        if (unitController)
        {
            moveable = true;
        }
        else
        {
            moveable = false;
        }
    }

    public override void Initialize()
    {
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        team = FindObjectOfType<LevelLoader>().teamColors[m_BehaviorParameters.TeamId];
        foreach (Player player in FindObjectsOfType<Player>())
        {
            if (player.teamColor == team)
            {
                owner = player;
                break;
            }
        }
        unitController = gameObject.GetComponent<Unit>();
        buildingController = gameObject.GetComponent<Building>();

        if (unitController)
        {
            moveable = true;
        } 
        else
        {
            moveable = false;
        }
    }

    public override void OnEpisodeBegin()
    {
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        team = FindObjectOfType<LevelLoader>().teamColors[m_BehaviorParameters.TeamId];
        foreach (Player player in FindObjectsOfType<Player>())
        {
            if (player.teamColor == team)
            {
                owner = player;
                break;
            }
        }
        unitController = gameObject.GetComponent<Unit>();
        buildingController = gameObject.GetComponent<Building>();

        if (unitController)
        {
            moveable = true;
        }
        else
        {
            moveable = false;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (moveable)
        {
            // Units
            sensor.AddObservation(unitController.hitPoints);
            List<WorldObjects> nearbyObjects = WorkManager.FindNearbyObjects(this.transform.position, 250);
            // #friends
            int nFriends = 1;
            // #enemies
            int nEnemies = 0;
            // distance to closest friend
            float friendDistance = int.MaxValue;
            // distance to closest enemy
            float enemyDistance = int.MaxValue;
            // distance to closest resource
            float resourceDistance = int.MaxValue;
            // distance to closest friendly Hall
            float hallDistance = int.MaxValue;
            foreach (WorldObjects worldObj in nearbyObjects)
            {
                float dist = Vector3.Distance(this.transform.position, worldObj.transform.position);
                if (worldObj.IsOwnedBy(owner))
                {
                    nFriends++;
                    if (worldObj.GetObjectName() == entity.ToString())
                    {
                        if (dist < friendDistance)
                        {
                            friendDistance = dist;
                        }
                    }
                    if (worldObj.GetObjectName() == "Town Center" || worldObj.GetObjectName() == "City Hall")
                    {
                        if (dist < hallDistance)
                        {
                            hallDistance = dist;
                        }
                    }
                }
                else
                {
                    if (worldObj.CanAttack())
                    {
                        nEnemies++;
                        if (dist < enemyDistance)
                        {
                            enemyDistance = dist;
                        }
                    }
                }
                if (WorkManager.ObjectIsOre(worldObj.gameObject))
                {
                    if (dist < resourceDistance)
                    {
                        resourceDistance = dist;
                    }
                }
            }
            sensor.AddObservation(nFriends - nEnemies);
            sensor.AddObservation(enemyDistance);
            if (entity == Entity.Worker || entity == Entity.Tank || entity == Entity.BattleShip || entity == Entity.BatteringRam)
            {
                sensor.AddObservation((friendDistance != int.MaxValue) ? friendDistance : 0);
            }
            if (entity == Entity.Worker)
            {
                sensor.AddObservation(hallDistance);
            }
            if (entity == Entity.Harvester || entity == Entity.RustyHarvester)
            {
                sensor.AddObservation(resourceDistance);
                sensor.AddObservation(owner.GetResourceAmount(ResourceType.Copper));
                sensor.AddObservation(owner.GetResourceAmount(ResourceType.Iron));
                sensor.AddObservation(owner.GetResourceAmount(ResourceType.Gold));
            } 
        }
        else
        {
            // Buildings
            sensor.AddObservation(owner.GetResourceAmount(ResourceType.Spacing));
            sensor.AddObservation(owner.GetResourceAmount(ResourceType.Copper));
            sensor.AddObservation(owner.GetResourceAmount(ResourceType.Iron));
            sensor.AddObservation(owner.GetResourceAmount(ResourceType.Oil));
            sensor.AddObservation(owner.GetResourceAmount(ResourceType.Gold));
            if (entity == Entity.University)
            {
                sensor.AddObservation(owner.GetResourceAmount(ResourceType.ResearchPoint));
            }
        }
    }

    public void FixedUpdate()
    {
        if (owner.IsFindingBuildingLocation())
        {
            if (owner.CanPlaceBuilding())
            {
                buildID = 0;
                owner.StartConstruction();
                AddReward(-0.5f);
            }
        }
    }

    private void MoveUnit(Unit controller, int forwardMove, int sideMove)
    {
        AddReward(-0.001f);
        Vector3 target = controller.transform.position;
        target += Vector3.forward * (forwardMove == 1 ? 5f : (forwardMove == 2 ? -5f : 0f));
        target += Vector3.right * (sideMove == 1 ? -5f : (sideMove == 2 ? 5f : 0f));
        controller.StartMove(target);
    }

    private void ConstructBuilding(Worker controller, int buildType)
    {
        if (buildType <= 0)
            return;
        if (buildType == 1)
        {
            Buildings friendlyBuildings = owner.GetComponentInChildren<Buildings>();
            Building[] buildingsList = friendlyBuildings.GetComponentsInChildren<Building>();
            float closestDistance = int.MaxValue;
            Building helpProj = null;
            foreach(Building building in buildingsList)
            {
                if (building.UnderConstruction())
                {
                    float dist = Vector3.Distance(transform.position, building.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        helpProj = building;
                    }
                }
            }
            if (helpProj != null)
            {
                controller.StartMove(helpProj.transform.position, helpProj.gameObject);
            }
        }
        string buildName = controller.GetPotentialActions()[buildType - 2];
        controller.PerformAction(buildName);
    }

    private void HarvestResources(Harvester controller, int resourceType)
    {
        ResourceType resourceToHarvest = ResourceType.Unknown;
        switch (resourceType)
        {
            case 1: resourceToHarvest = ResourceType.CopperOre; break;
            case 2: resourceToHarvest = ResourceType.IronOre; break;
            case 3: resourceToHarvest = ResourceType.GoldOre; break;
            default: break;
        }
        if (resourceToHarvest == ResourceType.Unknown) return;
        List<WorldObjects> nearbyObjects = WorkManager.FindNearbyObjects(this.transform.position, 250);
        float closestDistance = int.MaxValue;
        Resource closestResource = null;
        foreach (WorldObjects obj in nearbyObjects)
        {
            if (WorkManager.ObjectIsOre(obj.gameObject))
            {
                if ((obj as Resource).GetResourceType() == resourceToHarvest)
                {
                    float dist = Vector3.Distance(obj.transform.position, this.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestResource = obj as Resource;
                    }
                }
            }
        }
        controller.StartHarvest(closestResource);
    }

    private void UnitController(Unit controller, ActionSegment<int> act)
    {
        var forwardMove = act[0]; // move forward
        var sideMove = act[1]; // move backward
        var ownAction = act[2];
        /* 
         * 0 = Do nothing
         * Worker - build
         * 1 = Help Build; 2 = City Hall; 3 = University; 4 = Refinery; 5 = Oil Pump; 6 = War Factory; 7 = Turret; 8 = Wonder; 9 = Dock
         * Attackers (Tank, BatteringRam, BattleShip) - attack
         * 1 = Attack closest enemy
         * Harvester - harvest
         * 1 = Harvest Copper, 2 = Harvest Iron, 3 = Harvest Gold
         * Cargo - unload
         * 1 = Unload units
         */
        if (forwardMove != 0 || sideMove != 0)
        {
            MoveUnit(controller, forwardMove, sideMove);
        }
        switch (entity)
        {
            case Entity.Worker:
                ConstructBuilding(controller as Worker, ownAction);
                break;
            case Entity.Harvester:
                HarvestResources(controller as Harvester, ownAction);
                break;
            default:
                break;
        }
    }

    private void BuildingController(Building controller, ActionSegment<int> act)
    {

    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        WorldObjects obj = (unitController != null) ? unitController as WorldObjects : buildingController;
        List<int> mask = new List<int>();
        List<string> allowed = new List<string>(obj.GetActions());
        int idx = 2;
        foreach (string act in obj.GetPotentialActions())
        {
            if (!allowed.Contains(act) || owner.IsFindingBuildingLocation())
            {
                mask.Add(idx);
            }
            idx++;
        }
        if (moveable)
        {
            actionMask.WriteMask(2, mask);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (moveable)
        {
            UnitController(unitController, actionBuffers.DiscreteActions);
        }
        else
        {
            BuildingController(buildingController, actionBuffers.DiscreteActions);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;
        discreteActionsOut[1] = 0;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 2;
        }
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            buildID = (buildID + 1) % 10;
        }
        discreteActionsOut[2] = buildID;
    }

    private void OnDestroy()
    {
        AddReward(-10);
    }

}
