using System.IO;
using UnityEngine;

namespace LogisticsSAC.Core
{
    public class DataLogger : MonoBehaviour
    {
        public static DataLogger Instance { get; private set; }

        [Header("Logger Settings")]
        public string modelName = "SAC_Agent";
        public bool enableLogging = false;

        private string filePath;
        private StreamWriter logWriter;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;

            InitializeLogger();
        }

        private void InitializeLogger()
        {
            if (!enableLogging) return;

            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            string directory = projectPath + "/EvaluationLogs/";

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            int existingFilesCount = Directory.GetFiles(directory, modelName + "*.csv").Length;
            int runNumber = existingFilesCount + 1;

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            filePath = directory + modelName + "_Run_" + runNumber.ToString() + "_" + timestamp + ".csv";

            string header = "Episode,TotalScore,PositiveReward,NegativeReward,TrashCollected,OverflowCount,WastedTrips,AvgDepotDeliveryPercent";

            logWriter = new StreamWriter(filePath, true);
            logWriter.WriteLine(header);

            Debug.Log("Data Logger Initialized at: " + filePath);
        }

        public void LogEpisodeData(int episode, float totalScore, float posReward, float negReward, float collected, int overflows, int wastedTrips, float avgDepotDelivery)
        {
            if (!enableLogging || logWriter == null) return;

            string data = episode.ToString() + "," +
                          totalScore.ToString("F2") + "," +
                          posReward.ToString("F2") + "," +
                          negReward.ToString("F2") + "," +
                          collected.ToString("F2") + "," +
                          overflows.ToString() + "," +
                          wastedTrips.ToString() + "," +
                          avgDepotDelivery.ToString("F2");

            logWriter.WriteLine(data);
            logWriter.Flush();
        }

        private void OnDestroy()
        {
            if (logWriter != null)
            {
                logWriter.Flush();
                logWriter.Close();
                logWriter.Dispose();
            }
        }

        private void OnApplicationQuit()
        {
            if (logWriter != null)
            {
                logWriter.Flush();
                logWriter.Close();
                logWriter.Dispose();
            }
        }
    }
}