using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RTS;
using System.IO;
using Newtonsoft.Json;

public class GameManager : MonoSingleton<GameManager>
{
    private bool initialised = false;
    public VictoryCondition[] victoryConditions;
    private HUD hud;
    public int[] territoriesOwner;
    public AudioClip[] soundTrack;
    public AudioSource musicPlayer;
    public LevelLoader loader;

    public static float generalVolume;
    public static float soundsVolume;
    public static float musicVolume;

    void Awake()
    {
        if (this != Instance) return;
        initialised = true;

        if (initialised)
        {
            StartCoroutine(LoadDetails());
        }
        loader = GameObject.FindObjectOfType<LevelLoader>();
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
            StartCoroutine(LoadDetails());
            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                musicPlayer.Stop();
            }
        }
    }

    private IEnumerator LoadDetails()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            Player[] players = null;
            while (players == null || players.Length < loader.playersNumber)
            {
                players = GameObject.FindObjectsOfType(typeof(Player)) as Player[];
                yield return null;
            }
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
        LoadVolumes();
    }

    void Update()
    {
        if (victoryConditions != null)
        {
            foreach (VictoryCondition victoryCondition in victoryConditions)
            {
                if (victoryCondition != null && victoryCondition.GameFinished())
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
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            if (!musicPlayer.isPlaying)
            {
                musicPlayer.clip = soundTrack[Random.Range(0, soundTrack.Length)];
                musicPlayer.Play();
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
        try
        {
            return territoriesOwner[id];
        } 
        catch
        {
            return -2;
        }
    }

    public int GetNumberOfTerritories()
    {
        return territoriesOwner.Length;
    }

    private void LoadVolumes()
    {
        char separator = Path.DirectorySeparatorChar;
        string path = "Settings" + separator + "settings.json";
        if (!File.Exists(path))
        {

            generalVolume = 0.5f;
            soundsVolume = 0.5f;
            musicVolume = 0.5f;
            return;
        }
        string input;
        using (StreamReader sr = new StreamReader(path))
        {
            input = sr.ReadToEnd();
        }
        if (input != null)
        {
            using (JsonTextReader reader = new JsonTextReader(new StringReader(input)))
            {
                string property = "";
                while (reader.Read())
                {
                    if (reader.Value != null)
                    {
                        if (reader.TokenType == JsonToken.PropertyName)
                        {
                            property = (string)reader.Value;
                        }
                        else
                        {
                            switch (property)
                            {
                                case "general": generalVolume = (float)(double)reader.Value; break;
                                case "sounds": soundsVolume = (float)(double)reader.Value; break;
                                case "music": musicVolume = (float)(double)reader.Value; break;
                                default: break;
                            }
                        }
                    }
                }
            }
        }
    }
}
