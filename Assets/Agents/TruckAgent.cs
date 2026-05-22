using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using LogisticsSAC.Pathfinding;
using LogisticsSAC.Environment;
using LogisticsSAC.Core;
using LogisticsSAC.Scripts;
using TMPro;

namespace LogisticsSAC.Agents
{
    public class TruckAgent : Agent
    {
        [Header("Logistics Parameters")]
        [SerializeField] private float _currentCapacity = 0f;
        public float currentCapacity
        {
            get => _currentCapacity;
            set { _currentCapacity = value; UpdateCapacityUI(); }
        }
        [SerializeField] private float maxCapacity = 300f;
        [SerializeField] private float moveSpeed = 80f;
        [SerializeField] private float rotationSpeed = 25f;

        [Header("Routing System")]
        [SerializeField] private WaypointNode startingNode;
        [SerializeField] private WaypointNode currentNode;

        [Header("Manual Testing (Heuristic Only)")]
        public WaypointNode manualTestTarget;

        [Header("Proportional Economics System")]
        [SerializeField] private float collectionRewardPerUnit = 0.02f;
        [SerializeField] private float depotDeliveryRewardPerUnit = 0.05f;
        [SerializeField] private float invalidPathPenalty = -0.05f;

        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI capacityText;

        private List<WaypointNode> currentPath;
        private int currentPathIndex = 0;
        private bool isMoving = false;
        private bool isIdling = false;
        private int idleTimer = 0;
        private WaypointNode currentTargetNode;
        private List<WaypointNode> allTargetableNodes;

        private float mapMinX = float.MaxValue, mapMaxX = float.MinValue;
        private float mapMinY = float.MaxValue, mapMaxY = float.MinValue;

        public override void Initialize()
        {
            this.MaxStep = 0;
            UpdateCapacityUI();
        }

        private void EnsureNodesInitialized()
        {
            if (allTargetableNodes != null && allTargetableNodes.Count > 0) return;

            allTargetableNodes = new List<WaypointNode>();

            if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.allWaypointNodes != null)
            {
                allTargetableNodes.AddRange(EnvironmentManager.Instance.allWaypointNodes);
            }

            Depot[] depots = Object.FindObjectsByType<Depot>(FindObjectsSortMode.None);
            List<WaypointNode> depotNodes = new List<WaypointNode>();
            foreach (var depot in depots)
            {
                WaypointNode depotWp = depot.GetComponent<WaypointNode>();
                if (depotWp != null) depotNodes.Add(depotWp);
            }
            depotNodes.Sort((a, b) => a.gameObject.name.CompareTo(b.gameObject.name));
            allTargetableNodes.AddRange(depotNodes);

            CalculateMapBoundaries();
        }

        private void CalculateMapBoundaries()
        {
            if (allTargetableNodes == null || allTargetableNodes.Count == 0) return;
            foreach (var node in allTargetableNodes)
            {
                Vector3 pos = node.transform.position;
                if (pos.x < mapMinX) mapMinX = pos.x;
                if (pos.x > mapMaxX) mapMaxX = pos.x;
                if (pos.y < mapMinY) mapMinY = pos.y;
                if (pos.y > mapMaxY) mapMaxY = pos.y;
            }
        }

        public override void OnEpisodeBegin()
        {
            EnsureNodesInitialized();

            currentCapacity = 0f;
            isMoving = false;
            isIdling = false;
            idleTimer = 0;
            currentPath = null;
            currentTargetNode = null;

            if (startingNode != null)
            {
                currentNode = startingNode;
                this.transform.position = currentNode.transform.position;
            }
            this.RequestDecision();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            EnsureNodesInitialized();

            sensor.AddObservation((this.transform.position.x - mapMinX) / (mapMaxX - mapMinX));
            sensor.AddObservation((this.transform.position.y - mapMinY) / (mapMaxY - mapMinY));
            sensor.AddObservation(_currentCapacity / maxCapacity);

            if (allTargetableNodes != null)
            {
                foreach (var node in allTargetableNodes)
                {
                    bool isDepot = node.GetComponent<Depot>() != null;
                    sensor.AddObservation((node.transform.position.x - mapMinX) / (mapMaxX - mapMinX));
                    sensor.AddObservation((node.transform.position.y - mapMinY) / (mapMaxY - mapMinY));

                    if (!isDepot)
                    {
                        TrashNode tNode = node.GetComponentInChildren<TrashNode>(true);
                        sensor.AddObservation(tNode != null && tNode.gameObject.activeInHierarchy ? tNode.observedTrash / tNode.maxTrashCapacity : 0f);
                        sensor.AddObservation(tNode != null && tNode.gameObject.activeInHierarchy ? tNode.GetCurrentGenerateRate() : 0f);
                    }
                    else
                    {
                        sensor.AddObservation(0f);
                        sensor.AddObservation(0f);
                    }
                }
            }

            float timeRatio = GameManager.Instance != null ? (float)GameManager.Instance.globalTimeStep / GameManager.Instance.maxTimeSteps : 0f;
            sensor.AddObservation(timeRatio);
        }

        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            EnsureNodesInitialized();
            if (allTargetableNodes == null) return;

            bool isFull = currentCapacity >= (maxCapacity - 0.1f);

            for (int i = 0; i < allTargetableNodes.Count; i++)
            {
                if (allTargetableNodes[i] == null) continue;

                TrashNode tNode = allTargetableNodes[i].GetComponentInChildren<TrashNode>(true);
                if (tNode != null && !tNode.gameObject.activeInHierarchy)
                {
                    actionMask.SetActionEnabled(0, i, false);
                    continue;
                }

                bool isDepot = allTargetableNodes[i].GetComponent<Depot>() != null;

                if (allTargetableNodes[i] == currentNode)
                {
                    actionMask.SetActionEnabled(0, i, false);
                    continue;
                }

                if (!isDepot && isFull)
                {
                    actionMask.SetActionEnabled(0, i, false);
                }
            }
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            EnsureNodesInitialized();
            if (isMoving || isIdling || allTargetableNodes == null) return;

            int targetIndex = actions.DiscreteActions[0];
            if (targetIndex >= 0 && targetIndex < allTargetableNodes.Count)
            {
                currentTargetNode = allTargetableNodes[targetIndex];
                currentPath = PathfindingManager.Instance.FindPath(currentNode, currentTargetNode);

                if (currentPath != null && currentPath.Count > 0)
                {
                    currentPathIndex = 0;
                    isMoving = true;
                }
                else
                {
                    ApplyReward(invalidPathPenalty);
                    isIdling = true;
                    idleTimer = 10;
                }
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            EnsureNodesInitialized();
            if (allTargetableNodes == null || allTargetableNodes.Count == 0) return;

            var discreteActionsOut = actionsOut.DiscreteActions;

            if (manualTestTarget != null)
            {
                int targetIndex = allTargetableNodes.IndexOf(manualTestTarget);
                if (targetIndex != -1 && allTargetableNodes[targetIndex] != currentNode)
                {
                    discreteActionsOut[0] = targetIndex;
                    return;
                }
            }

            List<int> validRandomIndexes = new List<int>();
            for (int i = 0; i < allTargetableNodes.Count; i++)
            {
                if (allTargetableNodes[i] != currentNode)
                {
                    validRandomIndexes.Add(i);
                }
            }

            if (validRandomIndexes.Count > 0)
            {
                int randomSelection = Random.Range(0, validRandomIndexes.Count);
                discreteActionsOut[0] = validRandomIndexes[randomSelection];
            }
            else
            {
                discreteActionsOut[0] = 0;
            }
        }

        private void FixedUpdate()
        {
            if (isMoving)
            {
                MoveAlongPath();
            }
            else if (isIdling)
            {
                idleTimer--;
                if (idleTimer <= 0)
                {
                    isIdling = false;
                    this.RequestDecision();
                }
            }
        }

        private void MoveAlongPath()
        {
            WaypointNode nextWaypoint = currentPath[currentPathIndex];
            Vector2 direction = (Vector2)nextWaypoint.transform.position - (Vector2)this.transform.position;

            if (direction != Vector2.zero)
            {
                float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, targetAngle - 90f), rotationSpeed * Time.fixedDeltaTime);
            }

            transform.position = Vector2.MoveTowards(transform.position, nextWaypoint.transform.position, moveSpeed * Time.fixedDeltaTime);

            if (Vector2.Distance(transform.position, nextWaypoint.transform.position) < 0.05f)
            {
                currentNode = nextWaypoint;
                currentPathIndex++;
                if (currentPathIndex >= currentPath.Count)
                {
                    isMoving = false;
                    currentPath = null;
                    ProcessNodeArrival();
                }
            }
        }

        private void ProcessNodeArrival()
        {
            if (currentNode == null) return;
            TrashNode trashNode = currentNode.GetComponentInChildren<TrashNode>();
            Depot depot = currentNode.GetComponent<Depot>();

            if (trashNode != null && currentCapacity < maxCapacity)
            {
                float collected = trashNode.CollectTrash(maxCapacity - currentCapacity);

                if (collected > 0f)
                {
                    currentCapacity += collected;
                    float reward = collected * collectionRewardPerUnit;
                    ApplyReward(reward);
                    if (GameManager.Instance != null) GameManager.Instance.TrackCollection(collected);
                }

                if (collected < 10f)
                {
                    if (GameManager.Instance != null) GameManager.Instance.TrackWastedTrip();
                }
            }
            else if (depot != null && currentCapacity > 0f)
            {
                float reward = currentCapacity * depotDeliveryRewardPerUnit;
                ApplyReward(reward);

                if (GameManager.Instance != null) GameManager.Instance.TrackDepotDelivery(currentCapacity / maxCapacity);

                currentCapacity = 0f;
            }

            this.RequestDecision();
        }

        private void ApplyReward(float amount)
        {
            AddReward(amount);
            if (GameManager.Instance != null) GameManager.Instance.TrackAgentReward(amount);
        }

        private void UpdateCapacityUI()
        {
            if (capacityText != null && !Application.isBatchMode)
            {
                capacityText.text = $"{Mathf.RoundToInt(_currentCapacity)}/{Mathf.RoundToInt(maxCapacity)}";
            }
        }
    }
}