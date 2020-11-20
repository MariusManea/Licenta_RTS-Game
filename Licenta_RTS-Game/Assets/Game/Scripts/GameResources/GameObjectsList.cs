﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class GameObjectsList : MonoSingleton<GameObjectsList>
{
    public GameObject[] buildings;
    public GameObject[] units;
    public GameObject[] worldObjects;
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
            Building building = buildings[i].GetComponent<Building>();
            if (building && building.name == name) return buildings[i];
        }
        return null;
    }

    public GameObject GetUnit(string name)
    {
        for (int i = 0; i < units.Length; i++)
        {
            Unit unit = units[i].GetComponent<Unit>();
            if (unit && unit.name == name) return units[i];
        }
        return null;
    }

    public GameObject GetWorldObject(string name)
    {
        foreach (GameObject worldObject in worldObjects)
        {
            if (worldObject.name == name) return worldObject;
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
            Building building = buildings[i].GetComponent<Building>();
            if (building && building.name == name) return building.buildImage;
        }
        for (int i = 0; i < units.Length; i++)
        {
            Unit unit = units[i].GetComponent<Unit>();
            if (unit && unit.name == name) return unit.buildImage;
        }
        return null;
    }
}
