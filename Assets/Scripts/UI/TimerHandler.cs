using UnityEngine;
using UnityEngine.UI;

/**
	This class is the implementation of the timer used in the game and how it is handled in it
*/
public class TimerHandler : MonoBehaviour
{
    private float initTimerValue;
    private Text timerText;
    public GameObject gameOverMenu;
    public GameManager gameManager;

    public void Awake() {
        initTimerValue = Time.time; 
    }

    // Start is called before the first frame update
    public void Start() {
        gameManager = GameManager.Instance;
        gameOverMenu.SetActive(false);
        timerText = GetComponent<Text>();
        setTime(gameManager.RoundTime);
    }

    
    // FixedUpdate is called 50 times per second
    public void Update() {
        float t = Time.time - initTimerValue;

        DisplayTime(t);

        if (t >= gameManager.RoundTime) {
            gameManager.EndGame();
            gameOverMenu.SetActive(true);
        }
    }

    public void setTime(float max)
    {
        gameManager.RoundTime = max;
        foreach (Button b in GameObject.Find("Game Duration UI").GetComponentsInChildren<Button>())
            b.interactable = true;
        GameObject.Find(string.Format("{0} Minutes", max / 60)).GetComponent<Button>().interactable = false;
        DisplayTime(Time.time - initTimerValue);
        //Debug.Log("Set game duration to " + maxTime);
    }

    private void DisplayTime(float t)
    {
        int minutesCount = (int)(gameManager.RoundTime - t) / 60;
        int secondsCount = (int)(gameManager.RoundTime - t) % 60;
        string minutesText = (minutesCount).ToString();
        string secondsText = (secondsCount).ToString();
        if (secondsCount < 10) secondsText = "0" + secondsText;

        timerText.text = minutesText + ":" + secondsText;
    }
}
