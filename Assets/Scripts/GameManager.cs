namespace LogisticsSAC.Core
{
    public class GameManager : UnityEngine.MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [UnityEngine.Header("Simulation Time Parameters")]
        public int globalTimeStep = 0;
        public float timeStepInterval = 1f;
        private float timer = 0f;

        [UnityEngine.Header("Score & Reward System")]
        public int totalPenaltyScore = 0;
        public int timePenaltyValue = -1;
        public int heavyPenaltyValue = -100;

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
            ProcessGlobalTime();
        }

        private void ProcessGlobalTime()
        {
            timer += UnityEngine.Time.deltaTime;

            if (timer >= timeStepInterval)
            {
                timer -= timeStepInterval;
                globalTimeStep++;
                ApplyTimeStepPenalty();
            }
        }

        private void ApplyTimeStepPenalty()
        {
            totalPenaltyScore += timePenaltyValue;
        }

        public void TriggerOverflowPenalty(string nodeID)
        {
            totalPenaltyScore += heavyPenaltyValue;
            UnityEngine.Debug.LogWarning("Heavy Penalty Applied! Trash overflow at Node: " + nodeID + ". Current Total Score: " + totalPenaltyScore);
            FailEpisode();
        }

        private void FailEpisode()
        {
            UnityEngine.Debug.LogError("Episode Terminated: Bullwhip effect reached critical point. Resetting Environment...");
        }

        public void AddReward(int amount)
        {
            totalPenaltyScore += amount;
        }

        public void ResetEnvironment()
        {
            globalTimeStep = 0;
            totalPenaltyScore = 0;
            timer = 0f;
        }
    }
}