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
    public AudioClip pointDeduction;

    // Start is called before the first frame update
    void Start()
    {
        _map = GameManager.Instance.Map();
        _player = GameObject.FindGameObjectWithTag("Player");

        // Audio sources initialisation (two are needed due to switching audio source processing delays)
        audioSource = (gameObject.GetComponent<AudioSource>() != null) ? gameObject.GetComponent<AudioSource>() : gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.clip = cheater;
    }

    // Update is called once per frame
    void Update()
    {
        if (waitOver()) {
            punishmentInProgress = false;
        }

        if (_map.IsCheating(Utils.ToVector2(_player.gameObject.transform.localPosition)) && !punishmentInProgress) {
            startWaiting(waitingDuration);
            audioSource.PlayOneShot(cheater, 07f);
            audioSource.PlayOneShot(pointDeduction, 07f);
            Debug.Log("Cheater!");
            
            // audioSource.Play();
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
