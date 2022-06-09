using System.Collections;
using System.Collections.Generic;
using Game;
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
            punishmentInProgress = true;
            audioSource.clip = cheater;
            audioSource.Play();
            
            Debug.Log("Cheater!");
            Invoke(nameof(ApplyPenalty), 1.8f);
        }
    }

    private void ApplyPenalty()
    {
        audioSource.clip = pointDeduction;
        audioSource.PlayOneShot(pointDeduction);
        GameManager.Instance.Players.Get(0).RemoveScore(GameRules.CHEATING_PENALTY);

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
