using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] public AudioSource AudioSource;

    [SerializeField] private AudioClip Merge;
    [SerializeField] private AudioClip Move;

    public void PlayMerge()
    {
        AudioSource.PlayOneShot(Merge);
    }

    public void PlayMove()
    {
        AudioSource.PlayOneShot(Move);
    }
}