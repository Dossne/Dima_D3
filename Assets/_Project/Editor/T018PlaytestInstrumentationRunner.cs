using System.IO;
using UnityEditor;
using UnityEngine;
using TapMiner.Core;

namespace TapMiner.EditorTools
{
    public static class T018PlaytestInstrumentationRunner
    {
        private const string SessionKey = "TapMiner.T018.PlaytestInstrumentation.Active";
        private static int stepIndex;
        private static double nextStepTime;

        private static string SessionLogPath => Path.GetFullPath(Path.Combine(Application.dataPath, "_Project", "Data", "T018_MockSessionLog.json"));
        private static string ReportPath => Path.GetFullPath(Path.Combine(Application.dataPath, "_Project", "Data", "T018_MockPlaytestReport.md"));

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        [MenuItem("Tools/Tap Miner/Run T018 Instrumentation Smoke")]
        public static void RunT018InstrumentationSmoke()
        {
            if (EditorPrefs.GetBool(SessionKey, false))
            {
                Debug.LogWarning("[T018PlaytestInstrumentationRunner] Smoke run is already active.");
                return;
            }

            EditorPrefs.SetBool(SessionKey, true);
            stepIndex = 0;
            nextStepTime = 0d;
            EditorApplication.isPlaying = true;
            Debug.Log("[T018PlaytestInstrumentationRunner] Requested play mode for T018 instrumentation smoke.");
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (!EditorPrefs.GetBool(SessionKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                stepIndex = 0;
                nextStepTime = EditorApplication.timeSinceStartup + 0.2d;
                Debug.Log("[T018PlaytestInstrumentationRunner] Entered play mode.");
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                Debug.Log("[T018PlaytestInstrumentationRunner] Returned to edit mode.");
                EditorPrefs.DeleteKey(SessionKey);
                stepIndex = 0;
                nextStepTime = 0d;
            }
        }

        private static void OnEditorUpdate()
        {
            if (!EditorPrefs.GetBool(SessionKey, false) || !EditorApplication.isPlaying)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < nextStepTime)
            {
                return;
            }

            var bootstrap = Object.FindAnyObjectByType<AppBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError("[T018PlaytestInstrumentationRunner] AppBootstrap was not found.");
                EditorApplication.isPlaying = false;
                return;
            }

            var continueRunning = ExecuteStep(bootstrap);
            nextStepTime = EditorApplication.timeSinceStartup + 0.2d;

            if (!continueRunning)
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static bool ExecuteStep(AppBootstrap bootstrap)
        {
            switch (stepIndex)
            {
                case 0:
                    bootstrap.DebugResetUpgradeProgressForValidation();
                    bootstrap.DebugResetMissionProgressForValidation();
                    Debug.Log("[T018PlaytestInstrumentationRunner] Validation state reset.");
                    stepIndex += 1;
                    return true;
                case 1:
                    Debug.Log($"[T018PlaytestInstrumentationRunner] Start run -> {bootstrap.RequestStartRun()} | Session={bootstrap.CurrentPlaytestSessionId} | RunState={bootstrap.CurrentRunState}");
                    stepIndex += 1;
                    return true;
                case 2:
                    var moveLeft = bootstrap.RequestLaneTransitionLeft();
                    bootstrap.DebugAdvanceRuntimeLoop(0.2f);
                    Debug.Log($"[T018PlaytestInstrumentationRunner] Move left -> {moveLeft} | Lane={bootstrap.CurrentCommittedLaneIndex}");
                    stepIndex += 1;
                    return true;
                case 3:
                    var process = bootstrap.RequestProcessCurrentSegment();
                    Debug.Log($"[T018PlaytestInstrumentationRunner] Process segment -> {process} | Break={bootstrap.LastBreakResolutionResult} | Loot={bootstrap.LastLootResolutionResult} | RunReward={bootstrap.CurrentRunRewardResult.TotalRewardValue}");
                    stepIndex += 1;
                    return true;
                case 4:
                    var moveCenter = bootstrap.RequestLaneTransitionRight();
                    bootstrap.DebugAdvanceRuntimeLoop(0.2f);
                    Debug.Log($"[T018PlaytestInstrumentationRunner] Move center -> {moveCenter} | Lane={bootstrap.CurrentCommittedLaneIndex}");
                    stepIndex += 1;
                    return true;
                case 5:
                    var moveHazard = bootstrap.RequestLaneTransitionRight();
                    bootstrap.DebugAdvanceRuntimeLoop(0.2f);
                    Debug.Log($"[T018PlaytestInstrumentationRunner] Move hazard -> {moveHazard} | Lane={bootstrap.CurrentCommittedLaneIndex}");
                    stepIndex += 1;
                    return true;
                case 6:
                    var hazard = bootstrap.RequestResolveCurrentLaneHazardContact();
                    Debug.Log($"[T018PlaytestInstrumentationRunner] Hazard resolve -> {hazard} | RunState={bootstrap.CurrentRunState} | Health={bootstrap.CurrentRunHealth}/{bootstrap.CurrentRunMaxHealth}");
                    stepIndex += 1;
                    return true;
                case 7:
                    bootstrap.DebugGrantSoftCurrencyForValidation(40);
                    var purchase = bootstrap.TryPurchaseUpgrade(UpgradeId.MoveSpeed);
                    Debug.Log($"[T018PlaytestInstrumentationRunner] Purchase move speed -> {purchase} | Level={bootstrap.GetUpgradeLevel(UpgradeId.MoveSpeed)} | Balance={bootstrap.SoftCurrencyBalance}");
                    stepIndex += 1;
                    return true;
                case 8:
                    var restart = bootstrap.RequestRestartRun();
                    Debug.Log($"[T018PlaytestInstrumentationRunner] Restart run -> {restart} | RunState={bootstrap.CurrentRunState} | Context={bootstrap.CurrentRunContextId}");
                    stepIndex += 1;
                    return true;
                case 9:
                    ExportArtifacts(bootstrap);
                    stepIndex += 1;
                    return false;
                default:
                    return false;
            }
        }

        private static void ExportArtifacts(AppBootstrap bootstrap)
        {
            var sessionLog = bootstrap.CreatePlaytestSessionLogSnapshot();
            var reportRecord = bootstrap.CreatePlaytestReportRecord();
            var sessionValidation = PlaytestSchemaValidator.ValidateSessionLog(sessionLog);
            var reportValidation = PlaytestSchemaValidator.ValidateReportRecord(reportRecord);

            File.WriteAllText(SessionLogPath, JsonUtility.ToJson(sessionLog, true));
            File.WriteAllText(ReportPath, PlaytestReportFormatter.FormatMarkdown(reportRecord));
            AssetDatabase.Refresh();

            Debug.Log($"[T018PlaytestInstrumentationRunner] Session validation -> Valid={sessionValidation.IsValid} | Missing={string.Join(", ", sessionValidation.MissingFields)}");
            Debug.Log($"[T018PlaytestInstrumentationRunner] Report validation -> Valid={reportValidation.IsValid} | Missing={string.Join(", ", reportValidation.MissingFields)}");
            Debug.Log($"[T018PlaytestInstrumentationRunner] CSV preview -> {bootstrap.CreatePlaytestCsvPreview().Replace(System.Environment.NewLine, " || ")}");
            Debug.Log($"[T018PlaytestInstrumentationRunner] Wrote mock session log -> {SessionLogPath}");
            Debug.Log($"[T018PlaytestInstrumentationRunner] Wrote mock playtest report -> {ReportPath}");
        }
    }
}
