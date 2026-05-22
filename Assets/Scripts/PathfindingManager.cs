using UnityEngine;
using System.Collections.Generic;

namespace LogisticsSAC.Pathfinding
{
    public class PathfindingManager : MonoBehaviour
    {
        public static PathfindingManager Instance { get; private set; }

        private Dictionary<(WaypointNode, WaypointNode), List<WaypointNode>> pathCache;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            pathCache = new Dictionary<(WaypointNode, WaypointNode), List<WaypointNode>>();
        }

        public List<WaypointNode> FindPath(WaypointNode startNode, WaypointNode targetNode)
        {
            if (startNode == null || targetNode == null) return null;

            var cacheKey = (startNode, targetNode);
            if (pathCache.ContainsKey(cacheKey))
            {
                return pathCache[cacheKey];
            }

            List<WaypointNode> openSetList = new List<WaypointNode>();
            HashSet<WaypointNode> openSetHash = new HashSet<WaypointNode>();
            HashSet<WaypointNode> closedSet = new HashSet<WaypointNode>();

            Dictionary<WaypointNode, WaypointNode> parentMap = new Dictionary<WaypointNode, WaypointNode>();
            Dictionary<WaypointNode, float> gCost = new Dictionary<WaypointNode, float>();
            Dictionary<WaypointNode, float> fCost = new Dictionary<WaypointNode, float>();

            openSetList.Add(startNode);
            openSetHash.Add(startNode);
            gCost[startNode] = 0f;
            fCost[startNode] = Vector2.Distance(startNode.transform.position, targetNode.transform.position);

            while (openSetList.Count > 0)
            {
                WaypointNode currentNode = openSetList[0];
                for (int i = 1; i < openSetList.Count; i++)
                {
                    if (fCost.ContainsKey(openSetList[i]) && fCost.ContainsKey(currentNode))
                    {
                        if (fCost[openSetList[i]] < fCost[currentNode] ||
                            (fCost[openSetList[i]] == fCost[currentNode] && gCost.ContainsKey(openSetList[i]) && gCost.ContainsKey(currentNode) && gCost[openSetList[i]] < gCost[currentNode]))
                        {
                            currentNode = openSetList[i];
                        }
                    }
                }

                openSetList.Remove(currentNode);
                openSetHash.Remove(currentNode);
                closedSet.Add(currentNode);

                if (currentNode == targetNode)
                {
                    List<WaypointNode> calculatedPath = RetracePath(startNode, targetNode, parentMap);
                    pathCache[cacheKey] = calculatedPath;
                    return calculatedPath;
                }

                if (currentNode.neighbors != null)
                {
                    foreach (WaypointNode neighbor in currentNode.neighbors)
                    {
                        if (neighbor == null || closedSet.Contains(neighbor)) continue;

                        float tentativeGCost = gCost[currentNode] + Vector2.Distance(currentNode.transform.position, neighbor.transform.position);

                        if (!gCost.ContainsKey(neighbor) || tentativeGCost < gCost[neighbor])
                        {
                            parentMap[neighbor] = currentNode;
                            gCost[neighbor] = tentativeGCost;
                            fCost[neighbor] = tentativeGCost + Vector2.Distance(neighbor.transform.position, targetNode.transform.position);

                            if (!openSetHash.Contains(neighbor))
                            {
                                openSetList.Add(neighbor);
                                openSetHash.Add(neighbor);
                            }
                        }
                    }
                }
            }

            pathCache[cacheKey] = null;
            return null;
        }

        private List<WaypointNode> RetracePath(WaypointNode startNode, WaypointNode endNode, Dictionary<WaypointNode, WaypointNode> parentMap)
        {
            List<WaypointNode> path = new List<WaypointNode>();
            WaypointNode currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                if (parentMap.ContainsKey(currentNode))
                {
                    currentNode = parentMap[currentNode];
                }
                else
                {
                    break;
                }
            }
            path.Reverse();
            return path;
        }
    }
}