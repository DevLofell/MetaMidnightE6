using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

public class HandTrackingLineRenderer : MonoBehaviour
{
    public GameObject[] leftHandPoints;
    public GameObject[] rightHandPoints;
    public LineRenderer[] leftHandConnections;
    public LineRenderer[] rightHandConnections;

    private UdpClient udpClient;
    private Thread receiveThread;
    private int receivePort = 5052;
    private bool isRunning = true;
    private string receivedData = string.Empty;
    private object dataLock = new object();

    public float handScaleFactor = 0.1f;
    public float zScaleFactor = 0.1f;
    public Vector3 handOffset = new Vector3(0f, 0f, 0.5f);

    private readonly int[][] handConnections = new int[][]
    {
        new int[] {0, 1, 2, 3, 4},
        new int[] {0, 5, 6, 7, 8},
        new int[] {0, 9, 10, 11, 12},
        new int[] {0, 13, 14, 15, 16},
        new int[] {0, 17, 18, 19, 20}
    };

    private Color[] fingerColors = new Color[]
    {
        Color.red,    // 엄지
        Color.green,  // 검지
        Color.blue,   // 중지
        Color.yellow, // 약지
        Color.magenta // 소지
    };

    private Process pythonProcess;
    private bool isPythonRunning = false;

    private const string HandLayerName = "Hand";
    private int handLayerId;

    void Awake()
    {
        handLayerId = LayerMask.NameToLayer(HandLayerName);
        if (handLayerId == -1)
        {
            Debug.LogError($"Layer '{HandLayerName}' not found. Please create this layer in Unity.");
        }
    }

    void OnEnable()
    {
        StartPythonScript();
    }

    void OnDisable()
    {
        StopPythonScript();
    }

    void Start()
    {
        CreateHandPoints();
        CreateHandConnections();
        InitializeUDP();
    }

    private void StartPythonScript()
    {
        if (isPythonRunning)
        {
            Debug.Log("Python script is already running.");
            return;
        }

        string pythonScriptPath = GetPythonScriptPath();
        if (string.IsNullOrEmpty(pythonScriptPath))
        {
            Debug.LogError("Python script path is not valid!");
            return;
        }

        try
        {
            pythonProcess = new Process();
            pythonProcess.StartInfo.FileName = "python";
            pythonProcess.StartInfo.Arguments = pythonScriptPath;
            pythonProcess.StartInfo.UseShellExecute = false;
            pythonProcess.StartInfo.CreateNoWindow = true;
            pythonProcess.StartInfo.RedirectStandardOutput = true;
            pythonProcess.StartInfo.RedirectStandardError = true;
            pythonProcess.OutputDataReceived += (sender, e) => Debug.Log("Python output: " + e.Data);
            pythonProcess.ErrorDataReceived += (sender, e) => Debug.LogError("Python error: " + e.Data);

            pythonProcess.Start();
            pythonProcess.BeginOutputReadLine();
            pythonProcess.BeginErrorReadLine();

            isPythonRunning = true;
            Debug.Log($"Python script started: {pythonScriptPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start Python script: {e.Message}");
        }
    }

    private string GetPythonScriptPath()
    {
        string scriptName = "handtrack.py";
        string folderName = "Games";
        string folderpath = Path.Combine(Application.streamingAssetsPath, folderName);
        string fullPath = Path.Combine(folderpath, scriptName);

        if (File.Exists(fullPath))
        {
            return fullPath;
        }
        else
        {
            Debug.LogError($"Python script not found: {fullPath}");
            return null;
        }
    }

    private void StopPythonScript()
    {
        if (!isPythonRunning)
        {
            Debug.Log("No Python script is running.");
            return;
        }

        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            try
            {
                pythonProcess.Kill();
                pythonProcess.WaitForExit(5000);  // Wait up to 5 seconds for the process to exit
                if (!pythonProcess.HasExited)
                {
                    Debug.LogWarning("Python process did not exit in time. Forcing termination.");
                    pythonProcess.Kill();  // Force terminate if it doesn't exit in time
                }
                pythonProcess.Close();
                Debug.Log("Python script stopped.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error stopping Python script: {e.Message}");
            }
            finally
            {
                pythonProcess = null;
                isPythonRunning = false;
            }
        }
    }

    void CreateHandPoints()
    {
        leftHandPoints = new GameObject[21];
        rightHandPoints = new GameObject[21];

        for (int i = 0; i < 21; i++)
        {
            leftHandPoints[i] = CreateSphere($"LeftHand_Point_{i}", Vector3.zero);
            rightHandPoints[i] = CreateSphere($"RightHand_Point_{i}", Vector3.zero);
        }
    }

    void CreateHandConnections()
    {
        leftHandConnections = new LineRenderer[5];
        rightHandConnections = new LineRenderer[5];

        for (int i = 0; i < 5; i++)
        {
            leftHandConnections[i] = CreateLineRenderer($"LeftHand_Connection_{i}", fingerColors[i]);
            rightHandConnections[i] = CreateLineRenderer($"RightHand_Connection_{i}", fingerColors[i]);
        }
    }

    GameObject CreateSphere(string name, Vector3 position)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.layer = LayerMask.NameToLayer("Hand");
        sphere.name = name;
        sphere.transform.localScale = Vector3.one * 0.01f;
        sphere.transform.position = position;
        sphere.transform.SetParent(this.transform);
        sphere.layer = handLayerId;
        return sphere;
    }

    LineRenderer CreateLineRenderer(string name, Color color)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(this.transform);
        lineObj.layer = LayerMask.NameToLayer("Hand");
        lineObj.layer = handLayerId;
        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.startWidth = 0.002f;
        line.endWidth = 0.002f;
        line.positionCount = 5;
        line.useWorldSpace = true;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        return line;
    }

    void InitializeUDP()
    {
        udpClient = new UdpClient(receivePort);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log($"UDP initialized on port {receivePort}");
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
                Debug.LogError($"UDP Receive Error: {e.Message}");
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

        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        try
        {
            JObject jsonObject = JObject.Parse(data);
            ProcessHandData(jsonObject, "left", leftHandPoints, leftHandConnections);
            ProcessHandData(jsonObject, "right", rightHandPoints, rightHandConnections);
        }
        catch (JsonException je)
        {
            Debug.LogError($"JSON parsing error: {je.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing data: {e.Message}");
        }
    }

    void ProcessHandData(JObject jsonObject, string handKey, GameObject[] points, LineRenderer[] connections)
    {
        if (jsonObject.TryGetValue(handKey, out JToken handToken))
        {
            try
            {
                HandLandmarks handLandmarks = handToken.ToObject<HandLandmarks>();
                if (handLandmarks != null && handLandmarks.landmarks != null && handLandmarks.landmarks.Length > 0)
                {
                    UpdateHand(handLandmarks, points, connections);
                }
                else
                {
                    HideHand(points, connections);
                }
            }
            catch (JsonException je)
            {
                Debug.LogError($"Error deserializing {handKey} hand data: {je.Message}");
                HideHand(points, connections);
            }
        }
        else
        {
            HideHand(points, connections);
        }
    }

    void UpdateHand(HandLandmarks landmarks, GameObject[] points, LineRenderer[] connections)
    {
        for (int i = 0; i < landmarks.landmarks.Length && i < points.Length; i++)
        {
            LandmarkData landmark = landmarks.landmarks[i];
            Vector3 position = new Vector3(
                landmark.x * handScaleFactor,
                -landmark.y * handScaleFactor,
                -landmark.z * zScaleFactor
            ) + handOffset;

            points[i].transform.position = position;
            points[i].SetActive(true);
        }

        for (int i = 0; i < connections.Length; i++)
        {
            Vector3[] linePositions = new Vector3[5];
            for (int j = 0; j < 5 && j < handConnections[i].Length; j++)
            {
                int index = handConnections[i][j];
                if (index < points.Length)
                {
                    linePositions[j] = points[index].transform.position;
                }
            }
            connections[i].SetPositions(linePositions);
            connections[i].gameObject.SetActive(true);
        }
    }

    void HideHand(GameObject[] points, LineRenderer[] connections)
    {
        foreach (var point in points)
        {
            point.SetActive(false);
        }

        foreach (var connection in connections)
        {
            connection.gameObject.SetActive(false);
        }
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

[System.Serializable]
public class HandLandmarks
{
    public LandmarkData[] landmarks;
}

[System.Serializable]
public class LandmarkData
{
    public float x;
    public float y;
    public float z;
}