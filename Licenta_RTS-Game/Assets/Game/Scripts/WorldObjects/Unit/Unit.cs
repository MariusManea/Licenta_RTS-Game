using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;
using Pathfinding;
using Newtonsoft.Json;

public class Unit : WorldObjects
{
    protected bool moving, rotating;

    protected Vector3 destination = ResourceManager.InvalidPosition;
    protected Quaternion targetRotation;
    public float moveSpeed, rotateSpeed;
    protected GameObject destinationTarget;
    private int loadedDestinationTargetId = -1;
    private int loadedDestinationLoadingTargetId = -1;

    public AudioClip driveSound, moveSound;
    public float driveVolume = 0.5f, moveVolume = 1.0f;
    public AstarPath graph;
    public NavGraph navGraph;

    public CargoShip loadingTarget;

    protected override void Awake()
    {
        base.Awake();
        graph = FindObjectOfType<AstarPath>();
        navGraph = graph.data.graphs[0];
    }

    protected override void Start()
    {
        base.Start();
        if (player && loadedSavedValues && loadedDestinationTargetId >= 0)
        {
            destinationTarget = player.GetObjectForId(loadedDestinationTargetId).gameObject;
        }
        if (player && loadedSavedValues && loadedDestinationLoadingTargetId >= 0)
        {
            loadingTarget = player.GetObjectForId(loadedDestinationLoadingTargetId).GetComponent<CargoShip>();
        }
        if (destination != ResourceManager.InvalidPosition)
        {
            StartMove(destination);
        }
    }

    protected override void Update()
    {
        base.Update();
        if (rotating) TurnToTarget();
        else if (moving) MakeMove();
        else if (loadingTarget != null)
        {
            if (WorkManager.IsWorldObjectNearby(loadingTarget, transform.position))
            {
                EnterCargo();
            }
        }
    }

    protected override void OnGUI()
    {
        base.OnGUI();
    }

    protected override void InitialiseAudio()
    {
        base.InitialiseAudio();
        List<AudioClip> sounds = new List<AudioClip>();
        List<float> volumes = new List<float>();
        if (driveVolume < 0.0f) driveVolume = 0.0f;
        if (driveVolume > 1.0f) driveVolume = 1.0f;
        volumes.Add(driveVolume);
        sounds.Add(driveSound);
        if (moveVolume < 0.0f) moveVolume = 0.0f;
        if (moveVolume > 1.0f) moveVolume = 1.0f;
        sounds.Add(moveSound);
        volumes.Add(moveVolume);
        audioElement.Add(sounds, volumes);
    }

    protected virtual void EnterCargo()
    {
        destination = ResourceManager.InvalidPosition;
        destinationTarget = null;
        loadingTarget.LoadUnit(this);
    }

    public override void SetHoverState(GameObject hoverObject)
    {
        base.SetHoverState(hoverObject);
        //only handle input if owned by a human player and currently selected
        if (player && player.isHuman && currentlySelected)
        {
            bool moveHover = false;
            bool cargoHover = false;
            if (Input.mousePosition.x > ResourceManager.ScrollWidth && Input.mousePosition.y > ResourceManager.ScrollWidth)
            {
                if (WorkManager.ObjectIsGround(hoverObject))
                {
                    moveHover = true;
                }
                else
                {
                    Resource resource = hoverObject.transform.parent.GetComponent<Resource>();
                    if (resource && resource.isEmpty()) moveHover = true;
                }
                if (WorkManager.ObjectIsCargo(hoverObject) && GetComponent<Ship>() == null)
                {
                    cargoHover = true;
                    moveHover = false;
                }
            }
            if (moveHover) player.hud.SetCursorState(CursorState.Move);
            if (cargoHover) player.hud.SetCursorState(CursorState.Load);
        }
    }

    public override void MouseClick(GameObject hitObject, Vector3 hitPoint, Player controller)
    {
        if (player && player.isHuman && currentlySelected && WorkManager.ObjectIsCargo(hitObject) && hitPoint != ResourceManager.InvalidPosition)
        {
            if (this.gameObject.GetComponent<Ship>() == null)
            {
                // SetTarget, MoveToTarget, WhatIfCancels? if AtTarget then -> hitObject.GetComponent<CargoShip>().LoadUnit(GetComponent<Unit>());
                destinationTarget = hitObject;
                loadingTarget = hitObject.transform.parent.GetComponent<CargoShip>();
                Vector3 position = GetClosestValidDestination(hitPoint);
                StartMove(position);
                loadingTarget.StartMove(position);
            } 
            else
            {
                bool unitsSelected = false;
                foreach(WorldObjects wO in player.SelectedObjects)
                {
                    if (!wO.GetComponent<Ship>())
                    {
                        unitsSelected = true;
                        break;
                    }
                }
                if (!unitsSelected) ChangeSelection(hitObject.transform.parent.GetComponent<WorldObjects>(), controller);
            }
        }
        else
        {
            loadingTarget = null;
            base.MouseClick(hitObject, hitPoint, controller);
            //only handle input if owned by a human player and currently selected
            if (player && player.isHuman && currentlySelected)
            {
                bool clickedOnEmptyResource = false;
                if (hitObject.transform.parent)
                {
                    Resource resource = hitObject.transform.parent.GetComponent<Resource>();
                    if (resource && resource.isEmpty()) clickedOnEmptyResource = true;
                }
                if ((WorkManager.ObjectIsGround(hitObject) || WorkManager.ObjectIsWater(hitObject) || clickedOnEmptyResource) && hitPoint != ResourceManager.InvalidPosition)
                {
                    attacking = false;
                    target = null;
                    StartMove(hitPoint);
                }
            }
        }
    }

    protected Vector3 GetClosestValidDestination(Vector3 destination)
    {
        int d = 1;
        while (true)
        {
            for (int i = 0; i < 36; i++)
            {
                Vector3 newPosition = destination + new Vector3(d * Mathf.Cos(2 * Mathf.PI * (float)i / 36.0f), 0, d * Mathf.Sin(2 * Mathf.PI * (float)i / 36.0f));
                newPosition.y = terrain.SampleHeight(newPosition);
                if (navGraph.GetNearest(newPosition).node.Walkable)
                {

                    return newPosition;
                }
            }
            d++;
            if (d > 50)
            {
                return ResourceManager.InvalidPosition;
            }
        }
    }

    public virtual void StartMove(Vector3 destination)
    {
        if (audioElement != null) audioElement.Play(moveSound);
        if (navGraph.GetNearest(destination).node.Walkable)
        {
            this.destination = navGraph.GetNearest(destination).clampedPosition;
        }
        else
        {
            if (destinationTarget == null)
            {
                this.destination = GetClosestValidDestination(destination);
                if (this.destination == ResourceManager.InvalidPosition)
                {
                    return;
                }
            }
            else
            {
                this.destination = navGraph.GetNearest(destination).clampedPosition;
            }
        }
        this.destination = new Vector3(this.destination.x, terrain.SampleHeight(this.destination), this.destination.z);
        destinationTarget = null;
        GetComponent<AIPath>().enabled = true;
        GetComponent<AIPath>().destination = this.destination;
        Vector3 rotateTo = this.transform.position;
        rotateTo = Vector3.Lerp(rotateTo, destination, 0.01f);
        rotateTo.y = terrain.SampleHeight(rotateTo);
        targetRotation = Quaternion.LookRotation(rotateTo - transform.position);
        rotating = true;
        moving = false;
    }

    public void StartMove(Vector3 destination, GameObject destinationTarget)
    {
        StartMove(destination);
        this.destinationTarget = destinationTarget;
    }

    private void TurnToTarget()
    {
        CalculateBounds();
        rotating = false;
        moving = true;
        if (audioElement != null) audioElement.Play(driveSound);
        if (destinationTarget)
        {
            CalculateTargetDestination();
            GetComponent<AIPath>().destination = this.destination;
        }
        return;
        /*transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed);
        //sometimes it gets stuck exactly 180 degrees out in the calculation and does nothing, this check fixes that
        Quaternion inverseTargetRotation = new Quaternion(-targetRotation.x, -targetRotation.y, -targetRotation.z, -targetRotation.w);
        if (transform.rotation == targetRotation || transform.rotation == inverseTargetRotation)
        {
            if (audioElement != null) audioElement.Play(driveSound);
            rotating = false;
            moving = true;
        }*/
    }

    private void MakeMove()
    {
        Vector3 ahead = this.transform.position + this.transform.forward;
        ahead.y = terrain.SampleHeight(ahead);
        transform.rotation = Quaternion.RotateTowards(this.transform.rotation, Quaternion.LookRotation(ahead - this.transform.position), Time.deltaTime * moveSpeed);
        // transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * moveSpeed);
        if (transform.position == destination || GetComponent<AIPath>().reachedDestination)
        {
            if (audioElement != null) audioElement.Stop(driveSound);
            moving = false;
            GetComponent<AIPath>().enabled = false;
            movingIntoPosition = false;
            destination = ResourceManager.InvalidPosition;
        }
        CalculateBounds();
    }

    private void CalculateTargetDestination()
    {
        //calculate number of unit vectors from unit centre to unit edge of bounds
        Vector3 originalExtents = selectionBounds.extents;
        Vector3 normalExtents = originalExtents;
        normalExtents.Normalize();
        float numberOfExtents = originalExtents.x / normalExtents.x;
        int unitShift = Mathf.FloorToInt(numberOfExtents);

        //calculate number of unit vectors from target centre to target edge of bounds
        WorldObjects worldObject = destinationTarget.GetComponent<WorldObjects>();
        if (worldObject) originalExtents = worldObject.GetSelectionBounds().extents;
        else originalExtents = new Vector3(0.0f, 0.0f, 0.0f);
        normalExtents = originalExtents;
        normalExtents.Normalize();
        numberOfExtents = originalExtents.x / normalExtents.x;
        int targetShift = Mathf.FloorToInt(numberOfExtents);

        //calculate number of unit vectors between unit centre and destination centre with bounds just touching
        int shiftAmount = targetShift + unitShift;

        //calculate direction unit needs to travel to reach destination in straight line and normalize to unit vector
        Vector3 origin = transform.position;
        Vector3 direction = new Vector3(destination.x - origin.x, 0.0f, destination.z - origin.z);
        direction.Normalize();

        //destination = center of destination - number of unit vectors calculated above
        //this should give us a destination where the unit will not quite collide with the target
        //giving the illusion of moving to the edge of the target and then stopping
        for (int i = 0; i < shiftAmount; i++) destination -= direction;
        destination.y = terrain.SampleHeight(destination);
        destination = GetClosestValidDestination(destination);
        destination.y = terrain.SampleHeight(destination);
        destinationTarget = null;
    }

    public virtual void SetBuilding(Building creator)
    {
        //specific initialization for a unit can be specified here
    }

    public override void SaveDetails(JsonWriter writer)
    {
        base.SaveDetails(writer);
        SaveManager.WriteBoolean(writer, "Moving", moving);
        SaveManager.WriteBoolean(writer, "Rotating", rotating);
        SaveManager.WriteVector(writer, "Destination", destination);
        SaveManager.WriteQuaternion(writer, "TargetRotation", targetRotation);
        if (destinationTarget)
        {
            WorldObjects destinationObject = destinationTarget.GetComponent<WorldObjects>();
            if (destinationObject) SaveManager.WriteInt(writer, "DestinationTargetId", destinationObject.ObjectId);
        }
        if (loadingTarget)
        {
            SaveManager.WriteInt(writer, "LoadingTargetId", loadingTarget.ObjectId);
        }
    }

    protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
    {
        base.HandleLoadedProperty(reader, propertyName, readValue);
        switch (propertyName)
        {
            case "Moving": moving = (bool)readValue; break;
            case "Rotating": rotating = (bool)readValue; break;
            case "Destination": destination = LoadManager.LoadVector(reader); break;
            case "TargetRotation": targetRotation = LoadManager.LoadQuaternion(reader); break;
            case "DestinationTargetId": loadedDestinationTargetId = (int)(System.Int64)readValue; break;
            case "LoadingTargetId": loadedDestinationLoadingTargetId = (int)(System.Int64)readValue; break;
            default: break;
        }
    }
    protected override bool ShouldMakeDecision()
    {
        if (moving || rotating) return false;
        return base.ShouldMakeDecision();
    }

}
