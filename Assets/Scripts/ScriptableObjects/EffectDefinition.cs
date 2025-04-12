using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewEffect", menuName = "Scriptable Objects/Roguelite/Enemies/Effects/Effect Definition")]
public class EffectDefinition : ScriptableObject
{
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
}
