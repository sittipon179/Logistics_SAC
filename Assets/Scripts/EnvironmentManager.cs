namespace LogisticsSAC.Environment
{
    public class EnvironmentManager : UnityEngine.MonoBehaviour
    {
        public static EnvironmentManager Instance { get; private set; }

        [UnityEngine.Header("Information Delay Settings")]
        public int delayTimeSteps = 5;
        private int lastSyncStep = 0;

        [UnityEngine.Header("Nodes Tracking")]
        public System.Collections.Generic.List<TrashNode> allTrashNodes = new System.Collections.Generic.List<TrashNode>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                UnityEngine.Object.Destroy(this.gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (LogisticsSAC.Core.GameManager.Instance != null)
            {
                ProcessEnvironmentLogic();
            }
        }

        private void ProcessEnvironmentLogic()
        {
            int currentStep = LogisticsSAC.Core.GameManager.Instance.globalTimeStep;

            if (currentStep > lastSyncStep)
            {
                foreach (TrashNode node in allTrashNodes)
                {
                    node.GenerateTrash();
                }

                if (currentStep % delayTimeSteps == 0)
                {
                    SyncAllNodes();
                }

                lastSyncStep = currentStep;
            }
        }

        private void SyncAllNodes()
        {
            foreach (TrashNode node in allTrashNodes)
            {
                node.SyncObservedData();
            }
            UnityEngine.Debug.Log("Information Sync: Observed data updated at time step " + LogisticsSAC.Core.GameManager.Instance.globalTimeStep);
        }
    }
}