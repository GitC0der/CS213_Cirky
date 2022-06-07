using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheatingDetector : MonoBehaviour
{
    private CircularMap _map;
    private GameObject _player;
    private bool punishmentInProgress = false;
    private float waitingDuration = 3f;
    // private Timer timer;
    private float waitEnd = 0;

    // Start is called before the first frame update
    void Start()
    {
        _map = GameManager.Instance.Map();
        _player = GameObject.FindGameObjectWithTag("Player");
        // timer = Timer.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (waitOver()) {
            punishmentInProgress = false;
        }

        if (_map.IsCheating(_player.gameObject.transform.position) && !punishmentInProgress) {
            startWaiting(waitingDuration);
            punishmentInProgress = true;
            GameManager.Instance.Players.Get(0).RemoveScore();
            Debug.Log("The player is cheating! (CHEATING DETECTOR)");
        }

        // if (Timer.Instance.waitOver()) {
        //     punishmentInProgress = false;
        // }

        // if (_map.IsCheating(_player.gameObject.transform.position) && !punishmentInProgress) {
        //     Timer.Instance.startWaiting(waitingDuration);
        //     punishmentInProgress = true;
        //     GameManager.Instance.Players.Get(0).RemoveScore();
        //     Debug.Log("The player is cheating! (CHEATING DETECTOR)");
        // }
    }

    public void startWaiting(float waitingDuration)
    {
        waitEnd = Time.time + waitingDuration;
    }

    public bool waitOver()
    {
        Debug.Log("waitEnd: " + waitEnd + ", Time.time: " + Time.time);
        return waitEnd < Time.time;
    }
}
