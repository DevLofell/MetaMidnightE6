using UnityEngine;
using UnityEngine.UI;

public class AttendanceManager : MonoBehaviour
{
    public Text[] dayTexts;  // 출석 상태를 표시할 Text 개체 배열
    public Button attendanceButton;  // 현재 날짜의 출석을 기록할 버튼

    private int currentDay;  // 현재 날짜 (첫 번째 날은 0)
    private bool[] attendanceRecord = new bool[7];  // 7일 동안의 출석 기록

    private void Start()
    {
        LoadAttendanceData();
        UpdateUI();
    }

    // PlayerPrefs에서 출석 데이터를 불러옵니다.
    private void LoadAttendanceData()
    {
        for (int i = 0; i < 7; i++)
        {
            attendanceRecord[i] = PlayerPrefs.GetInt("Day" + i, 0) == 1;
        }
    }

    // PlayerPrefs에 출석 데이터를 저장합니다.
    private void SaveAttendanceData()
    {
        for (int i = 0; i < 7; i++)
        {
            PlayerPrefs.SetInt("Day" + i, attendanceRecord[i] ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    // 현재 날짜의 출석을 기록합니다.
    public void MarkAttendance()
    {
        if (currentDay < 7 && !attendanceRecord[currentDay])
        {
            attendanceRecord[currentDay] = true;
            SaveAttendanceData();
            UpdateUI();
        }
    }

    // 출석 상태를 반영하기 위해 UI를 업데이트합니다.
    private void UpdateUI()
    {
        for (int i = 0; i < 7; i++)
        {
            if (i < currentDay)
            {
                dayTexts[i].text = "Day " + (i + 1) + " - 출석 완료";
            }
            else if (i == currentDay)
            {
                dayTexts[i].text = "Day " + (i + 1) + " - 오늘";
            }
            else
            {
                dayTexts[i].text = "Day " + (i + 1) + " - 미출석";
            }
        }

        attendanceButton.interactable = currentDay < 7 && !attendanceRecord[currentDay];
    }
}