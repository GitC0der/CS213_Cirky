using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheatingDetector : MonoBehaviour
{
    private CircularMap _map;
    private GameObject _player;

    // Start is called before the first frame update
    void Start()
    {
        _map = GameManager.Instance.Map();
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("CheatingDetector's Update method is being called!");

        if (_map.IsCheating(_player.gameObject.transform.position)) {

            Debug.Log("The player is cheating! (CHEATING DETECTOR)");
        }
    }
}
