using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;
using Newtonsoft.Json;
using System.IO;

public class SettingsMenu : MonoBehaviour
{
    public GUISkin mySkin;
    public AudioClip clickSound;
    public float clickVolume = 1.0f;
    public Texture2D header;

    private AudioElement audioElement;

    private float general, oldGeneral;
    private float sounds, oldSounds;
    private float music, oldMusic;
    void Start()
    {
        if (clickVolume < 0.0f) clickVolume = 0.0f;
        if (clickVolume > 1.0f) clickVolume = 1.0f;
        List<AudioClip> sounds = new List<AudioClip>();
        List<float> volumes = new List<float>();
        sounds.Add(clickSound);
        volumes.Add(clickVolume);
        audioElement = new AudioElement(sounds, volumes, "SaveMenu", null);
        LoadVolumes();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelSettings();
        }
        if (oldGeneral != general || oldSounds != sounds || oldMusic != music)
        {
            OnVolumeChange(general, sounds, music);
            oldGeneral = general;
            oldSounds = sounds;
            oldMusic = music;
        }
    }

    private void OnGUI()
    {
        GUI.skin = mySkin;
        DrawMenu();
    }

    private void DrawMenu()
    {
        float menuHeight = GetMenuHeight();
        float groupLeft = Screen.width / 2 - ResourceManager.MenuWidth / 2;
        float groupTop = Screen.height / 2 - menuHeight / 2;
        Rect groupRect = new Rect(groupLeft, groupTop, ResourceManager.MenuWidth, menuHeight);

        GUI.BeginGroup(groupRect);
        //background box
        GUI.Box(new Rect(0, 0, ResourceManager.MenuWidth, menuHeight), "");

        GUI.DrawTexture(new Rect(ResourceManager.MenuWidth / 2 - ResourceManager.HeaderWidth / 4, ResourceManager.Padding, ResourceManager.HeaderWidth / 2, ResourceManager.HeaderHeight / 5), header);
        float topPos = menuHeight - ResourceManager.Padding - ResourceManager.ButtonHeight;
        float leftPos = ResourceManager.MenuWidth / 2 - ResourceManager.ButtonWidth / 2;
        if (GUI.Button(new Rect(leftPos, topPos, ResourceManager.ButtonWidth, ResourceManager.ButtonHeight), "Cancel"))
        {
            PlayClick();
            CancelSettings();
        }

        leftPos = ResourceManager.MenuWidth / 2 - ResourceManager.SliderWidth / 2;
        topPos = ResourceManager.HeaderHeight / 5 + 2 * ResourceManager.Padding;

        GUI.Label(new Rect(leftPos, topPos, ResourceManager.ButtonWidth, ResourceManager.TextHeight), "General Volume:");
        topPos += ResourceManager.Padding;
        general = GUI.HorizontalSlider(new Rect(leftPos, topPos, ResourceManager.SliderWidth, ResourceManager.SliderHeight), general, 0.0f, 1.0f);

        topPos += ResourceManager.Padding;
        GUI.Label(new Rect(leftPos, topPos, ResourceManager.ButtonWidth, ResourceManager.TextHeight), "Sounds Volume:");
        topPos += ResourceManager.Padding;
        sounds = GUI.HorizontalSlider(new Rect(leftPos, topPos, ResourceManager.SliderWidth, ResourceManager.SliderHeight), sounds, 0.0f, 1.0f);

        topPos += ResourceManager.Padding;
        GUI.Label(new Rect(leftPos, topPos, ResourceManager.ButtonWidth, ResourceManager.TextHeight), "Music Volume:");
        topPos += ResourceManager.Padding;
        music = GUI.HorizontalSlider(new Rect(leftPos, topPos, ResourceManager.SliderWidth, ResourceManager.SliderHeight), music, 0.0f, 1.0f);

        GUI.EndGroup();

    }

    private void PlayClick()
    {
        if (audioElement != null) audioElement.Play(clickSound);
    }

    private float GetMenuHeight()
    {
        return 250 + GetMenuItemsHeight();
    }

    private float GetMenuItemsHeight()
    {
        return ResourceManager.ButtonHeight + ResourceManager.TextHeight + 3 * ResourceManager.Padding;
    }

    private void CancelSettings()
    {
        GetComponent<SettingsMenu>().enabled = false;
        PauseMenu pause = GetComponent<PauseMenu>();
        if (pause) pause.enabled = true;
    }

    private void OnVolumeChange(float generalVolume, float soundsVolume, float musicVolume)
    {
        JsonSerializer serializer = new JsonSerializer();
        serializer.NullValueHandling = NullValueHandling.Ignore;
        Directory.CreateDirectory("Settings");
        char separator = Path.DirectorySeparatorChar;
        string path = "Settings" + separator + "settings.json";
        using (StreamWriter sw = new StreamWriter(path))
        {
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                SaveManager.WriteFloat(writer, "general", generalVolume);
                SaveManager.WriteFloat(writer, "sounds", soundsVolume);
                SaveManager.WriteFloat(writer, "music", musicVolume);
                writer.WriteEndObject();
            }
        }
        GameManager.generalVolume = general;
        GameManager.soundsVolume = sounds;
        GameManager.musicVolume = music;
    }

    private void LoadVolumes()
    {
        oldGeneral = general = GameManager.generalVolume;
        oldSounds = sounds = GameManager.soundsVolume;
        oldMusic = music = GameManager.musicVolume;
    }
}
