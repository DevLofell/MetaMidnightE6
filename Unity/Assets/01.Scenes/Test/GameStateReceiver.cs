using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class GameStateReceiver : MonoBehaviour
{
    [SerializeField] public string gameIndex = "0";
    [SerializeField] private bool runInBackground = true;

    private UdpClient udpClient;
    private Thread receiveThread;
    private int receivePort = 5034;
    private bool isRunning = true;
    private string receivedData = string.Empty;
    private object dataLock = new object();

    private Process pythonProcess;
    private bool isPythonRunning = false;

    [System.Serializable]
    public class GameState
    {
        public string gameIndex;
        public string currentStage;
        public string stageComplete;
        public string finalComplete;
    }

    public List<GameObject> Stage_1_States;
    public List<GameObject> Stage_2_States;

    public GameState currentGameState = new GameState();
    public GameState previousGameState = new GameState();

    private int currentState = 0;

    void Start()
    {
        InitializeUDP();
        if (runInBackground)
        {
            StartPythonScript();
        }
    }

    void OnDisable()
    {
        StopPythonScript();
    }

    void InitializeUDP()
    {
        udpClient = new UdpClient(receivePort);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        UnityEngine.Debug.Log($"UDP initialized on port {receivePort}");
    }

    private void StartPythonScript()
    {
        if (isPythonRunning)
        {
            UnityEngine.Debug.Log("Python script is already running.");
            return;
        }

        string pythonScriptPath = GetPythonScriptPath();
        if (string.IsNullOrEmpty(pythonScriptPath))
        {
            UnityEngine.Debug.LogError("Python script path is not valid!");
            return;
        }

        try
        {
            pythonProcess = new Process();
            pythonProcess.StartInfo.FileName = "python";  // 또는 전체 경로를 사용: @"C:\Python39\python.exe"
            pythonProcess.StartInfo.Arguments = $"\"{pythonScriptPath}\" {gameIndex}";
            pythonProcess.StartInfo.UseShellExecute = false;
            pythonProcess.StartInfo.CreateNoWindow = true;
            pythonProcess.StartInfo.RedirectStandardOutput = true;
            pythonProcess.StartInfo.RedirectStandardError = true;
            pythonProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    UnityEngine.Debug.Log("Python output: " + e.Data);
            };
            pythonProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    UnityEngine.Debug.LogError("Python error: " + e.Data);
            };

            pythonProcess.Start();
            pythonProcess.BeginOutputReadLine();
            pythonProcess.BeginErrorReadLine();

            isPythonRunning = true;
            UnityEngine.Debug.Log($"Python script started in background: {pythonScriptPath}");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to start Python script: {e.Message}");
        }
    }

    private string GetPythonScriptPath()
    {
        string gamesFolderPath = Path.Combine(Application.streamingAssetsPath, "Games");
        string scriptName = $"game.py";
        string fullPath = Path.Combine(gamesFolderPath, scriptName);

        if (File.Exists(fullPath))
        {
            return fullPath;
        }
        else
        {
            UnityEngine.Debug.LogError($"Python script not found: {fullPath}");
            return null;
        }
    }

    private void StopPythonScript()
    {
        if (!isPythonRunning)
        {
            UnityEngine.Debug.Log("No Python script is running.");
            return;
        }

        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            try
            {
                pythonProcess.Kill();
                pythonProcess.WaitForExit(5000);
                if (!pythonProcess.HasExited)
                {
                    UnityEngine.Debug.LogWarning("Python process did not exit in time. Forcing termination.");
                    pythonProcess.Kill();
                }
                pythonProcess.Close();
                UnityEngine.Debug.Log("Python script stopped.");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error stopping Python script: {e.Message}");
            }
            finally
            {
                pythonProcess = null;
                isPythonRunning = false;
            }
        }
    }

    public void TogglePythonScript()
    {
        if (isPythonRunning)
        {
            StopPythonScript();
        }
        else
        {
            StartPythonScript();
        }
    }

    public void ForceQuitPythonScript()
    {
        if (isPythonRunning)
        {
            StopPythonScript();
            UnityEngine.Debug.Log("Python script forcefully terminated.");
        }
        else
        {
            UnityEngine.Debug.Log("No Python script is running to force quit.");
        }
    }

    private void ReceiveData()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedBytes = udpClient.Receive(ref remoteEndPoint);
                string data = Encoding.UTF8.GetString(receivedBytes);
                lock (dataLock)
                {
                    receivedData = data;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"UDP Receive Error: {e.Message}");
            }
        }
    }

    void Update()
    {
        string data;
        lock (dataLock)
        {
            data = receivedData;
            receivedData = string.Empty;
        }

        if (!string.IsNullOrEmpty(data))
        {
            ProcessGameState(data);
            CheckStateChanges();
        }
    }

    void ProcessGameState(string jsonData)
    {
        try
        {
            JObject jsonObject = JObject.Parse(jsonData);
            previousGameState = currentGameState;
            currentGameState = new GameState
            {
                gameIndex = jsonObject["game_index"]?.ToString(),
                currentStage = jsonObject["current_stage"]?.ToString(),
                stageComplete = jsonObject["stage_complete"]?.ToString(),
                finalComplete = jsonObject["final_complete"]?.ToString()
            };

            if (currentGameState.gameIndex != gameIndex)
            {
                gameIndex = currentGameState.gameIndex;
                UnityEngine.Debug.Log($"Game Index updated to: {gameIndex}");
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Error processing game state: {e.Message}");
        }
    }

    void CheckStateChanges()
    {
        if (currentGameState.gameIndex == "0" && currentGameState.currentStage == "0")
        {
            ResetAllStages();
        }

        if (currentGameState.gameIndex != previousGameState.gameIndex)
        {
            UnityEngine.Debug.Log($"Game Index changed to: {currentGameState.gameIndex}");
        }

        if (currentGameState.currentStage != previousGameState.currentStage)
        {
            UnityEngine.Debug.Log($"Current Stage changed to: {currentGameState.currentStage}");
            UpdateStageVisibility();
        }

        if (currentGameState.stageComplete != previousGameState.stageComplete && currentGameState.stageComplete == "True")
        {
            UnityEngine.Debug.Log("Stage completed!");
            ProgressState();
        }

        if (currentGameState.finalComplete != previousGameState.finalComplete && currentGameState.finalComplete == "True")
        {
            UnityEngine.Debug.Log("Game completed!");
        }
    }

    void UpdateStageVisibility()
    {
        ResetAllStages();

        if (currentGameState.currentStage == "1" && currentState < Stage_1_States.Count)
        {
            Stage_1_States[currentState].SetActive(true);
        }
        else if (currentGameState.currentStage == "2" && currentState < Stage_2_States.Count)
        {
            Stage_2_States[currentState].SetActive(true);
        }
    }

    void ProgressState()
    {
        if (currentGameState.currentStage == "1")
        {
            currentState++;
            if (currentState >= Stage_1_States.Count)
            {
                currentState = 0;
            }
        }
        else if (currentGameState.currentStage == "2")
        {
            currentState++;
            if (currentState >= Stage_2_States.Count)
            {
                currentState = 0;
            }
        }

        UpdateStageVisibility();
    }

    void ResetAllStages()
    {
        foreach (var stageObject in Stage_1_States)
        {
            stageObject.SetActive(false);
        }
        foreach (var stageObject in Stage_2_States)
        {
            stageObject.SetActive(false);
        }
    }

    public void SetGameIndex(string newIndex)
    {
        if (gameIndex != newIndex)
        {
            gameIndex = newIndex;
            RestartPythonScript();
        }
    }

    void RestartPythonScript()
    {
        StopPythonScript();
        StartPythonScript();
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (receiveThread != null)
            receiveThread.Abort();
        if (udpClient != null)
            udpClient.Close();
        StopPythonScript();
    }
}