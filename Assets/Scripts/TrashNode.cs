namespace LogisticsSAC.Environment
{
    public class TrashNode : UnityEngine.MonoBehaviour
    {
        public string nodeID = "Node_01";

        [UnityEngine.Header("Trash Data (POMDP Variables)")]
        public float actualTrashVolume = 0f;
        public float observedTrashVolume = 0f;

        [UnityEngine.Header("Node Parameters")]
        public float trashGenerationRate = 1f;
        public float maxCapacity = 100f;

        public void GenerateTrash()
        {
            actualTrashVolume += trashGenerationRate;

            if (actualTrashVolume >= maxCapacity)
            {
                LogisticsSAC.Core.GameManager.Instance.TriggerOverflowPenalty(nodeID);
            }
        }

        public void SyncObservedData()
        {
            observedTrashVolume = actualTrashVolume;
        }

        public float CollectTrash(float truckAvailableCapacity)
        {
            float collectedAmount = UnityEngine.Mathf.Min(actualTrashVolume, truckAvailableCapacity);
            actualTrashVolume -= collectedAmount;
            SyncObservedData();
            return collectedAmount;
        }
    }
}