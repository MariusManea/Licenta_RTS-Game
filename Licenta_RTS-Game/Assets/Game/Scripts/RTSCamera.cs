﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTSCamera : MonoBehaviour
{
    public float sensitivity = 8f;
    public float zoomSpeed = 4f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float horInput = Input.GetAxis("Horizontal");
        float verInput = Input.GetAxis("Vertical");
        float zoomInput = Input.GetAxis("Mouse ScrollWheel");

        Vector3 move = new Vector3(horInput, 0f, verInput) * sensitivity * Time.deltaTime;

        transform.position += move;

        Vector3 zoom = new Vector3(0, 0, zoomInput) * zoomSpeed;
        transform.Translate(zoom, Space.Self);

    }
}
