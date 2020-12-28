using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;
using Newtonsoft.Json;

public class Resource : WorldObjects
{
    //Public variables
    public float capacity;

    //Variables accessible by subclass
    protected float amountLeft;
    public ResourceType resourceType;

    private float refillTime;
    private float refillInterval;
    private float refillDuration;

    /*** Game Engine methods, all can be overridden by subclass ***/

    protected override void Start()
    {
        base.Start();
        if (loadedSavedValues) return;
        amountLeft = capacity;
        refillInterval = 300;
        refillTime = Time.time + refillInterval;
    }

    protected override void Update()
    {
        base.Update();
        if (Time.time > refillTime)
        {
            amountLeft = capacity;
            refillTime += refillInterval;
        }
    }

    /*** Public methods ***/

    public void Remove(float amount)
    {
        amountLeft -= amount;
        if (amountLeft < 0) amountLeft = 0;
    }

    public bool isEmpty()
    {
        return amountLeft <= 0;
    }

    public ResourceType GetResourceType()
    {
        return resourceType;
    }

    protected override void CalculateCurrentHealth(float lowSplit, float highSplit)
    {
        healthPercentage = amountLeft / capacity;
        healthStyle.normal.background = ResourceManager.GetResourceHealthBar(resourceType);
    }

    public override void SaveDetails(JsonWriter writer)
    {
        base.SaveDetails(writer);
        SaveManager.WriteFloat(writer, "AmountLeft", amountLeft);
        SaveManager.WriteFloat(writer, "Capacity", capacity);
    }

    protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
    {
        base.HandleLoadedProperty(reader, propertyName, readValue);
        switch (propertyName)
        {
            case "AmountLeft": amountLeft = (float)(double)readValue; break;
            case "Capacity": capacity = (float)(double)readValue; break;
            default: break;
        }
    }

    public virtual bool Harvestable()
    {
        return true;
    }

    protected override bool ShouldMakeDecision()
    {
        return false;
    }
}
