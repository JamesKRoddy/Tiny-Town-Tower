using System;
using System.Collections.Generic;
using UnityEngine;

public class GeneticMutationSystem : MonoBehaviour
{
    public static GeneticMutationSystem Instance { get; private set; }

    public event Action OnMutationChanged;

    public List<GeneticMutationObj> activeMutations = new List<GeneticMutationObj>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddMutation(GeneticMutationObj mutation)
    {
        if (!activeMutations.Contains(mutation))
        {
            activeMutations.Add(mutation);
            OnMutationChanged?.Invoke();
        }
    }

    public void RemoveMutation(GeneticMutationObj mutation)
    {
        if (activeMutations.Contains(mutation))
        {
            activeMutations.Remove(mutation);
            OnMutationChanged?.Invoke();
        }
    }
}
