using UnityEngine;

public abstract class WorkTask : MonoBehaviour
{
    public WorkType workType;

    // Abstract method for NPC to perform the work task
    public abstract void PerformTask(SettlerNPC npc);
}
