using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameZeroManager : MonoBehaviour
{
    public GameObject MainStagePanel;
    public List<Image> mainStageImages;
    public GameStateReceiver gameStateReceiver;

    private int currentStage = 0;
    private bool isGameCompleted = false;

    private void Awake()
    {
        gameStateReceiver = GetComponent<GameStateReceiver>();
    }

    void Start()
    {
        if (gameStateReceiver == null)
        {
            Debug.LogError("GameStateReceiver not found!");
            return;
        }

        // 초기 이미지 설정
        UpdateStageImage();
    }

    void Update()
    {
        // GameStateReceiver로부터 현재 스테이지 정보 가져오기
        int newStage = int.Parse(gameStateReceiver.currentGameState.currentStage);
        bool newGameCompleted = gameStateReceiver.currentGameState.finalComplete == "True";

        // 스테이지가 변경되었거나 게임이 완료되었을 때 업데이트
        if (newStage != currentStage || newGameCompleted != isGameCompleted)
        {
            currentStage = newStage;
            isGameCompleted = newGameCompleted;
            UpdateStageImage();
        }
    }

    private void UpdateStageImage()
    {
        if (isGameCompleted)
        {
            // 게임 완료 시 처리 (예: 모든 이미지 숨기기)
            foreach (Image img in mainStageImages)
            {
                img.gameObject.SetActive(false);
            }
            Debug.Log("Game Zero Completed!");
            return;
        }

        // 짝수/홀수 스테이지에 따라 이미지 표시
        int imageIndex = currentStage % 2;
        for (int i = 0; i < mainStageImages.Count; i++)
        {
            mainStageImages[i].gameObject.SetActive(i == imageIndex);
        }

        Debug.Log($"Updated to stage {currentStage}, showing image {imageIndex}");
    }
}