using UnityEngine;
using TMPro;
using LogisticsSAC.Environment;
using LogisticsSAC.Agents;

namespace LogisticsSAC.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Evaluation Mode Settings")]
        public bool isEvaluationMode = false;
        public float timeScaleMultiplier = 20f;
        public int maxEvaluationEpisodes = 100;

        [Header("Simulation Time Parameters")]
        public int globalTimeStep = 0;
        public int maxTimeSteps = 10000;
        public int currentEpisode = 1;

        [Header("Save System (Auto Resume)")]
        public bool autoResumeEpisode = true;

        [Header("Curriculum Status (Read Only)")]
        public string currentPhaseText = "Phase 1";
        public float currentActiveTimePenalty = 0f;
        public float currentActiveOverflowDrain = 0f;
        public int currentActiveNodes = 5;

        [Header("RL Reward System")]
        public float currentEpisodeReward = 0f;
        public float totalPositiveReward = 0f;
        public float totalNegativeReward = 0f;

        [Header("Evaluation Metrics")]
        public float totalTrashCollected = 0f;
        public int overflowCount = 0;
        public int wastedTripsCount = 0;
        public int depotVisitsCount = 0;
        public float totalDepotDeliveryPercentage = 0f;

        [Header("Global UI Components")]
        public TextMeshProUGUI timeStepText;
        public TextMeshProUGUI scoreBoardText;

        private TruckAgent truckAgent;
        private CommunityCenter commCenter;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            maxTimeSteps = 10000;
            commCenter = Object.FindFirstObjectByType<CommunityCenter>();
        }

        private void Start()
        {
            if (isEvaluationMode)
            {
                Time.timeScale = timeScaleMultiplier;
                currentEpisode = 1;
            }
            else
            {
                if (autoResumeEpisode) currentEpisode = PlayerPrefs.GetInt("SavedEpisode", 1);
                else PlayerPrefs.SetInt("SavedEpisode", currentEpisode);
            }

            RefreshTruckAgentCache();
            UpdateCurriculumPhase();
            ResetEnvironment();
        }

        private void FixedUpdate()
        {
            ProcessGlobalTime();
        }

        private void ProcessGlobalTime()
        {
            globalTimeStep++;
            ApplyCentralizedPenalties();
            UpdateUI();

            if (globalTimeStep >= maxTimeSteps) EndEpisodeByTimeout();
        }

        private void ApplyCentralizedPenalties()
        {
            float stepPenalty = 0f;

            if (currentActiveTimePenalty < 0f)
            {
                stepPenalty += currentActiveTimePenalty;
            }

            if (currentActiveOverflowDrain < 0f && EnvironmentManager.Instance != null)
            {
                float totalOverflowDrain = 0f;
                foreach (TrashNode node in EnvironmentManager.Instance.activeGenerators)
                {
                    if (node != null && node.IsNodeOverflowing())
                    {
                        totalOverflowDrain += node.GetOverflowDrainPenalty(currentActiveOverflowDrain);
                    }
                }
                if (totalOverflowDrain < 0f) stepPenalty += totalOverflowDrain;
            }

            if (stepPenalty < 0f)
            {
                TrackAgentReward(stepPenalty);
                if (truckAgent != null) truckAgent.AddReward(stepPenalty);
            }
        }

        public void RefreshTruckAgentCache()
        {
            truckAgent = Object.FindFirstObjectByType<TruckAgent>();
        }

        public void RegisterOverflowEvent()
        {
            overflowCount++;
            UpdateUI();
        }

        public void TrackAgentReward(float amount)
        {
            currentEpisodeReward += amount;
            if (amount > 0) totalPositiveReward += amount;
            else totalNegativeReward += amount;
            UpdateUI();
        }

        public void TrackCollection(float amount)
        {
            totalTrashCollected += amount;
        }

        public void TrackWastedTrip()
        {
            wastedTripsCount++;
        }

        public void TrackDepotDelivery(float percentage)
        {
            depotVisitsCount++;
            totalDepotDeliveryPercentage += percentage;
        }

        private void EndEpisodeByTimeout()
        {
            if (DataLogger.Instance != null && DataLogger.Instance.enableLogging)
            {
                float avgDepotPercent = depotVisitsCount > 0 ? (totalDepotDeliveryPercentage / depotVisitsCount) * 100f : 0f;
                DataLogger.Instance.LogEpisodeData(currentEpisode, currentEpisodeReward, totalPositiveReward, totalNegativeReward, totalTrashCollected, overflowCount, wastedTripsCount, avgDepotPercent);
            }

            if (isEvaluationMode && currentEpisode >= maxEvaluationEpisodes)
            {
                UnityEngine.Debug.Log($"Evaluation Complete: {maxEvaluationEpisodes} Episodes.");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPaused = true;
#endif
                return;
            }

            currentEpisode++;

            if (!isEvaluationMode)
            {
                PlayerPrefs.SetInt("SavedEpisode", currentEpisode);
                PlayerPrefs.Save();
            }

            UpdateCurriculumPhase();
            ResetEnvironment();
        }

        public void ResetEnvironment()
        {
            globalTimeStep = 0;
            currentEpisodeReward = 0f;
            totalPositiveReward = 0f;
            totalNegativeReward = 0f;
            totalTrashCollected = 0f;
            overflowCount = 0;
            wastedTripsCount = 0;
            depotVisitsCount = 0;
            totalDepotDeliveryPercentage = 0f;

            RefreshTruckAgentCache();

            if (EnvironmentManager.Instance != null)
                EnvironmentManager.Instance.ResetEnvironmentNodes();

            if (truckAgent != null) truckAgent.EndEpisode();
            UpdateUI();
        }

        private void UpdateCurriculumPhase()
        {
            if (commCenter != null)
            {
                if (isEvaluationMode)
                {
                    commCenter.SetDelayParameters(680, 1);
                    currentActiveTimePenalty = -0.0002f;
                    currentActiveOverflowDrain = -0.002f;
                    currentActiveNodes = 20;
                    currentPhaseText = "Evaluation Mode (Phase 3)";
                    return;
                }

                if (currentEpisode <= 40)
                {
                    commCenter.SetDelayParameters(0, 1);
                    currentActiveTimePenalty = -0.0002f;
                    currentActiveOverflowDrain = 0f;
                    currentActiveNodes = 5;
                    currentPhaseText = "Phase 1 (Ep 1-40): Local Sweeper";
                }
                else if (currentEpisode <= 70)
                {
                    commCenter.SetDelayParameters(300, 1);
                    currentActiveTimePenalty = -0.0002f;
                    currentActiveOverflowDrain = -0.001f;
                    currentActiveNodes = 10;
                    currentPhaseText = "Phase 2 (Ep 41-70): The Expanding City";
                }
                else
                {
                    commCenter.SetDelayParameters(680, 1);
                    currentActiveTimePenalty = -0.0002f;
                    currentActiveOverflowDrain = -0.002f;
                    currentActiveNodes = 20;
                    currentPhaseText = "Phase 3 (Ep 71-100): Crisis Management";
                }
            }
        }

        private void UpdateUI()
        {
            if (Application.isBatchMode) return;

            if (timeStepText != null)
                timeStepText.text = $"<color=yellow>{currentPhaseText}</color>\nEpisode: {currentEpisode} | Time Step: {globalTimeStep} / {maxTimeSteps}";

            if (scoreBoardText != null)
                scoreBoardText.text = $"Reward: <color=green>+{totalPositiveReward:F1}</color> | <color=red>{totalNegativeReward:F1}</color>\nTotal Score: {currentEpisodeReward:F1}\nCollected: {totalTrashCollected:F0} | Overflows: {overflowCount}";
        }
    }
}