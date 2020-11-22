using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;
using Newtonsoft.Json;

public class Unit : WorldObjects
{
    protected bool moving, rotating;

    private Vector3 destination;
    private Quaternion targetRotation;
    public float moveSpeed, rotateSpeed;
    private GameObject destinationTarget;
    private int loadedDestinationTargetId = -1;

    public AudioClip driveSound, moveSound;
    public float driveVolume = 0.5f, moveVolume = 1.0f;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        if (player && loadedSavedValues && loadedDestinationTargetId >= 0)
        {
            destinationTarget = player.GetObjectForId(loadedDestinationTargetId).gameObject;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (rotating) TurnToTarget();
        else if (moving) MakeMove();
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

    public override void SetHoverState(GameObject hoverObject)
    {
        base.SetHoverState(hoverObject);
        //only handle input if owned by a human player and currently selected
        if (player && player.isHuman && currentlySelected)
        {
            bool moveHover = false;
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
            }
            if (moveHover) player.hud.SetCursorState(CursorState.Move);
        }
    }

    public override void MouseClick(GameObject hitObject, Vector3 hitPoint, Player controller)
    {
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
            if ((WorkManager.ObjectIsGround(hitObject) || clickedOnEmptyResource) && hitPoint != ResourceManager.InvalidPosition)
            {
                attacking = false;
                target = null;
                float x = hitPoint.x;
                //makes sure that the unit stays on top of the surface it is on
                float y = hitPoint.y + player.SelectedObject.transform.position.y;
                float z = hitPoint.z;
                Vector3 destination = new Vector3(x, y, z);
                StartMove(destination);
            }
        }
    }

    public virtual void StartMove(Vector3 destination)
    {
        if (audioElement != null) audioElement.Play(moveSound);
        destinationTarget = null;
        this.destination = destination;
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

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed);
        //sometimes it gets stuck exactly 180 degrees out in the calculation and does nothing, this check fixes that
        Quaternion inverseTargetRotation = new Quaternion(-targetRotation.x, -targetRotation.y, -targetRotation.z, -targetRotation.w);
        if (transform.rotation == targetRotation || transform.rotation == inverseTargetRotation)
        {
            if (audioElement != null) audioElement.Play(driveSound);
            rotating = false;
            moving = true;
        }
        CalculateBounds();
        if (destinationTarget) CalculateTargetDestination();
    }

    private void MakeMove()
    {
        this.destination.y = terrain.SampleHeight(this.transform.position);
        Vector3 ahead = this.transform.position;
        ahead = Vector3.Lerp(ahead, destination, Time.deltaTime * moveSpeed);
        ahead.y = terrain.SampleHeight(ahead);
        transform.rotation = Quaternion.RotateTowards(this.transform.rotation, Quaternion.LookRotation(ahead - this.transform.position), Time.deltaTime * moveSpeed);
        transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * moveSpeed);
        
        if (transform.position == destination)
        {
            if (audioElement != null) audioElement.Stop(driveSound);
            moving = false;
            movingIntoPosition = false;
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
        destination.y = destinationTarget.transform.position.y;
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
            default: break;
        }
    }
    protected override bool ShouldMakeDecision()
    {
        if (moving || rotating) return false;
        return base.ShouldMakeDecision();
    }

}
