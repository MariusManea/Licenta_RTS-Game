using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RTS;

public class GameManager : MonoSingleton<GameManager>
{
    private bool initialised = false;
    private VictoryCondition[] victoryConditions;
    private HUD hud;
    public int[] territoriesOwner;


    void Awake()
    {
        if (this != Instance) return;
        initialised = true;

        if (initialised)
        {
            LoadDetails();
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += LevelLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= LevelLoaded;
    }

    void LevelLoaded(Scene scene, LoadSceneMode mode)
    {
        if (initialised)
        {
            LoadDetails();
        }
    }

    private void LoadDetails()
    {
        Player[] players = GameObject.FindObjectsOfType(typeof(Player)) as Player[];
        foreach (Player player in players)
        {
            if (player.isHuman) hud = player.GetComponentInChildren<HUD>();
        }
        victoryConditions = GameObject.FindObjectsOfType(typeof(VictoryCondition)) as VictoryCondition[];
        if (victoryConditions != null)
        {
            foreach (VictoryCondition victoryCondition in victoryConditions)
            {
                victoryCondition.SetPlayers(players);
            }
        }
    }

    void Update()
    {
        if (victoryConditions != null)
        {
            foreach (VictoryCondition victoryCondition in victoryConditions)
            {
                if (victoryCondition.GameFinished())
                {
                    ResultsScreen resultsScreen = hud.GetComponent<ResultsScreen>();
                    resultsScreen.SetMetVictoryCondition(victoryCondition);
                    resultsScreen.enabled = true;
                    Time.timeScale = 0.0f;
                    Cursor.visible = true;
                    ResourceManager.MenuOpen = true;
                    hud.enabled = false;
                }
            }
        }
    }

    public void InitOwnership(int size, int playersNumber)
    {
        territoriesOwner = new int[size];
        for (int i = 0; i < size; ++i)
        {
            territoriesOwner[i] = i < playersNumber ? i : -1;
        }
    }

    public void LoadOwnership(int[] newOwnership)
    {
        territoriesOwner = newOwnership;
    }

    public void SetOwner(int id, int owner)
    {
        territoriesOwner[id] = owner;
    }

    public int GetOwner(int id)
    {
        return territoriesOwner[id];
    }

    public int GetNumberOfTerritories()
    {
        return territoriesOwner.Length;
    }
}
