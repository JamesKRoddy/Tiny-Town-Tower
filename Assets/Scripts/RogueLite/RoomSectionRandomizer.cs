using Unity.AI.Navigation;
using UnityEngine;

public class RoomSectionRandomizer : MonoBehaviour
{
    [Header("Section Parents")]
    public GameObject centre;
    public GameObject frontSection;
    public GameObject backSection;
    public GameObject leftSection;
    public GameObject rightSection;

    private NavMeshSurface navMeshSurface;

    public void GenerateRoom()
    {
        // Randomize each section
        RandomizeSection(frontSection);
        RandomizeSection(backSection);
        RandomizeSection(leftSection);
        RandomizeSection(rightSection);
        RandomizePropsInSection(centre);

        if (navMeshSurface == null)
        {
            navMeshSurface = FindAnyObjectByType<NavMeshSurface>();
        }

        BakeNavMesh();
    }

    void RandomizeSection(GameObject sectionParent)
    {
        if (sectionParent == null) return;

        // Get only direct children of the section parent
        Transform[] sections = GetDirectChildren(sectionParent);

        if (sections.Length == 0) return;

        // Disable all child sections
        foreach (Transform child in sections)
        {
            child.gameObject.SetActive(false);
        }

        // Enable one random child (section variant)
        int randomIndex = Random.Range(0, sections.Length);
        GameObject selectedSection = sections[randomIndex].gameObject;
        selectedSection.SetActive(true);

        // Randomize props within the active section
        RandomizePropsInSection(selectedSection);
    }

    void RandomizePropsInSection(GameObject section)
    {
        if (section == null) return;

        // Find all PropRandomizer components within the section
        PropRandomizer[] propRandomizers = section.GetComponentsInChildren<PropRandomizer>();

        foreach (PropRandomizer propRandomizer in propRandomizers)
        {
            propRandomizer.RandomizeProps();
        }
    }

    Transform[] GetDirectChildren(GameObject parent)
    {
        Transform[] directChildren = new Transform[parent.transform.childCount];
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            directChildren[i] = parent.transform.GetChild(i);
        }
        return directChildren;
    }

    public void BakeNavMesh()
    {
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
            Debug.Log("NavMesh baked at runtime.");
        }
        else
        {
            Debug.LogError("NavMeshSurface is not assigned.");
        }
    }
}
