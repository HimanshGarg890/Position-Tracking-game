using System;
using UnityEngine;
using System.Diagnostics;
using WebSocketSharp;
using System.Collections;

public class PoseDataReceiver : MonoBehaviour
{
    public ControlInputs CurrentInputs { get; private set; } = new ControlInputs();
    public bool IsReady = false;

    public static PoseDataReceiver Instance { get; private set; }

    private WebSocket websocket;
    private Process pythonProcess;

    [SerializeField] private string websocketUrl = "ws://localhost:8765"; // Python server address
    [SerializeField] private float connectionRetryDelay = 2f;
    [SerializeField] private int maxRetries = 10;

    private void LaunchPythonScript()
    {
        string projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
        string scriptPath = System.IO.Path.Combine(projectRoot, "Python", "pose-tracking.py");
        string pythonExe = System.IO.Path.Combine(projectRoot, "Python", ".venv", "Scripts", "python.exe");

        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"-u \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        pythonProcess = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        pythonProcess.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
                UnityEngine.Debug.Log("[Python STDOUT] " + e.Data);
        };

        pythonProcess.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
                UnityEngine.Debug.LogError("[Python STDERR] " + e.Data);
        };

        try
        {
            pythonProcess.Start();
            pythonProcess.BeginOutputReadLine();
            pythonProcess.BeginErrorReadLine();
            UnityEngine.Debug.Log("Python script started.");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("Failed to start Python script: " + ex.Message);
        }
    }

    private void Start()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LaunchPythonScript();

        // Start connection attempt after a delay to let Python start
        StartCoroutine(ConnectAfterDelay(3f));
    }

    private IEnumerator ConnectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(TryConnectWebSocket());
    }

    private IEnumerator TryConnectWebSocket()
    {
        int attempts = 0;

        while (attempts < maxRetries && !IsReady)
        {
            attempts++;
            UnityEngine.Debug.Log($"WebSocket connection attempt {attempts}/{maxRetries} to {websocketUrl}");

            bool connectionFailed = false;

            try
            {
                SetupWebSocket();
                UnityEngine.Debug.Log("WebSocket setup complete, attempting connection...");
                websocket.Connect();
                UnityEngine.Debug.Log("Connect() method called successfully");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"WebSocket connection exception: {ex.GetType().Name}: {ex.Message}");
                UnityEngine.Debug.LogError($"Stack trace: {ex.StackTrace}");
                connectionFailed = true;
            }

            if (!connectionFailed)
            {
                // Wait a bit to see if connection succeeds
                yield return new WaitForSeconds(2f); // Increased wait time

                UnityEngine.Debug.Log($"WebSocket state after waiting: {websocket.ReadyState}");

                if (websocket != null && websocket.ReadyState == WebSocketState.Open)
                {
                    UnityEngine.Debug.Log("WebSocket connected successfully!");
                    IsReady = true;
                    yield break;
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Connection attempt failed. State: {websocket?.ReadyState}");
                }
            }

            if (attempts < maxRetries)
            {
                UnityEngine.Debug.Log($"Retrying in {connectionRetryDelay} seconds...");
                yield return new WaitForSeconds(connectionRetryDelay);
            }
        }

        if (!IsReady)
        {
            UnityEngine.Debug.LogError("Failed to connect to WebSocket after all attempts");
            UnityEngine.Debug.LogError("Make sure your Python server is running and listening on the correct port");
        }
    }

    private void SetupWebSocket()
    {
        // Clean up existing connection if any
        if (websocket != null)
        {
            websocket.Close();
            websocket = null;
        }

        websocket = new WebSocket(websocketUrl);

        websocket.OnOpen += (sender, e) =>
        {
            UnityEngine.Debug.Log("WebSocket connection opened (Unity successfully connected to Python).");
            IsReady = true;
        };

        websocket.OnError += (sender, e) =>
        {
            UnityEngine.Debug.LogError($"WebSocket Error: {e.Message}");
            IsReady = false;
        };

        websocket.OnClose += (sender, e) =>
        {
            UnityEngine.Debug.Log($"WebSocket connection closed. Code: {e.Code}, Reason: {e.Reason}");
            IsReady = false;
        };

        websocket.OnMessage += (sender, e) =>
        {
            if (e.IsText)
            {
                HandleIncomingInput(e.Data);
            }
        };
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        // Keep WebSocket alive and process messages
        if (websocket != null && websocket.ReadyState == WebSocketState.Open)
        {
            // WebSocketSharp handles message dispatching automatically
        }
#endif
    }

    private void HandleIncomingInput(string json)
    {
        try
        {
            var newInputs = JsonUtility.FromJson<ControlInputs>(json);
            if (newInputs != null)
            {
                CurrentInputs = newInputs;
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to parse input JSON: {ex.Message}");
            UnityEngine.Debug.LogError($"Received JSON: {json}");
        }
    }

    private void OnApplicationQuit()
    {
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (websocket != null)
        {
            try
            {
                websocket.Close();
                websocket = null;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error closing WebSocket: {ex.Message}");
            }
        }

        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            try
            {
                pythonProcess.Kill();
                pythonProcess.Dispose();
                UnityEngine.Debug.Log("Python script terminated.");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error terminating Python process: {ex.Message}");
            }
        }
    }

    // Matches the structure of the JSON sent from Python
    [Serializable]
    public class ControlInputs
    {
        public bool jump = false;
        public float walk = 0f;
        public int kick = 0;
        public int punch = 0;
        public int move = 0;
    }
}