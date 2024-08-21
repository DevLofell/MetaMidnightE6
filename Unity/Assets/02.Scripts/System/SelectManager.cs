using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

public enum InGameState
{
    Select,
    Pre,
    Traning,
    Result,
    Record
}

public class SelectManager : MonoBehaviour
{
    public static SelectManager Instance;

    public InGameState gameState = InGameState.Select;
    public List<GameObject> windowList = new List<GameObject>();
    public GameStateReceiver gameStateReceiver;
    public Button preButton;
    public Button trainingButton;
    public Button recordButton;
    public TimeSpan resentTime = TimeSpan.Zero;
    private int _index = 0;
    public int Index
    {
        get => _index;
        set
        {
            if (selectList.Count == 0) return;
            int newIndex = value;
            if (newIndex < 0)
            {
                newIndex = selectList.Count - 1;
            }
            else if (newIndex >= selectList.Count)
            {
                newIndex = 0;
            }
            if (_index != newIndex)
            {
                selectList[_index].SetActive(false);
                selectList[newIndex].SetActive(true);
                _index = newIndex;
            }
        }
    }
    public List<GameObject> selectList = new List<GameObject>();

    public Button leftButton;
    public Button rightButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        leftButton.onClick.AddListener(OnLeftButton);
        rightButton.onClick.AddListener(OnRightButton);
        preButton.onClick.AddListener(OnPreWindow);
        trainingButton.onClick.AddListener(OnTraningWindow);
        recordButton.onClick.AddListener(OnRecordWindow);
    }

    private void Start()
    {
        if (selectList.Count > 0)
        {
            selectList[_index].SetActive(true);
        }
    }

    private void OnLeftButton()
    {
        Index--;
    }

    private void OnRightButton()
    {
        Index++;
    }

    private void OnPreWindow()
    {
        ChangeState(InGameState.Pre);
    }

    private void OnTraningWindow()
    {
        ChangeState(InGameState.Traning);
    }

    private void OnRecordWindow()
    {
        ChangeState(InGameState.Record);
    }

    public void ChangeState(InGameState state)
    {
        if (gameState == InGameState.Traning && state != InGameState.Traning)
        {
            //resentTime = gameStateReceiver.GetTotalRuntime();
            UnityEngine.Debug.Log($"Training completed. Total time: {resentTime.TotalSeconds:F2} seconds");
        }

        gameState = state;

        UnityEngine.Debug.Log((int)state);
        for (int i = 0; i < windowList.Count; i++)
        {
            if (i == (int)state)
            {
                windowList[i]?.SetActive(true);
            }
            else
            {
                windowList[i]?.SetActive(false);
            }
        }

        if (state == InGameState.Result)
        {
            DisplayResult();
        }
    }

    private void DisplayResult()
    {
        Text resultText = windowList[(int)InGameState.Result].GetComponentInChildren<Text>();
        if (resultText != null)
        {
            resultText.text = $"총 걸린 시간: {resentTime.TotalSeconds:F2}초";
        }
    }

    public void ShowResultWindow()
    {
        //resentTime = gameStateReceiver.GetTotalRuntime();
        ChangeState(InGameState.Result);
    }
}