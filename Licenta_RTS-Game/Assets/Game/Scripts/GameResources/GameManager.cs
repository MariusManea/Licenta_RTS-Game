﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class GameManager : MonoSingleton<GameManager>
{
    private bool initialised = false;
    private VictoryCondition[] victoryConditions;
    private HUD hud;

    void Awake()
    {
        if (this != Instance) return;
        initialised = true;

        if (initialised)
        {
            LoadDetails();
        }
    }

    void OnLevelWasLoaded()
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
}