using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class GameObjectsList : MonoSingleton<GameObjectsList>
{
    public Building[] buildings;
    public Unit[] units;
    public WorldObjects[] worldObjects;
    public GameObject[] gameObjects;
    public GameObject player;
    public Texture2D[] avatars;

    void Awake()
    {
        if (this != Instance) return;
        ResourceManager.SetGameObjectList(this);
        PlayerManager.Load();
        PlayerManager.SetAvatarTextures(avatars);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Texture2D[] GetAvatars()
    {
        if (avatars == null) return null;
        return avatars;
    }

    public GameObject GetGameObject(string name)
    {
        for (int i = 0; i < buildings.Length; i++)
        {
            GameObject gameObject = gameObjects[i];
            if (gameObject && gameObject.name == name) return gameObjects[i];
        }
        return null;
    }

    public GameObject GetBuilding(string name)
    {
        for (int i = 0; i < buildings.Length; i++)
        {
            if (buildings[i] && buildings[i].name == name) return buildings[i].gameObject;
        }
        return null;
    }

    public GameObject GetUnit(string name)
    {
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] && units[i].name == name) return units[i].gameObject;
        }
        return null;
    }

    public GameObject GetWorldObject(string name)
    {
        foreach (WorldObjects worldObject in worldObjects)
        {
            if (worldObject.name == name) return worldObject.gameObject;
        }
        return null;
    }

    public GameObject GetPlayerObject()
    {
        return player;
    }

    public Texture2D GetBuildImage(string name)
    {
        for (int i = 0; i < buildings.Length; i++)
        {
            if (buildings[i] && buildings[i].name == name) return buildings[i].buildImage;
        }
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] && units[i].name == name) return units[i].buildImage;
        }
        return null;
    }
}
