using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    // Ư�� Scene(��ȣ)���� �̵��ϰ� �ϴ� �Լ�
    public int sceneNumber = 0;

    public void Change()
    {
        SceneManager.LoadScene(sceneNumber);
    }
}
