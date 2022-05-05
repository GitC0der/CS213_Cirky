using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    public Text scoreTextPlayer;
    int scorePlayer = 0;

    private void Awake() {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        scoreTextPlayer.text = "P1: " + scorePlayer.ToString() + " Points";
    }

    // Update is called once per frame
    public void AddPoint(string name)
    {
        scorePlayer += 1;
        scoreTextPlayer.text = scorePlayer.ToString() + " Points";  
    }

    public void RemovePoint(string name)
    {
        scorePlayer -= 1;
        scoreTextPlayer.text = scorePlayer.ToString() + " Points";
    }
}
