using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;
using Newtonsoft.Json;

public class Building : WorldObjects    
{
    public override bool IsActive { get { return !needsBuilding; } }

    public float maxBuildProgress;
    protected Queue<string> buildQueue;
    private float currentBuildProgress = 0.0f;
    protected Vector3 spawnPoint;
    protected Vector3 rallyPoint;
    public Texture2D rallyPointImage;
    public Texture2D sellImage;

    public AudioClip finishedJobSound;
    public float finishedJobVolume = 1.0f;

    private bool needsBuilding = false;

    protected override void Awake()
    {
        base.Awake();
        buildQueue = new Queue<string>();
        SetSpawnPoint();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
        ProcessBuildQueue();
    }

    protected override void OnGUI()
    {
        base.OnGUI();
        if (needsBuilding) DrawBuildProgress();
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

    private void DrawBuildProgress()
    {
        GUI.skin = ResourceManager.SelectBoxSkin;
        Rect selectBox = WorkManager.CalculateSelectionBox(selectionBounds, playingArea);
        //Draw the selection box around the currently selected object, within the bounds of the main draw area
        GUI.BeginGroup(playingArea);
        CalculateCurrentHealth(0.5f, 0.99f);
        DrawHealthBar(selectBox, "Building ...");
        GUI.EndGroup();
    }

    protected void CreateUnit(string unitName)
    {
        buildQueue.Enqueue(unitName);
        GameObject unit = ResourceManager.GetUnit(unitName);
        Unit unitObject = unit.GetComponent<Unit>();
        if (player && unitObject) player.RemoveResource(ResourceType.Money, unitObject.cost);
    }

    protected void ProcessBuildQueue()
    {
        if (buildQueue.Count > 0)
        {
            currentBuildProgress += Time.deltaTime * ResourceManager.BuildSpeed;
            if (currentBuildProgress > maxBuildProgress)
            {
                if (player)
                {
                    player.AddUnit(buildQueue.Dequeue(), spawnPoint, rallyPoint, transform.rotation, this);
                    if (audioElement != null) audioElement.Play(finishedJobSound);
                }
                currentBuildProgress = 0.0f;
            }
        }
    }

    public string[] getBuildQueueValues()
    {
        string[] values = new string[buildQueue.Count];
        int pos = 0;
        foreach (string unit in buildQueue) values[pos++] = unit;
        return values;
    }

    public float getBuildPercentage()
    {
        return currentBuildProgress / maxBuildProgress;
    }

    public override void SetSelection(bool selected, Rect playingArea)
    {
        base.SetSelection(selected, playingArea);
        if (player)
        {
            RallyPoints flag = player.GetComponentInChildren<RallyPoints>();
            if (selected)
            {
                if (flag && player.isHuman && spawnPoint != ResourceManager.InvalidPosition && rallyPoint != ResourceManager.InvalidPosition)
                {
                    flag.transform.localPosition = rallyPoint;
                    flag.transform.forward = transform.forward;
                    flag.Enable();
                }
            }
            else
            {
                if (flag && player.isHuman) flag.Disable();
            }
        }
    }

    public bool hasSpawnPoint()
    {
        return spawnPoint != ResourceManager.InvalidPosition && rallyPoint != ResourceManager.InvalidPosition;
    }

    public override void SetHoverState(GameObject hoverObject)
    {
        base.SetHoverState(hoverObject);
        //only handle input if owned by a human player and currently selected
        if (player && player.isHuman && currentlySelected)
        {
            if (WorkManager.ObjectIsGround(hoverObject))
            {
                if (player.hud.GetPreviousCursorState() == CursorState.RallyPoint) player.hud.SetCursorState(CursorState.RallyPoint);
            }
        }
    }

    public override void MouseClick(GameObject hitObject, Vector3 hitPoint, Player controller)
    {
        base.MouseClick(hitObject, hitPoint, controller);
        //only handle iput if owned by a human player and currently selected
        if (player && player.isHuman && currentlySelected)
        {
            if (WorkManager.ObjectIsGround(hitObject))
            {
                if ((player.hud.GetCursorState() == CursorState.RallyPoint || player.hud.GetPreviousCursorState() == CursorState.RallyPoint) && hitPoint != ResourceManager.InvalidPosition)
                {
                    SetRallyPoint(hitPoint);
                }
            }
        }
    }

    public void SetRallyPoint(Vector3 position)
    {
        rallyPoint = position;
        if (player && player.isHuman && currentlySelected)
        {
            RallyPoints flag = player.GetComponentInChildren<RallyPoints>();
            if (flag) flag.transform.localPosition = rallyPoint;
        }
    }

    public void Sell()
    {
        if (player) player.AddResource(ResourceType.Money, sellValue);
        if (currentlySelected) SetSelection(false, playingArea);
        Destroy(this.gameObject);
    }

    public virtual bool Sellable()
    {
        return true;
    }

    public void StartConstruction()
    {
        CalculateBounds();
        needsBuilding = true;
        hitPoints = 0;
        SetSpawnPoint();
    }

    public bool UnderConstruction()
    {
        return needsBuilding;
    }

    public void Construct(int amount)
    {
        hitPoints += amount;
        if (hitPoints >= maxHitPoints)
        {
            hitPoints = maxHitPoints;
            needsBuilding = false;
            RestoreMaterials();
            SetTeamColor();
        }
    }

    public void SetSpawnPoint()
    {
        float spawnX = selectionBounds.center.x + transform.forward.x * selectionBounds.extents.x + transform.forward.x * 10;
        float spawnZ = selectionBounds.center.z + transform.forward.z * selectionBounds.extents.z + transform.forward.z * 10;
        spawnPoint = new Vector3(spawnX, 0.0f, spawnZ);
        rallyPoint = spawnPoint;
    }

    public override void SaveDetails(JsonWriter writer)
    {
        base.SaveDetails(writer);
        SaveManager.WriteBoolean(writer, "NeedsBuilding", needsBuilding);
        SaveManager.WriteVector(writer, "SpawnPoint", spawnPoint);
        SaveManager.WriteVector(writer, "RallyPoint", rallyPoint);
        SaveManager.WriteFloat(writer, "BuildProgress", currentBuildProgress);
        SaveManager.WriteStringArray(writer, "BuildQueue", buildQueue.ToArray());
        if (needsBuilding) SaveManager.WriteRect(writer, "PlayingArea", playingArea);
    }
    protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
    {
        base.HandleLoadedProperty(reader, propertyName, readValue);
        switch (propertyName)
        {
            case "NeedsBuilding": needsBuilding = (bool)readValue; break;
            case "SpawnPoint": spawnPoint = LoadManager.LoadVector(reader); break;
            case "RallyPoint": rallyPoint = LoadManager.LoadVector(reader); break;
            case "BuildProgress": currentBuildProgress = (float)(double)readValue; break;
            case "BuildQueue": buildQueue = new Queue<string>(LoadManager.LoadStringArray(reader)); break;
            case "PlayingArea": playingArea = LoadManager.LoadRect(reader); break;
            default: break;
        }
    }

}
