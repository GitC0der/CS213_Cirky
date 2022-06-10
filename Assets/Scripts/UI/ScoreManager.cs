using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    public Text scoreTextPlayer;
    int scorePlayer = 0;

    List<Text> scoreEntries = new List<Text>();

    private void Awake() {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        scoreTextPlayer.text = "P1: " + scorePlayer.ToString() + " Points";
    }

    public void UpdateScoreboard()
    {
        int i = 0;
        foreach(Players.Player p in GameManager.Instance.Players.players)
        {
            if (scoreEntries.Count > i)
            {
                scoreEntries[i].text = FormatScore(p);
            }
            else {
                Debug.Log("Added text instance to scoreboard ");
                scoreEntries.Add(Instantiate(scoreTextPlayer, GameObject.Find("Points").transform));
            }
        }
    }

    public string FormatScore(Players.Player player)
    {
        return string.Format("{0}: {1}", player.name, player.Score);
    }
}
