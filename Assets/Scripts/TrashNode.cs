using LogisticsSAC.Scripts;
using TMPro;
using UnityEngine;

namespace LogisticsSAC.Environment
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class TrashNode : MonoBehaviour
    {
        [Header("Trash Node Settings")]
        public string nodeID = "Node_01";
        public float actualTrash = 0f;
        public float observedTrash = 0f;
        public float maxTrashCapacity = 100f;

        [Header("Algorithmic Spatial Distribution")]
        [SerializeField] private bool useDistanceBasedGeneration = true;
        [SerializeField] private float minGenerateRate = 0.002f;
        [SerializeField] private float maxGenerateRate = 0.015f;
        [SerializeField] private float mapReferenceDistance = 800f;

        [Header("Stochastic Generation State")]
        public float generateRate = 0.015f;
        public float generateVariance = 0.002f;

        [Header("Asymmetric Growth Limits")]
        [SerializeField] private float minMultiplier = 0.8f;
        [SerializeField] private float maxMultiplier = 1.2f;

        [Header("UI Components")]
        public TextMeshProUGUI actualText;
        public TextMeshProUGUI observedText;

        private bool isOverflowing = false;
        private float nodeSpecificMultiplier = 1f;
        private int localOverrideTimer = 0;

        private Core.CommunityCenter commCenter;
        private static Depot[] cachedDepots;

        private void Awake()
        {
            CircleCollider2D col = this.GetComponent<CircleCollider2D>();
            col.isTrigger = true;
        }

        private void Start()
        {
            commCenter = Object.FindFirstObjectByType<Core.CommunityCenter>();

            if (useDistanceBasedGeneration)
            {
                CalculateDistanceBasedRate();
            }
        }

        private void CalculateDistanceBasedRate()
        {
            if (cachedDepots == null || cachedDepots.Length == 0)
            {
                cachedDepots = Object.FindObjectsByType<Depot>(FindObjectsSortMode.None);
            }

            if (cachedDepots == null || cachedDepots.Length == 0) return;

            float minDistance = float.MaxValue;
            foreach (var depot in cachedDepots)
            {
                float dist = Vector2.Distance(this.transform.position, depot.transform.position);
                if (dist < minDistance) minDistance = dist;
            }

            float normalizedDistance = Mathf.Clamp01(minDistance / mapReferenceDistance);
            generateRate = Mathf.Lerp(minGenerateRate, maxGenerateRate, normalizedDistance);
        }

        private void FixedUpdate()
        {
            if (localOverrideTimer > 0)
            {
                localOverrideTimer--;
            }
        }

        public void ResetGenerator()
        {
            isOverflowing = false;
            localOverrideTimer = 0;
            UpdateUI();
        }

        public void InitializeStartingTrash()
        {
            nodeSpecificMultiplier = Random.Range(minMultiplier, maxMultiplier);
            float initialTrash = Random.Range(5f, 20f);
            actualTrash = initialTrash;
            observedTrash = initialTrash;
            localOverrideTimer = 0;
            UpdateUI();
        }

        public float GetCurrentGenerateRate()
        {
            return generateRate * nodeSpecificMultiplier;
        }

        public void GenerateTrash()
        {
            if (generateRate <= 0f) return;

            float baseRate = GetCurrentGenerateRate();
            float randomizedGeneration = baseRate + Random.Range(-generateVariance, generateVariance);
            float actualGenerationAmount = Mathf.Max(0f, randomizedGeneration);

            actualTrash += actualGenerationAmount;

            if (actualTrash >= maxTrashCapacity)
            {
                if (!isOverflowing)
                {
                    isOverflowing = true;
                    if (Core.GameManager.Instance != null)
                    {
                        Core.GameManager.Instance.RegisterOverflowEvent();
                    }
                }
            }
            else
            {
                if (isOverflowing)
                {
                    isOverflowing = false;
                }
            }

            UpdateUI();
        }

        public float GetOverflowDrainPenalty(float drainPenalty)
        {
            if (!isOverflowing) return 0f;
            return drainPenalty;
        }

        public void UpdateDelayedObservation(float delayedAmount)
        {
            if (localOverrideTimer <= 0)
            {
                observedTrash = delayedAmount;
                UpdateUI();
            }
        }

        public float CollectTrash(float availableCapacity)
        {
            float amountToCollect = Mathf.Min(actualTrash, availableCapacity);
            actualTrash -= amountToCollect;

            if (actualTrash < maxTrashCapacity)
            {
                isOverflowing = false;
            }

            if (commCenter != null)
            {
                localOverrideTimer = commCenter.GetCurrentDelaySteps() + 1;
            }

            observedTrash = actualTrash;
            UpdateUI();

            return amountToCollect;
        }

        public bool IsNodeOverflowing()
        {
            return isOverflowing;
        }

        private void UpdateUI()
        {
            if (Application.isBatchMode) return;

            if (actualText != null) actualText.text = Mathf.RoundToInt(actualTrash).ToString();
            if (observedText != null) observedText.text = Mathf.RoundToInt(observedTrash).ToString();
        }
    }
}