﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

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
    }

    protected void ProcessBuildQueue()
    {
        if (buildQueue.Count > 0)
        {
            currentBuildProgress += Time.deltaTime * ResourceManager.BuildSpeed;
            if (currentBuildProgress > maxBuildProgress)
            {
                if (player) player.AddUnit(buildQueue.Dequeue(), spawnPoint, rallyPoint, transform.rotation, this);
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
            if (hoverObject.name == "Ground")
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
            if (hitObject.name == "Ground")
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
        }
    }

    private void SetSpawnPoint()
    {
        float spawnX = selectionBounds.center.x + transform.forward.x * selectionBounds.extents.x + transform.forward.x * 10;
        float spawnZ = selectionBounds.center.z + transform.forward.z * selectionBounds.extents.z + transform.forward.z * 10;
        spawnPoint = new Vector3(spawnX, 0.0f, spawnZ);
        rallyPoint = spawnPoint;
    }
}