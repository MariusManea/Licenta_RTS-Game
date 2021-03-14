using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using RTS;

public class OilPump : Building
{
    private float extractingTime;
    private GameObject oilPileSupport;
    private int loadedOilPileId;
    protected override void Start()
    {
        base.Start();        
        extractingTime = 0;

        if (player && loadedSavedValues && loadedOilPileId >= 0)
        {
            WorldObjects obj = player.GetObjectForId(loadedOilPileId);
            if (obj.GetType().IsSubclassOf(typeof(Resource)))
            {
                oilPileSupport = obj.gameObject;
                foreach (Renderer renderer in oilPileSupport.GetComponentsInChildren<MeshRenderer>())
                {
                    renderer.enabled = false;
                }
                foreach (Collider collider in oilPileSupport.GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }
            }
        }
    }

    protected override void Update()
    {
        base.Update();
        if (!UnderConstruction() && !Ghost)
        {
            ExtractOil();
            objectName = GetObjectName() + " (" + ResourceManager.GetLevelAlias(player.GetLevel(UpgradeableObjects.OilPump)) + ")";
        }
    }

    private void ExtractOil()
    {
        extractingTime += Time.deltaTime;
        if (extractingTime > 1)
        {
            int rate = 3 * player.GetLevel(UpgradeableObjects.OilPump);
            int times = Mathf.FloorToInt(extractingTime);
            rate *= times;
            player.AddResource(ResourceType.Oil, rate);
            extractingTime = 0;
        }
    }

    public override string GetObjectName()
    {
        return "Oil Pump";
    }

    public void SetPile(GameObject oilPile)
    {
        oilPileSupport = oilPile;
    }

    public void OnDestroy()
    {
        if (oilPileSupport)
        {
            foreach (Renderer renderer in oilPileSupport.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.enabled = true;
            }
            foreach (Collider collider in oilPileSupport.GetComponentsInChildren<Collider>())
            {
                collider.enabled = true;
            }
        }
    }

    public override void SaveDetails(JsonWriter writer)
    {
        base.SaveDetails(writer);
        SaveManager.WriteInt(writer, "OilPileId", oilPileSupport.GetComponent<WorldObjects>().ObjectId);
    }
    protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
    {
        base.HandleLoadedProperty(reader, propertyName, readValue);
        switch (propertyName)
        {
            case "OilPileId": loadedOilPileId = (int)(System.Int64)readValue; break;
            default: break;
        }
    }
}
