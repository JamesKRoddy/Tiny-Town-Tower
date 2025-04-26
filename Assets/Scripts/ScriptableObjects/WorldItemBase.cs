using UnityEngine;

// Base class for all items in the game
public class WorldItemBase : ScriptableObject
{
    [Header("Basic Item Information")]
    public string objectName; // Name of the resource
    [TextArea(3, 5)] // Allows for multi-line text in the Inspector
    public string description; // Description of the resource
    public Sprite sprite; // Sprite for the UI representation
}
