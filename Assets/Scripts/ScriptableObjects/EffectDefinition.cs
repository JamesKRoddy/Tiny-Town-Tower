using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Effect Definition", menuName = "Scriptable Objects/Effects/Effect Definition")]
public class EffectDefinition : ScriptableObject
{
    public enum PlayMode
    {
        Random,
        All
    }

    [Tooltip("Array of possible particle system prefabs. One will be randomly selected")]
    public GameObject[] prefabs;
    
    [Tooltip("Array of possible sound effects to play. One will be randomly selected")]
    public AudioClip[] sounds;
    
    [Tooltip("Minimum pitch variation for the sound effect (0.9 = 10% lower)")]
    public float minPitch = 0.9f;
    
    [Tooltip("Maximum pitch variation for the sound effect (1.1 = 10% higher)")]
    public float maxPitch = 1.1f;
    
    [Tooltip("Volume level for the sound effect (0-1)")]
    public float volume = 1f;

    [Tooltip("Duration of the effect in seconds. If 0, uses the particle system duration")]
    public float duration = 0f;
    
    [Tooltip("Should this effect loop indefinitely? Useful for persistent status effects like sleeping, working, etc.")]
    public bool looping = false;
    
    [Tooltip("Loop interval in seconds. How long to wait between loop cycles (only used if looping is true)")]
    public float loopInterval = 5f;

    [Tooltip("Controls how much the sound is affected by 3D positioning (0 = 2D, 1 = 3D)")]
    [Range(0f, 1f)]
    public float spatialBlend = 1f;

    [Tooltip("Whether to play a random effect or all effects")]
    public PlayMode playMode = PlayMode.Random;
}
