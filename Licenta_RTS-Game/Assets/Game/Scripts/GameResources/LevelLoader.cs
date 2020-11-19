using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class LevelLoader : MonoBehaviour
{
    private static int nextObjectId = 0;
    private static bool created = false;
    private bool initialised = false;

    void Awake()
    {
        if (!created)
        {
            DontDestroyOnLoad(transform.gameObject);
            created = true;
            initialised = true;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void OnLevelWasLoaded()
    {
        if (initialised)
        {
            if (ResourceManager.LevelName != null && ResourceManager.LevelName != "")
            {
                LoadManager.LoadGame(ResourceManager.LevelName);
            }
            else
            {
                WorldObjects[] worldObjects = GameObject.FindObjectsOfType(typeof(WorldObjects)) as WorldObjects[];
                foreach (WorldObjects worldObject in worldObjects)
                {
                    worldObject.ObjectId = nextObjectId++;
                    if (nextObjectId >= int.MaxValue) nextObjectId = 0;
                }
            }
        }
    }

    public int GetNewObjectId()
    {
        nextObjectId++;
        if (nextObjectId >= int.MaxValue) nextObjectId = 0;
        return nextObjectId;
    }
}
