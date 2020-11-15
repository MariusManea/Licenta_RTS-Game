using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string userName;
    public bool isHuman;

    public HUD hud;

    public WorldObjects SelectedObject { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        hud = GetComponentInChildren<HUD>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
