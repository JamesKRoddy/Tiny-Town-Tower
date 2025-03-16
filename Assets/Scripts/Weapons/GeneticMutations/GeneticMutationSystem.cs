using System;
using System.Collections.Generic;
using UnityEngine;

public class GeneticMutationSystem : MonoBehaviour
{
    public static GeneticMutationSystem Instance { get; private set; }

    public event Action OnMutationChanged;

    public List<GeneticMutationData> activeMutations = new List<GeneticMutationData>();
    public List<GeneticMutationData> contaminatedMutations = new List<GeneticMutationData>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddMutation(GeneticMutationData mutation)
    {
        if (!activeMutations.Contains(mutation))
        {
            activeMutations.Add(mutation);
            if (mutation.isContaminated)
            {
                contaminatedMutations.Add(mutation);
            }
            OnMutationChanged?.Invoke();
        }
    }

    public void RemoveMutation(GeneticMutationData mutation)
    {
        if (activeMutations.Contains(mutation))
        {
            activeMutations.Remove(mutation);
            contaminatedMutations.Remove(mutation);
            OnMutationChanged?.Invoke();
        }
    }

    public void PurifyMutation(GeneticMutationData mutation)
    {
        if (contaminatedMutations.Contains(mutation))
        {
            mutation.isContaminated = false;
            contaminatedMutations.Remove(mutation);
            OnMutationChanged?.Invoke();
        }
    }

    public bool HasContaminatedMutations()
    {
        return contaminatedMutations.Count > 0;
    }
}
