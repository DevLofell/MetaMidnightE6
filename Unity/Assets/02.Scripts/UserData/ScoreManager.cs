using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

[Serializable]
public class StageScore
{
    public int stageNumber;
    public int highScore;
    public string achievedDateTime; // ISO 8601 형식의 날짜와 시간 문자열
}

[Serializable]
public class StageScoreData
{
    public List<StageScore> stages = new List<StageScore>();
}

public class ScoreManager : MonoBehaviour
{
    private StageScoreData stageData;
    private string saveFilePath;

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "stageScores.json");
        LoadScoreData();
    }

    public void SaveScore(int stageNumber, int score)
    {
        StageScore stage = stageData.stages.Find(s => s.stageNumber == stageNumber);
        if (stage == null)
        {
            stage = new StageScore { stageNumber = stageNumber };
            stageData.stages.Add(stage);
        }

        if (score > stage.highScore)
        {
            stage.highScore = score;
            stage.achievedDateTime = DateTime.Now.ToString("o"); // ISO 8601 형식
            SaveScoreData();
        }
    }

    public (int highScore, DateTime achievedDateTime) LoadScore(int stageNumber)
    {
        StageScore stage = stageData.stages.Find(s => s.stageNumber == stageNumber);
        if (stage != null)
        {
            DateTime.TryParse(stage.achievedDateTime, out DateTime achievedDateTime);
            return (stage.highScore, achievedDateTime);
        }
        return (0, DateTime.MinValue);
    }

    public List<(int stageNumber, int highScore, DateTime achievedDateTime)> GetAllScores()
    {
        return stageData.stages.ConvertAll(stage =>
        {
            DateTime.TryParse(stage.achievedDateTime, out DateTime achievedDateTime);
            return (stage.stageNumber, stage.highScore, achievedDateTime);
        });
    }

    private void SaveScoreData()
    {
        string json = JsonUtility.ToJson(stageData);
        File.WriteAllText(saveFilePath, json);
    }

    private void LoadScoreData()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            stageData = JsonUtility.FromJson<StageScoreData>(json);
        }
        else
        {
            stageData = new StageScoreData();
        }
    }
}