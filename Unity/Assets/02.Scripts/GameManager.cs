using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UserData
{
    public string username;
    public int totalScore;
}
public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

    public List<GameObject> gameList = new List<GameObject>();

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnGameStarted(int index)
    {

    }
}
