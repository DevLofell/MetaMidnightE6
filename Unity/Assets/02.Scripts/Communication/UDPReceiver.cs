using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UDPReceiver : MonoBehaviour
{
    private const int PORT = 5053;
    private const string IP = "192.168.56.1";
    private UdpClient client;
    private float receivedDistance;

    void Start()
    {
        try
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(IP), PORT);
            client = new UdpClient(localEndPoint);
            client.BeginReceive(new AsyncCallback(ReceiveCallback), null);
            Debug.Log($"UDP 수신 대기 중... (IP: {IP}, Port: {PORT})");

        }
        catch (Exception e)
        {
            Debug.LogError($"UDP 클라이언트 초기화 오류: {e.Message}");
        }
    }

    void ReceiveCallback(IAsyncResult ar)
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        byte[] data = client.EndReceive(ar, ref remoteEP);
        string message = Encoding.UTF8.GetString(data);

        if (float.TryParse(message, out float distance))
        {
            receivedDistance = distance;
            Debug.Log($"받은 거리: {receivedDistance} (From: {remoteEP})");
        }
        else
        {
            Debug.LogWarning($"유효하지 않은 거리 값: {message} (From: {remoteEP})");
        }

        client.BeginReceive(new AsyncCallback(ReceiveCallback), null);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), $"엄지-검지 거리: {receivedDistance:F2}");
    }

    void OnApplicationQuit()
    {
        if (client != null)
        {
            client.Close();
        }
    }
}