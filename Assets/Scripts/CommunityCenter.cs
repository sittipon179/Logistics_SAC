using UnityEngine;
using System.Collections.Generic;
using LogisticsSAC.Environment;

namespace LogisticsSAC.Core
{
    public class CommunityCenter : MonoBehaviour
    {
        [SerializeField] private EnvironmentManager envManager;
        [SerializeField] private int delaySteps = 250;
        [SerializeField] private int updateInterval = 250;

        private Queue<float[]> observationQueue;
        private Queue<float[]> arrayPool;
        private float[] currentDelayedObservation;
        private int currentTotalNodes = 0;

        private void Awake()
        {
            observationQueue = new Queue<float[]>();
            arrayPool = new Queue<float[]>();
        }

        private void FixedUpdate()
        {
            if (envManager == null || envManager.allWaypointNodes == null || envManager.allWaypointNodes.Count == 0) return;

            if (currentTotalNodes != envManager.allWaypointNodes.Count)
            {
                currentTotalNodes = envManager.allWaypointNodes.Count;
                currentDelayedObservation = new float[currentTotalNodes];
                ResetCenter();
            }

            float[] realTrashLevels;
            if (arrayPool.Count > 0)
            {
                realTrashLevels = arrayPool.Dequeue();
            }
            else
            {
                realTrashLevels = new float[currentTotalNodes];
            }

            PopulateRealTrashLevelsFromEnvironment(realTrashLevels);
            observationQueue.Enqueue(realTrashLevels);

            if (observationQueue.Count > delaySteps)
            {
                float[] delayedData = observationQueue.Dequeue();

                if (GameManager.Instance.globalTimeStep % updateInterval == 0 || updateInterval == 1)
                {
                    UpdateNodeObservations(delayedData);
                }

                arrayPool.Enqueue(delayedData);
            }
        }

        private void UpdateNodeObservations(float[] data)
        {
            data.CopyTo(currentDelayedObservation, 0);

            for (int i = 0; i < currentDelayedObservation.Length; i++)
            {
                if (i < envManager.allWaypointNodes.Count)
                {
                    TrashNode node = envManager.cachedTrashNodes[i];
                    if (node != null) node.UpdateDelayedObservation(currentDelayedObservation[i]);
                }
            }
        }

        public void ResetCenter()
        {
            observationQueue.Clear();
            arrayPool.Clear();
            if (currentDelayedObservation != null)
            {
                for (int i = 0; i < currentDelayedObservation.Length; i++)
                {
                    currentDelayedObservation[i] = 0f;
                }
            }
        }

        public void SetDelayParameters(int newDelay, int newInterval)
        {
            delaySteps = newDelay;
            updateInterval = Mathf.Max(1, newInterval);
            ResetCenter();
        }

        public float[] GetDelayedTrashObservations()
        {
            return currentDelayedObservation;
        }

        public int GetCurrentDelaySteps()
        {
            return delaySteps;
        }

        private void PopulateRealTrashLevelsFromEnvironment(float[] levels)
        {
            for (int i = 0; i < currentTotalNodes; i++)
            {
                levels[i] = envManager.GetTrashNodeLevelFast(i);
            }
        }
    }
}