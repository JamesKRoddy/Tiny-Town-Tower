using UnityEngine;

namespace Managers
{
    public class ResearchManager : MonoBehaviour
    {
        private int researchPoints = 0;

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