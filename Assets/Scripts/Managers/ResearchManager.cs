using UnityEngine;

namespace Managers
{
    public class ResearchManager : MonoBehaviour
    {
        private static ResearchManager _instance;
        public static ResearchManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ResearchManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("ResearchManager instance not found in the scene!");
                    }
                }
                return _instance;
            }
        }

        private int researchPoints = 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
        }

        public void AddResearchPoints(int points)
        {
            researchPoints += points;
            Debug.Log($"Total research points: {researchPoints}");
        }

        public bool SpendResearchPoints(int points)
        {
            if (researchPoints >= points)
            {
                researchPoints -= points;
                return true;
            }
            return false;
        }

        public int GetResearchPoints()
        {
            return researchPoints;
        }
    } 
}