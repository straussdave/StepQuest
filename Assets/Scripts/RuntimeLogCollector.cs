using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Captures all Unity runtime logs and writes them to a session file under persistentDataPath.
/// Supports local export on demand.
/// </summary>
public class RuntimeLogCollector : MonoBehaviour
{
    private static RuntimeLogCollector _instance;

    private readonly object _fileLock = new object();
    private string _currentSessionLogPath;
    private DateTime _sessionStartedUtc;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;

        var existing = FindFirstObjectByType<RuntimeLogCollector>();
        if (existing != null)
        {
            _instance = existing;
            return;
        }

        var go = new GameObject(nameof(RuntimeLogCollector));
        _instance = go.AddComponent<RuntimeLogCollector>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSessionLogFile();
    }

    private void OnEnable()
    {
        Application.logMessageReceivedThreaded += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLog;
    }

    public static string GetCurrentSessionLogPath()
    {
        return _instance ? _instance._currentSessionLogPath : null;
    }

    public static string ExportCurrentSessionLog()
    {
        if (_instance == null || string.IsNullOrEmpty(_instance._currentSessionLogPath))
            return null;

        var exportsDir = Path.Combine(Application.persistentDataPath, "Exports");
        Directory.CreateDirectory(exportsDir);

        var exportPath = Path.Combine(
            exportsDir,
            $"StepQuest-log-export-{DateTime.Now:yyyyMMdd-HHmmss}.log");

        lock (_instance._fileLock)
        {
            File.Copy(_instance._currentSessionLogPath, exportPath, true);
        }

        return exportPath;
    }

    /// <summary>
    /// Exports all session log files from this device/user into one combined file.
    /// </summary>
    public static string ExportAllUserLogs()
    {
        var logsDir = Path.Combine(Application.persistentDataPath, "Logs");
        if (!Directory.Exists(logsDir))
            return null;

        var exportsDir = Path.Combine(Application.persistentDataPath, "Exports");
        Directory.CreateDirectory(exportsDir);

        var exportPath = Path.Combine(
            exportsDir,
            $"StepQuest-all-logs-{DateTime.Now:yyyyMMdd-HHmmss}.log");

        var logFiles = Directory.GetFiles(logsDir, "*.log", SearchOption.TopDirectoryOnly);
        if (logFiles.Length == 0)
            return null;

        Array.Sort(logFiles, StringComparer.Ordinal);

        var sb = new StringBuilder(64 * 1024);
        sb.AppendLine("StepQuest combined user log export");
        sb.AppendLine($"Generated: {DateTime.UtcNow:O}");
        sb.AppendLine($"Persistent data path: {Application.persistentDataPath}");
        sb.AppendLine($"Log file count: {logFiles.Length}");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine();

        foreach (var logFile in logFiles)
        {
            string content;

            // If this is the currently active session log, lock while reading it.
            if (_instance != null && string.Equals(logFile, _instance._currentSessionLogPath, StringComparison.OrdinalIgnoreCase))
            {
                lock (_instance._fileLock)
                {
                    content = File.ReadAllText(logFile, Encoding.UTF8);
                }
            }
            else
            {
                content = File.ReadAllText(logFile, Encoding.UTF8);
            }

            sb.AppendLine($"FILE: {Path.GetFileName(logFile)}");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine(content);

            if (!content.EndsWith(Environment.NewLine))
                sb.AppendLine();

            sb.AppendLine();
            sb.AppendLine(new string('=', 80));
            sb.AppendLine();
        }

        File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);
        return exportPath;
    }

    /// <summary>
    /// UI-friendly hook. Call from a button to export and print where the file was written.
    /// </summary>
    public void ExportLogsToFile()
    {
        var path = ExportCurrentSessionLog();
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[Logs] Export failed: RuntimeLogCollector is not initialized.");
            return;
        }

        Debug.Log($"[Logs] Exported runtime logs to: {path}");
    }

    /// <summary>
    /// UI-friendly hook. Exports all logs from this device into one file.
    /// </summary>
    public void ExportAllLogsToFile()
    {
        var path = ExportAllUserLogs();
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[Logs] Export all failed: No log files found.");
            return;
        }

        Debug.Log($"[Logs] Exported all user logs to: {path}");
    }

    private void InitializeSessionLogFile()
    {
        var logsDir = Path.Combine(Application.persistentDataPath, "Logs");
        Directory.CreateDirectory(logsDir);

        _currentSessionLogPath = Path.Combine(
            logsDir,
            $"StepQuest-session-{DateTime.Now:yyyyMMdd-HHmmss}.log");

        _sessionStartedUtc = DateTime.UtcNow;
        var header =
            $"StepQuest runtime log session\nStarted: {_sessionStartedUtc:O}\nPersistent data path: {Application.persistentDataPath}\n---\n";
        File.WriteAllText(_currentSessionLogPath, header, Encoding.UTF8);
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (string.IsNullOrEmpty(_currentSessionLogPath))
            return;

        var line = $"[{DateTime.Now:O}] [{type}] {condition}{Environment.NewLine}";
        var includeStack = type == LogType.Exception || type == LogType.Error || type == LogType.Assert;

        lock (_fileLock)
        {
            File.AppendAllText(_currentSessionLogPath, line, Encoding.UTF8);
            if (includeStack && !string.IsNullOrEmpty(stackTrace))
            {
                File.AppendAllText(_currentSessionLogPath, stackTrace + Environment.NewLine, Encoding.UTF8);
            }
        }
    }
}