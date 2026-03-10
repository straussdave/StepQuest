using System;
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

#if UNITY_ANDROID && !UNITY_EDITOR
    private void OpenMailIntent(string filePath, string subject, string body)
{
    try
    {
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var context = currentActivity.Call<AndroidJavaObject>("getApplicationContext"))
        using (var fileObj = new AndroidJavaObject("java.io.File", filePath))
        using (var intent = new AndroidJavaObject("android.content.Intent"))
        using (var intentClass = new AndroidJavaClass("android.content.Intent"))
        using (var fileProvider = new AndroidJavaClass("androidx.core.content.FileProvider"))
        {
            string packageName = context.Call<string>("getPackageName");
            string authority = packageName + ".fileprovider";

            AndroidJavaObject contentUri = fileProvider.CallStatic<AndroidJavaObject>(
                "getUriForFile",
                context,
                authority,
                fileObj
            );

            string actionSend = intentClass.GetStatic<string>("ACTION_SEND");
            string extraEmail = intentClass.GetStatic<string>("EXTRA_EMAIL");
            string extraSubject = intentClass.GetStatic<string>("EXTRA_SUBJECT");
            string extraText = intentClass.GetStatic<string>("EXTRA_TEXT");
            string extraStream = intentClass.GetStatic<string>("EXTRA_STREAM");

            int flagGrantReadUriPermission = intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION");
            int flagActivityNewTask = intentClass.GetStatic<int>("FLAG_ACTIVITY_NEW_TASK");

            string[] emailArray = { targetEmail };

            intent.Call<AndroidJavaObject>("setAction", actionSend);
            intent.Call<AndroidJavaObject>("setType", "text/plain");
            intent.Call<AndroidJavaObject>("putExtra", extraEmail, emailArray);
            intent.Call<AndroidJavaObject>("putExtra", extraSubject, subject);
            intent.Call<AndroidJavaObject>("putExtra", extraText, body);
            intent.Call<AndroidJavaObject>("putExtra", extraStream, contentUri);
            intent.Call<AndroidJavaObject>("addFlags", flagGrantReadUriPermission);
            intent.Call<AndroidJavaObject>("addFlags", flagActivityNewTask);

            using (var clipDataClass = new AndroidJavaClass("android.content.ClipData"))
            using (var clipData = clipDataClass.CallStatic<AndroidJavaObject>(
                "newRawUri",
                "StepQuest Logs",
                contentUri))
            {
                intent.Call("setClipData", clipData); // void method
            }

            using (var chooser = intentClass.CallStatic<AndroidJavaObject>(
                "createChooser",
                intent,
                "Send logs via email"))
            {
                currentActivity.Call("startActivity", chooser);
            }
        }
    }
    catch (System.Exception ex)
    {
        Debug.LogError("Failed to open email intent: " + ex);
    }
}
#endif

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