using UnityEngine;
using System.Collections.Generic;
using LogisticsSAC.Pathfinding;

namespace LogisticsSAC.Environment
{
    public class EnvironmentManager : MonoBehaviour
    {
        public static EnvironmentManager Instance { get; private set; }

        [Header("Spawning System")]
        public GameObject trashNodePrefab;
        public Transform spawnPointsParent;
        public bool useRandomSpawning = true;

        [Header("Nodes Tracking")]
        public List<WaypointNode> allWaypointNodes = new List<WaypointNode>();
        public List<TrashNode> activeGenerators = new List<TrashNode>();

        public TrashNode[] cachedTrashNodes;

        private int lastStep = 0;
        private bool isInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            InitializeAllFixedNodes();
        }

        private void FixedUpdate()
        {
            if (Core.GameManager.Instance != null && isInitialized)
            {
                ProcessEnvironmentLogic();
            }
        }

        private void InitializeAllFixedNodes()
        {
            if (spawnPointsParent == null) return;

            int childCount = spawnPointsParent.childCount;
            cachedTrashNodes = new TrashNode[childCount];

            int index = 0;
            foreach (Transform child in spawnPointsParent)
            {
                WaypointNode wp = child.GetComponent<WaypointNode>();
                if (wp != null)
                {
                    allWaypointNodes.Add(wp);

                    TrashNode existingNode = wp.GetComponentInChildren<TrashNode>(true);
                    if (existingNode != null)
                    {
                        existingNode.nodeID = "Node_" + activeGenerators.Count.ToString();
                        activeGenerators.Add(existingNode);
                        cachedTrashNodes[index] = existingNode;
                    }
                    else if (trashNodePrefab != null)
                    {
                        GameObject newObj = Instantiate(trashNodePrefab, wp.transform.position, Quaternion.identity, wp.transform);
                        TrashNode newNode = newObj.GetComponent<TrashNode>();
                        if (newNode != null)
                        {
                            newNode.nodeID = "Node_" + activeGenerators.Count.ToString();
                            activeGenerators.Add(newNode);
                            cachedTrashNodes[index] = newNode;
                        }
                    }
                }
                index++;
            }
            isInitialized = true;
        }

        public void ResetEnvironmentNodes()
        {
            if (Core.GameManager.Instance == null) return;

            int activeCount = Core.GameManager.Instance.currentActiveNodes;

            List<int> indices = new List<int>();
            for (int i = 0; i < activeGenerators.Count; i++)
            {
                indices.Add(i);
            }

            if (useRandomSpawning)
            {
                for (int i = 0; i < indices.Count; i++)
                {
                    int temp = indices[i];
                    int randomIndex = Random.Range(i, indices.Count);
                    indices[i] = indices[randomIndex];
                    indices[randomIndex] = temp;
                }
            }

            HashSet<int> selectedIndices = new HashSet<int>();
            for (int i = 0; i < activeCount && i < indices.Count; i++)
            {
                selectedIndices.Add(indices[i]);
            }

            for (int i = 0; i < activeGenerators.Count; i++)
            {
                if (activeGenerators[i] != null)
                {
                    if (selectedIndices.Contains(i))
                    {
                        activeGenerators[i].gameObject.SetActive(true);
                        activeGenerators[i].ResetGenerator();
                        activeGenerators[i].InitializeStartingTrash();
                    }
                    else
                    {
                        activeGenerators[i].ResetGenerator();
                        activeGenerators[i].actualTrash = 0f;
                        activeGenerators[i].observedTrash = 0f;
                        activeGenerators[i].gameObject.SetActive(false);
                    }
                }
            }
            lastStep = Core.GameManager.Instance.globalTimeStep;
        }

        private void ProcessEnvironmentLogic()
        {
            int currentStep = Core.GameManager.Instance.globalTimeStep;

            if (currentStep > lastStep)
            {
                for (int i = 0; i < activeGenerators.Count; i++)
                {
                    if (activeGenerators[i] != null && activeGenerators[i].gameObject.activeInHierarchy)
                    {
                        activeGenerators[i].GenerateTrash();
                    }
                }
                lastStep = currentStep;
            }
        }

        public float GetTrashNodeLevelFast(int index)
        {
            if (cachedTrashNodes == null || index < 0 || index >= cachedTrashNodes.Length) return 0f;

            TrashNode tNode = cachedTrashNodes[index];
            return tNode != null && tNode.gameObject.activeInHierarchy ? tNode.actualTrash : 0f;
        }

        public int GetOverflowingNodesCount()
        {
            int count = 0;

            for (int i = 0; i < activeGenerators.Count; i++)
            {
                if (activeGenerators[i] != null && activeGenerators[i].gameObject.activeInHierarchy)
                {
                    if (activeGenerators[i].IsNodeOverflowing()) count++;
                }
            }
            return count;
        }
    }
}