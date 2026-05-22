namespace LogisticsSAC.Pathfinding
{
    public class WaypointNode : UnityEngine.MonoBehaviour
    {
        [UnityEngine.Header("Connected Nodes")]
        public System.Collections.Generic.List<WaypointNode> neighbors = new System.Collections.Generic.List<WaypointNode>();

        private void OnDrawGizmos()
        {
            UnityEngine.Gizmos.color = UnityEngine.Color.green;
            UnityEngine.Gizmos.DrawSphere(this.transform.position, 0.3f);

            if (neighbors != null && neighbors.Count > 0)
            {
                foreach (WaypointNode node in neighbors)
                {
                    if (node != null)
                    {
                        UnityEngine.Gizmos.color = UnityEngine.Color.cyan;
                        UnityEngine.Gizmos.DrawLine(this.transform.position, node.transform.position);
                    }
                }
            }
        }
    }
}