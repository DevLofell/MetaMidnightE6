using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class HandTrackingCustomModel : MonoBehaviour
{
    public Transform leftHandParent;
    public Transform rightHandParent;
    public Transform[] leftHandJoints;
    public Transform[] rightHandJoints;

    private UdpClient udpClient;
    private Thread receiveThread;
    private int receivePort = 5052;
    private bool isRunning = true;
    private string receivedData = string.Empty;
    private object dataLock = new object();

    public float handScaleFactor = 0.1f;
    public float zScaleFactor = 0.1f;
    public Vector3 handOffset = Vector3.zero;  // 전역 위치 오프셋

    // 전역 회전 조정을 위한 변수들
    public Vector3 rotationAdjustmentAxis = Vector3.forward;
    public float rotationAdjustmentAngle = 0f;

    // 추가적인 방향 벡터 조정을 위한 변수들
    public Vector3 palmDirectionAdjustment = Vector3.forward;
    public Vector3 thumbDirectionAdjustment = Vector3.right;

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
                Debug.Log($"Received data: {data}");
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
            ProcessHandData(jsonObject, "left", leftHandJoints, leftHandParent);
            ProcessHandData(jsonObject, "right", rightHandJoints, rightHandParent);
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

    void ProcessHandData(JObject jsonObject, string handKey, Transform[] joints, Transform handParent)
    {
        if (jsonObject.TryGetValue(handKey, out JToken handToken))
        {
            try
            {
                HandLandmarks handLandmarks = handToken.ToObject<HandLandmarks>();
                if (handLandmarks != null && handLandmarks.landmarks != null && handLandmarks.landmarks.Length > 0)
                {
                    UpdateHandModel(handLandmarks, joints, handParent);
                }
                else
                {
                    HideHand(handParent);
                }
            }
            catch (JsonException je)
            {
                Debug.LogError($"Error deserializing {handKey} hand data: {je.Message}");
                HideHand(handParent);
            }
        }
        else
        {
            HideHand(handParent);
        }
    }

    void UpdateHandModel(HandLandmarks landmarks, Transform[] joints, Transform handParent)
    {
        Vector3 wristPosition = Vector3.zero;
        Quaternion wristRotation = Quaternion.identity;

        if (landmarks.landmarks.Length > 0)
        {
            wristPosition = ConvertToUnityPosition(landmarks.landmarks[0]);
            Vector3 palmDirection = (ConvertToUnityPosition(landmarks.landmarks[9]) - wristPosition).normalized;
            Vector3 thumbDirection = (ConvertToUnityPosition(landmarks.landmarks[2]) - wristPosition).normalized;

            // 방향 벡터 조정 적용
            palmDirection = Vector3.Normalize(palmDirection + palmDirectionAdjustment);
            thumbDirection = Vector3.Normalize(thumbDirection + thumbDirectionAdjustment);

            wristRotation = CalculateRotation(palmDirection, thumbDirection);
        }

        // 전역 위치 오프셋 적용
        //handParent.position = wristPosition + handOffset;
        //handParent.position = Vector3.zero;

        // 전역 회전 조정 적용
        Quaternion globalRotationAdjustment = Quaternion.AngleAxis(rotationAdjustmentAngle, rotationAdjustmentAxis);
        handParent.rotation = globalRotationAdjustment * wristRotation;

        for (int i = 1; i < landmarks.landmarks.Length && i < joints.Length; i++)
        {
            Vector3 jointPosition = ConvertToUnityPosition(landmarks.landmarks[i]);
            Vector3 jointDirection = jointPosition - wristPosition;

            int parentIndex = (i - 1) / 4 * 4;
            Vector3 parentPosition = ConvertToUnityPosition(landmarks.landmarks[parentIndex]);
            Vector3 parentDirection = parentPosition - wristPosition;

            float angle = Vector3.Angle(parentDirection, jointDirection);
            Vector3 rotationAxis = Vector3.Cross(parentDirection, jointDirection).normalized;

            joints[i].localRotation = Quaternion.AngleAxis(angle, rotationAxis);
        }

        //handParent.localScale = Vector3.one * handScaleFactor;
        handParent.gameObject.SetActive(true);
    }

    Vector3 ConvertToUnityPosition(LandmarkData landmark)
    {
        return new Vector3(
            landmark.x * handScaleFactor,
            -landmark.y * handScaleFactor,
            -landmark.z * zScaleFactor
        );
    }

    Quaternion CalculateRotation(Vector3 palmDirection, Vector3 thumbDirection)
    {
        Vector3 palmNormal = Vector3.Cross(palmDirection, thumbDirection).normalized;
        Vector3 adjustedThumbDirection = Vector3.Cross(palmNormal, palmDirection).normalized;

        return Quaternion.LookRotation(palmDirection, adjustedThumbDirection);
    }


    void HideHand(Transform handParent)
    {
        //Vector3.Angle()
        handParent.gameObject.SetActive(false);
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