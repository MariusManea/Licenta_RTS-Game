using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundRecording : MonoBehaviour
{
    public int direction = -1;
    public float rotationSpeed = 5f;
    public float leftLimit = 290, rightLimit = 60;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.eulerAngles.y < 180 && transform.eulerAngles.y > rightLimit) direction = -1;
        if (transform.eulerAngles.y > 180 && transform.eulerAngles.y < leftLimit) direction = 1;
        Debug.Log(transform.eulerAngles.y);
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + direction * rotationSpeed * Time.deltaTime, transform.eulerAngles.z);
    }
}
