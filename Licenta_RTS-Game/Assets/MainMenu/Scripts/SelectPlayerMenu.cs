﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class SelectPlayerMenu : MonoBehaviour
{
    public GUISkin mySkin;

    private string playerName = "NewPlayer";

    public Texture2D[] avatars;
    private int avatarIndex = -1;

    private void Start()
    {
        Cursor.visible = true;
        PlayerManager.SetAvatarTextures(avatars);
        if (avatars.Length > 0) avatarIndex = 0;
    }

    void OnGUI()
    {

        GUI.skin = mySkin;

        float menuHeight = GetMenuHeight();
        float groupLeft = Screen.width / 2 - ResourceManager.MenuWidth / 2;
        float groupTop = Screen.height / 2 - menuHeight / 2;
        Rect groupRect = new Rect(groupLeft, groupTop, ResourceManager.MenuWidth, menuHeight);

        GUI.BeginGroup(groupRect);
        //background box
        GUI.Box(new Rect(0, 0, ResourceManager.MenuWidth, menuHeight), "");
        //menu buttons
        float leftPos = ResourceManager.MenuWidth / 2 - ResourceManager.ButtonWidth / 2;
        float topPos = menuHeight - ResourceManager.Padding - ResourceManager.ButtonHeight;
        if (GUI.Button(new Rect(leftPos, topPos, ResourceManager.ButtonWidth, ResourceManager.ButtonHeight), "Select"))
        {
            SelectPlayer();
        }
        //text area for player to type new name
        float textTop = menuHeight - 2 * ResourceManager.Padding - ResourceManager.ButtonHeight - ResourceManager.TextHeight;
        float textWidth = ResourceManager.MenuWidth - 2 * ResourceManager.Padding;
        playerName = GUI.TextField(new Rect(ResourceManager.Padding, textTop, textWidth, ResourceManager.TextHeight), playerName, 14);

        if (avatarIndex >= 0)
        {
            float avatarLeft = ResourceManager.MenuWidth / 2 - avatars[avatarIndex].width / 2;
            float avatarTop = textTop - ResourceManager.Padding - avatars[avatarIndex].height;
            float avatarWidth = avatars[avatarIndex].width;
            float avatarHeight = avatars[avatarIndex].height;
            GUI.DrawTexture(new Rect(avatarLeft, avatarTop, avatarWidth, avatarHeight), avatars[avatarIndex]);
            float buttonTop = textTop - ResourceManager.Padding - ResourceManager.ButtonHeight;
            float buttonLeft = ResourceManager.Padding;
            if (GUI.Button(new Rect(buttonLeft, buttonTop, ResourceManager.ButtonHeight, ResourceManager.ButtonHeight), "<"))
            {
                avatarIndex -= 1;
                if (avatarIndex < 0) avatarIndex = avatars.Length - 1;
            }
            buttonLeft = ResourceManager.MenuWidth - ResourceManager.Padding - ResourceManager.ButtonHeight;
            if (GUI.Button(new Rect(buttonLeft, buttonTop, ResourceManager.ButtonHeight, ResourceManager.ButtonHeight), ">"))
            {
                avatarIndex = (avatarIndex + 1) % avatars.Length;
            }
        }

        GUI.EndGroup();
    }

    private float GetMenuHeight()
    {
        float avatarHeight = 0;
        if (avatars.Length > 0) avatarHeight = avatars[0].height + 2 * ResourceManager.Padding;
        return avatarHeight + ResourceManager.ButtonHeight + ResourceManager.TextHeight + 3 * ResourceManager.Padding;
    }

    private void SelectPlayer()
    {
        PlayerManager.SelectPlayer(playerName, avatarIndex);
        GetComponent<SelectPlayerMenu>().enabled = false;
        MainMenu main = GetComponent<MainMenu>();
        if (main) main.enabled = true;
    }
}
