using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager
{
    //used to make the game manager instance pop in the scenes
    private GameObject gameObject;

    //Singleton system with an instance of this Game Manager
    private static GameManager m_Instance;
    public static GameManager Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = new GameManager();
                m_Instance.gameObject = new GameObject("_gameManager");
                m_Instance.gameObject.AddComponent<Players>();
            }
            return m_Instance;
        }
    }

    private Players m_Players;
    public Players Players {
        get
        {
            if (m_Players == null)
                m_Players = (gameObject.GetComponent<Players>() != null) ? gameObject.GetComponent<Players>() : gameObject.AddComponent<Players>();
            return m_Players;
        }
    }
}

public class Players : MonoBehaviour {

    List<Player> _players = new List<Player>();
    public List<Player> players
    {
        get
        {
            return _players;
        }
    }

    private void Awake()
    {
        
    }

    private void Start()
    {
        
    }

    public class Player {

        private string _name = "Player ";
        public string name
        {
            get
            {
                return _name;
            }
        }

        private GameObject _gameObject = new GameObject();
        public GameObject gameObject
        {
            get
            {
                return _gameObject;
            }
            private set
            {
                _gameObject = value;
            }
        }

        private int _score;
        public int Score
        {
            get
            {
                return _score;
            }
            private set
            {
                _score = value;
            }
        }
        public void AddScore()
        {
            AddScore(1);
        }
        public void AddScore(int n)
        {
            Score += n;
            Debug.Log("Added " + n + " points to " + _name +", total score is: " + Score);
        }

        public Player():this(new GameObject()){}
        public Player(GameObject g):this(g, 0){}
        public Player(GameObject g, int score) : this(g, score, (GameManager.Instance.Players._players.Count+1).ToString()) {}

        public Player(GameObject g, int score, string name)
        {
            gameObject = g;
            Score = score;
            _name += name;
        }

        
    }
        public Player Get(int index = 0) {
            if (_players.Count - 1 < index)
                return new Player();
            return _players[index];
        }
    

    public int AddPlayer(GameObject player)
    {
        _players.Add(new Player(player));


        return 0;
    }

    private Player GetFirstOrDefault() { return Get(0); }
    private List<Player> Skip(int n) {
        if (n >= _players.Count)
            return null;
        
        return _players.GetRange(n-1, _players.Count-n);
    }

    public Player GetClosestPlayer(Transform t)
    {
        Player g = GetFirstOrDefault();
        foreach (Player p in Skip(1))
        {
            g = (p.gameObject.transform.position - t.position).magnitude < (g.gameObject.transform.position - t.position).magnitude ? p : g;
        }
        return g;
    }
}
