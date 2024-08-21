using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Globalization;

public class Calender : MonoBehaviour
{
    // 한국의 현재 날짜와 시간 표시
    private static TimeZoneInfo Korea = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");

    // UI 텍스트
    public Text KoreaText;

    void Start()
    {
        DateTime dateTime_Korea = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Korea);

        // 현재 날짜
        DateTime dateTime = DateTime.Now;

        KoreaText.text = ($"날짜 : {dateTime_Korea} \n");

    }
}
