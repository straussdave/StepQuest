using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class AnalyticsLogger : MonoBehaviour
{
    public static AnalyticsLogger Instance { get; private set; }

    private readonly object _lock = new object();

    private string _analyticsDir;
    private string _eventsPath;
    private string _summaryPath;

    private DateTime _sessionStartedUtc;
    private bool _sessionOpenLogged;

    private const string Header =
        "timestamp_utc,event_type,session_id,quest_id,quest_name,quest_type,step_target,step_delta,raw_steps,passive_steps,extra";

    private string _sessionId;

    private int _lastRawStepCounter = -1;
    private DateTime _lastRawSnapshotUtc;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        var existing = FindFirstObjectByType<AnalyticsLogger>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        var go = new GameObject(nameof(AnalyticsLogger));
        Instance = go.AddComponent<AnalyticsLogger>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeFiles();
        LogSessionStart();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            LogEvent("app_pause");
        else
            LogEvent("app_resume");
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
            LogEvent("app_focus");
    }

    private void InitializeFiles()
    {
        _analyticsDir = Path.Combine(Application.persistentDataPath, "Analytics");
        Directory.CreateDirectory(_analyticsDir);

        _eventsPath = Path.Combine(_analyticsDir, "StepQuest-analytics-events.csv");
        _summaryPath = Path.Combine(_analyticsDir, "StepQuest-analytics-summary.csv");

        _sessionStartedUtc = DateTime.UtcNow;
        _sessionId = _sessionStartedUtc.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        if (!File.Exists(_eventsPath))
        {
            File.WriteAllText(_eventsPath, Header + Environment.NewLine, Encoding.UTF8);
        }
    }

    private void LogSessionStart()
    {
        if (_sessionOpenLogged) return;
        _sessionOpenLogged = true;

        LogEvent("session_start");
    }

    public void LogQuestSelected(Quest quest)
    {
        LogEvent(
            "quest_selected",
            quest,
            stepTarget: quest != null ? quest.Steps : 0
        );
    }

    public void LogQuestCompleted(Quest quest)
    {
        LogEvent(
            "quest_completed",
            quest,
            stepTarget: quest != null ? quest.Steps : 0
        );
    }

    public void LogQuestStepDelta(Quest quest, int delta)
    {
        if (delta <= 0) return;

        LogEvent(
            "quest_step_delta",
            quest,
            stepTarget: quest != null ? quest.Steps : 0,
            stepDelta: delta
        );
    }

    public void LogPartUnlocked(Quest quest)
    {
        LogEvent(
            "part_unlocked",
            quest,
            stepTarget: quest != null ? quest.Steps : 0
        );
    }

    public void LogPartDescriptionOpened(string partName, int minigameIndex, bool unlocked)
    {
        LogEvent(
            "part_description_opened",
            extra: $"partName={Safe(partName)};minigameIndex={minigameIndex};unlocked={unlocked}"
        );
    }

    public void LogDialogueContinue(string phase)
    {
        LogEvent("dialogue_continue", extra: $"phase={Safe(phase)}");
    }

    public void LogDialogueSkipped(string phase)
    {
        LogEvent("dialogue_skipped", extra: $"phase={Safe(phase)}");
    }

    public void LogShipRotatedFirstTime()
    {
        LogEvent("ship_rotated_first_time");
    }

    public void LogStepSensorUnavailable()
    {
        LogEvent("step_sensor_unavailable");
    }

    public void LogStepCounterSnapshot(int rawSteps)
    {
        int passiveSteps = 0;
        string extra = "";

        if (_lastRawStepCounter >= 0)
        {
            if (rawSteps >= _lastRawStepCounter)
            {
                passiveSteps = rawSteps - _lastRawStepCounter;
                extra = "valid=true";
            }
            else
            {
                // Device probably rebooted, because TYPE_STEP_COUNTER resets after reboot.
                passiveSteps = 0;
                extra = "valid=false;reason=counter_reset_or_device_reboot";
            }
        }
        else
        {
            extra = "valid=false;reason=first_snapshot";
        }

        _lastRawStepCounter = rawSteps;
        _lastRawSnapshotUtc = DateTime.UtcNow;

        LogEvent(
            "step_counter_snapshot",
            rawSteps: rawSteps,
            passiveSteps: passiveSteps,
            extra: extra
        );
    }

    public void LogEvent(
        string eventType,
        Quest quest = null,
        int stepTarget = 0,
        int stepDelta = 0,
        int rawSteps = 0,
        int passiveSteps = 0,
        string extra = "")
    {
        if (string.IsNullOrWhiteSpace(eventType)) return;

        string questId = quest != null ? quest.Id : "";
        string questName = quest != null ? quest.PartName : "";
        string questType = quest != null ? (quest.IsStoryQuest ? "story" : "normal") : "";

        string line = string.Join(",",
            Csv(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
            Csv(eventType),
            Csv(_sessionId),
            Csv(questId),
            Csv(questName),
            Csv(questType),
            stepTarget.ToString(CultureInfo.InvariantCulture),
            stepDelta.ToString(CultureInfo.InvariantCulture),
            rawSteps.ToString(CultureInfo.InvariantCulture),
            passiveSteps.ToString(CultureInfo.InvariantCulture),
            Csv(extra)
        );

        lock (_lock)
        {
            File.AppendAllText(_eventsPath, line + Environment.NewLine, Encoding.UTF8);
        }
    }

    public static string ExportAnalyticsEvents()
    {
        if (Instance == null) return null;
        return CopyToExports(Instance._eventsPath, "StepQuest-analytics-events");
    }

    public static string ExportAnalyticsSummary()
    {
        if (Instance == null) return null;

        Instance.GenerateSummaryFile();
        return CopyToExports(Instance._summaryPath, "StepQuest-analytics-summary");
    }

    public static string ExportAnalyticsReport()
    {
        if (Instance == null) return null;

        Instance.GenerateSummaryFile();

        string exportsDir = Path.Combine(Application.persistentDataPath, "Exports");
        Directory.CreateDirectory(exportsDir);

        string exportPath = Path.Combine(
            exportsDir,
            $"StepQuest-analytics-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        );

        var sb = new StringBuilder();
        sb.AppendLine("StepQuest Analytics Report");
        sb.AppendLine($"Generated UTC: {DateTime.UtcNow:O}");
        sb.AppendLine();

        sb.AppendLine("=== SUMMARY ===");
        sb.AppendLine(File.ReadAllText(Instance._summaryPath, Encoding.UTF8));

        sb.AppendLine();
        sb.AppendLine("=== RAW EVENTS ===");
        sb.AppendLine(File.ReadAllText(Instance._eventsPath, Encoding.UTF8));

        File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);
        return exportPath;
    }

    private void GenerateSummaryFile()
    {
        if (!File.Exists(_eventsPath))
            return;

        var lines = File.ReadAllLines(_eventsPath, Encoding.UTF8);
        if (lines.Length <= 1)
        {
            File.WriteAllText(_summaryPath, "metric,value\nno_data,1\n", Encoding.UTF8);
            return;
        }

        int sessionCount = 0;
        int questSelections = 0;
        int questCompletions = 0;
        int storyQuestCompletions = 0;
        int partDescriptionOpens = 0;
        int dialogueSkips = 0;
        int firstShipRotations = 0;

        int totalQuestSteps = 0;
        int totalPassiveSteps = 0;

        var activeDays = new HashSet<string>();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var cols = ParseCsvLine(lines[i]);
            if (cols.Count < 11) continue;

            string timestamp = cols[0];
            string eventType = cols[1];
            string questType = cols[5];

            int stepDelta = ParseInt(cols[7]);
            int passiveSteps = ParseInt(cols[9]);

            if (DateTime.TryParse(timestamp, null, DateTimeStyles.RoundtripKind, out var dt))
            {
                activeDays.Add(dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            switch (eventType)
            {
                case "session_start":
                    sessionCount++;
                    break;

                case "quest_selected":
                    questSelections++;
                    break;

                case "quest_completed":
                    questCompletions++;
                    if (questType == "story")
                        storyQuestCompletions++;
                    break;

                case "quest_step_delta":
                    totalQuestSteps += Math.Max(0, stepDelta);
                    break;

                case "step_counter_snapshot":
                    totalPassiveSteps += Math.Max(0, passiveSteps);
                    break;

                case "part_description_opened":
                    partDescriptionOpens++;
                    break;

                case "dialogue_skipped":
                    dialogueSkips++;
                    break;

                case "ship_rotated_first_time":
                    firstShipRotations++;
                    break;
            }
        }

        float completionRate = questSelections > 0
            ? (float)questCompletions / questSelections
            : 0f;

        int totalDetectedSteps = totalQuestSteps + totalPassiveSteps;

        var sb = new StringBuilder();
        sb.AppendLine("metric,value");
        AppendMetric(sb, "session_count", sessionCount);
        AppendMetric(sb, "active_days", activeDays.Count);
        AppendMetric(sb, "quest_selections", questSelections);
        AppendMetric(sb, "quest_completions", questCompletions);
        AppendMetric(sb, "quest_completion_rate", completionRate.ToString("0.000", CultureInfo.InvariantCulture));
        AppendMetric(sb, "total_quest_steps", totalQuestSteps);
        AppendMetric(sb, "total_passive_steps_between_sessions", totalPassiveSteps);
        AppendMetric(sb, "total_detected_steps", totalDetectedSteps);
        AppendMetric(sb, "story_quests_completed", storyQuestCompletions);
        AppendMetric(sb, "part_description_opens", partDescriptionOpens);
        AppendMetric(sb, "dialogue_skips", dialogueSkips);
        AppendMetric(sb, "ship_rotated_first_time", firstShipRotations);

        File.WriteAllText(_summaryPath, sb.ToString(), Encoding.UTF8);
    }

    private static void AppendMetric(StringBuilder sb, string metric, object value)
    {
        sb.AppendLine($"{metric},{value}");
    }

    private static string CopyToExports(string sourcePath, string prefix)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            return null;

        string exportsDir = Path.Combine(Application.persistentDataPath, "Exports");
        Directory.CreateDirectory(exportsDir);

        string exportPath = Path.Combine(
            exportsDir,
            $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}{Path.GetExtension(sourcePath)}"
        );

        File.Copy(sourcePath, exportPath, true);
        return exportPath;
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
            ? result
            : 0;
    }

    private static string Safe(string value)
    {
        return value == null ? "" : value.Replace(";", "_").Replace("\n", " ").Replace("\r", " ");
    }

    private static string Csv(string value)
    {
        if (value == null) value = "";

        bool mustQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");

        value = value.Replace("\"", "\"\"");

        return mustQuote ? $"\"{value}\"" : value;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        if (line == null) return result;

        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        result.Add(sb.ToString());
        return result;
    }
}