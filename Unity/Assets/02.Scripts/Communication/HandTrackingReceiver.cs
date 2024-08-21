using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class HandLandmarksReceiver : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread receiveThread;
    private int receivePort = 5052;
    private bool isRunning = true;

    public GameObject[] landmarkObjects; // 21개의 랜드마크를 표현할 게임 오브젝트 배열

    [Serializable]
    private class LandmarkData
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    private class HandData
    {
        public Dictionary<string, LandmarkData> landmarks;
    }

    void Start()
    {
        InitializeUDP();
    }

    void InitializeUDP()
    {
        udpClient = new UdpClient(receivePort);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        Debug.Log($"UDP 수신 시작 (포트: {receivePort})");
    }

    private void ReceiveData()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string jsonString = Encoding.UTF8.GetString(data);

                HandData handData = JsonUtility.FromJson<HandData>(jsonString);

                // 메인 스레드에서 UI 업데이트
                UnityMainThreadDispatcher.Instance().Enqueue(() => UpdateHandModel(handData));
            }
            catch (Exception e)
            {
                Debug.LogError($"UDP 수신 오류: {e.Message}");
            }
        }
    }

    private void UpdateHandModel(HandData handData)
    {
        if (landmarkObjects != null && handData.landmarks != null)
        {
            for (int i = 0; i < landmarkObjects.Length; i++)
            {
                if (handData.landmarks.TryGetValue(i.ToString(), out LandmarkData landmark))
                {
                    // Unity 좌표계에 맞게 조정 (예: y와 z 축 변환)
                    Vector3 position = new Vector3(
                        landmark.x,
                        -landmark.y,  // Y축 반전
                        landmark.z
                    );

                    // 스케일 조정 (예: 10배 확대)
                    position *= 10f;

                    landmarkObjects[i].transform.localPosition = position;
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (receiveThread != null)
            receiveThread.Abort();
        if (udpClient != null)
            udpClient.Close();
    }
}

// Unity 메인 스레드에서 작업을 실행하기 위한 헬퍼 클래스
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance = null;

    public static UnityMainThreadDispatcher Instance()
    {
        if (!_instance)
        {
            _instance = FindObjectOfType<UnityMainThreadDispatcher>();
            if (!_instance)
            {
                GameObject go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
            }
        }
        return _instance;
    }

    private readonly Queue<Action> _executionQueue = new Queue<Action>();

    public void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}