using System.Collections.Generic;

[System.Serializable]
public class DialogueData
{
    public string npcName; // NPC's name
    public List<DialogueLine> lines; // List of dialogue lines
}

[System.Serializable]
public class DialogueLine
{
    public string speaker; // Name of the speaker
    public string text; // Text of the dialogue
    public string id; //Used when narrative just moves to the next line WITH an option
    public string nextLine; //Used when narrative just moves to the next line WITHOUT an option
    public List<DialogueOption> options; // Available player options
    public bool isTerminal; //Ends the conversation
}

[System.Serializable]
public class DialogueOption
{
    public string text; // Text for the player option
    public string nextLine; // The ID of the next line to jump to
    public string requiredItem; // (Optional) Item required to enable this option
}
