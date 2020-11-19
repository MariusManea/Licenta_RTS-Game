using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RTS;

public class LevelLoader : MonoBehaviour
{
    private static int nextObjectId = 0;
    private static bool created = false;
    private bool initialised = false;

    public int playersNumber;
    public Color[] teamColors;

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
                if (SceneManager.GetActiveScene().name == "GameScene")
                {
                    InitNewWorld();
                }

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

    private void InitNewWorld()
    {
        // new level init
        GameObject humanPlayer = (GameObject)GameObject.Instantiate(ResourceManager.GetPlayerObject());
        Player player = humanPlayer.GetComponent<Player>();
        player.isHuman = true;
        player.teamColor = teamColors[0];
        player.userName = player.name = PlayerManager.GetPlayerName();
        Buildings buildings = player.GetComponentInChildren<Buildings>();
        GameObject townCenter = (GameObject)Instantiate(ResourceManager.GetBuilding("TownCenter"), player.transform.position, new Quaternion());
        player.townCenter = townCenter.GetComponent<TownCenter>();
        player.townCenter.ObjectId = ResourceManager.GetNewObjectId();
        if (buildings) townCenter.transform.parent = buildings.transform;
        player.townCenter.SetPlayer();
        player.townCenter.SetTeamColor();
        player.townCenter.SetPlayingArea(player.GetComponentInChildren<HUD>().GetPlayingArea());
        player.townCenter.CalculateBounds();
        player.townCenter.SetSpawnPoint();
        player.townCenter.FirstUnits(1);

        for (int k = 1; k < playersNumber; ++k)
        {
            GameObject computerPlayer = (GameObject)GameObject.Instantiate(ResourceManager.GetPlayerObject(), humanPlayer.transform.position + new Vector3(k * 10, 0, k * 10), new Quaternion());
            Player cPlayer = computerPlayer.GetComponent<Player>();
            cPlayer.isHuman = false;
            cPlayer.teamColor = teamColors[k];
            cPlayer.userName = cPlayer.name = PlayerManager.GetComputerNames()[k - 1];
            Buildings cBuildings = cPlayer.GetComponentInChildren<Buildings>();
            GameObject cTownCenter = (GameObject)Instantiate(ResourceManager.GetBuilding("TownCenter"), cPlayer.transform.position, new Quaternion());
            cPlayer.townCenter = cTownCenter.GetComponent<TownCenter>();
            cPlayer.townCenter.ObjectId = ResourceManager.GetNewObjectId();
            if (cBuildings) cTownCenter.transform.parent = cBuildings.transform;
            cPlayer.townCenter.SetPlayer();
            cPlayer.townCenter.SetTeamColor();
            cPlayer.townCenter.SetPlayingArea(player.GetComponentInChildren<HUD>().GetPlayingArea());
            cPlayer.townCenter.CalculateBounds();
            cPlayer.townCenter.SetSpawnPoint();
            cPlayer.townCenter.FirstUnits(1);
        }
    }
}
