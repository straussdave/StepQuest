using UnityEngine;

public class LogExportController : MonoBehaviour
{
    [SerializeField] private string targetEmail = "se24m049@technikum-wien.at";

    public void ExportLogsToMail(string subject, string body)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string filePath = RuntimeLogCollector.ExportCurrentSessionLog();

        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("Log export failed: RuntimeLogCollector not initialized or no session file.");
            return;
        }

        OpenMailIntent(filePath, subject, body);
#else
        string path = RuntimeLogCollector.ExportCurrentSessionLog();
        Debug.Log("Email export is Android-only. Exported log file: " + path);
#endif
    }

    public void ExportAllLogsToMail(string subject, string body)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string filePath = RuntimeLogCollector.ExportAllUserLogs();

        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("All-log export failed: No log files found.");
            return;
        }

        OpenMailIntent(filePath, subject, body);
#else
        string path = RuntimeLogCollector.ExportAllUserLogs();
        Debug.Log("Email export is Android-only. Exported merged log file: " + path);
#endif
    }

    private void OpenMailIntent(string filePath, string subject, string body)
    {
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND"))
            using (var uriClass = new AndroidJavaClass("android.net.Uri"))
            using (var fileObj = new AndroidJavaObject("java.io.File", filePath))
            {
                intent.Call<AndroidJavaObject>("setType", "text/plain");

                string[] emailArray = new string[] { targetEmail };
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.EMAIL", emailArray);
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.SUBJECT", subject);
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", body);

                AndroidJavaObject fileUri = uriClass.CallStatic<AndroidJavaObject>("fromFile", fileObj);
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", fileUri);

                intent.Call<AndroidJavaObject>("addFlags", 1);

                using (var chooser = intent.CallStatic<AndroidJavaObject>("createChooser", intent, "Send logs via email"))
                {
                    currentActivity.Call("startActivity", chooser);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to open email intent: " + ex.Message);
        }
    }

    public void ExportLogsToMail_Default()
    {
        ExportLogsToMail(
            "StepQuest Test Logs",
            "Hi,\n\nattached are the StepQuest test logs.\n\nThanks!"
        );
    }

    public void ExportAllLogsToMail_Default()
    {
        ExportAllLogsToMail(
            "StepQuest All User Logs",
            "Hi,\n\nattached are all locally stored StepQuest logs for this device.\n\nThanks!"
        );
    }
}