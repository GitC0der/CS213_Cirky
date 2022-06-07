using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheatingDetector : MonoBehaviour
{
    private CircularMap _map;
    private GameObject _player;
    private bool punishmentInProgress = false;
    private float waitingDuration = 3f;
    private float waitEnd = 0.0001f;
    private AudioSource audioSource;
    public AudioClip cheater;

    // Start is called before the first frame update
    void Start()
    {
        _map = GameManager.Instance.Map();
        _player = GameObject.FindGameObjectWithTag("Player");
        audioSource = (gameObject.GetComponent<AudioSource>() != null) ? gameObject.GetComponent<AudioSource>() : gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (waitOver()) {
            punishmentInProgress = false;
        }

        if (_map.IsCheating(_player.gameObject.transform.position) && !punishmentInProgress) {
            startWaiting(waitingDuration);
            audioSource.Play();
            punishmentInProgress = true;
            GameManager.Instance.Players.Get(0).RemoveScore();
        }
    }

    public void startWaiting(float waitingDuration)
    {
        waitEnd = Time.time + waitingDuration;
    }

    public bool waitOver()
    {
        return waitEnd < Time.time;
    }
}
