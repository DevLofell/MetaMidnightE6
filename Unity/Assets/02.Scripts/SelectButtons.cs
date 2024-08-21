using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectButtons : MonoBehaviour
{
    public GameObject MainStagePanel;

    public List<GameObject> buttonSets = new List<GameObject>();
    int showNumber = 0;

    void Start()
    {
        MainStagePanel.SetActive(false);
    }

    void Update()
    {
        if (buttonSets.Count > 1)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                showNumber = (showNumber + 1) % buttonSets.Count;
                SetShowButtonUI();

            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                showNumber = (buttonSets.Count + showNumber - 1) % buttonSets.Count;
                SetShowButtonUI();
            }

        }
    }

    void SetShowButtonUI()
    {
        for(int i = 0; i < buttonSets.Count; i++)
        {
            buttonSets[i].SetActive(false);
        }
        buttonSets[showNumber].SetActive(true);
    }
}
