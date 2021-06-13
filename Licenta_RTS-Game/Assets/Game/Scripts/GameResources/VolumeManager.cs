using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeManager : MonoBehaviour
{
    public AudioSource source;
    public int volumeType;
    public float volumeCap;
    void Start()
    {
        source = GetComponent<AudioSource>();
        volumeCap = source.volume;
    }

    // Update is called once per frame
    void Update()
    {
        source.volume = volumeCap * GameManager.generalVolume * (volumeType == 0 ? GameManager.soundsVolume : GameManager.musicVolume);
    }
}
