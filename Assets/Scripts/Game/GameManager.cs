using static Utils;
using System.Collections.Generic;
using System.Linq;
using Core.Behaviors;
using UnityEngine;

public class GameManager
{
    //used to make the game manager instance pop in the scenes
    private GameObject gameObject;
    private static readonly PlayerBehavior _player;
    private static float _roundTime;
    private List<GhostBehavior> _ghosts = new List<GhostBehavior>();


    public GameObject GameObject
    {
        set => gameObject = value;
    }

    public float RoundTime
    {
        set => _roundTime = value;
        get => _roundTime;
    }
    
    private static CircularMap _map;
    public CircularMap Map
    {
        private set => _map = value;
        get => _map;
    }

    static GameManager()
    {
        // Representation of the map with the pathfinder. Values found manually
        CircularMap map = new CircularMap(new Vector2(7.18f, -5.16f));
        map.AddRing(new Vector2(7.88f, -5.16f));
        map.AddRing(new Vector2(9.3f, -5.16f));
        map.AddRing(new Vector2(10.87f, -5.16f));
        map.AddPassage(new Vector2(9.72f, -3.69f));
        map.AddPassage(new Vector2(4.73f, -3.69f));
        map.AddPassage(new Vector2(7.19f, -3.9f));
        map.AddPassage(new Vector2(7.19f, -6.45f));
        map.AddPassage(new Vector2(7.19f, -7.94f));
        _map = map;
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerBehavior>();
        _roundTime = 300f;

    }

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
    
    public List<GhostBehavior> Ghosts()
    {
        if (_ghosts.Count == 0)
        {
            foreach (GameObject o in GameObject.FindGameObjectsWithTag("Ghost"))
            {
                _ghosts.Add(o.GetComponent<GhostBehavior>());
            }
        }
        return _ghosts;
    }

    public bool AllGhostsDead()
    {
        bool allDead = true;
        foreach (GhostBehavior ghost in _ghosts)
        {
            allDead = allDead && !ghost.IsAlive();
        }

        return allDead;
    }
    
    public PlayerBehavior Player() => _player;

    private Players m_Players;
    public Players Players {
        get
        {
            if (m_Players == null)
                m_Players = (gameObject.GetComponent<Players>() != null) ? gameObject.GetComponent<Players>() : gameObject.AddComponent<Players>();
            return m_Players;
        }
    }

    public void EndGame()
    {
        Cursor.visible = true;
        Time.timeScale = 0;
        Debug.Log("Game Over");
    }


    public GameObject ClosestGhostTo(Vector2 position, bool isGhost)
    {
        List<GameObject> ghosts = GameObject.FindGameObjectsWithTag("Ghost").ToList();
        ghosts.RemoveAll(g => ToVector2(g.transform.localPosition).Equals(position));
        return MinElement(ghosts, g => Vector2.Distance(ToVector2(g.transform.localPosition), position));
    }

    public GameObject OtherGhost(GameObject ghost)
    {
        List<GameObject> ghosts = GameObject.FindGameObjectsWithTag("Ghost").ToList();
        foreach (GameObject otherGhost in ghosts)
        {
            if (otherGhost != ghost) return otherGhost;
        }
        return null;
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

        private GameObject _gameObject;
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

        private Color color;

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
            //Debug.Log("Added " + n + " points to " + _name +", total score is: " + Score);

            // Update the displayed score
            ScoreManager.instance.UpdateScoreboard();
        }

        public void RemoveScore()
        {
            RemoveScore(1);       
        }

         public void RemoveScore(int n)
        {
            Score -= n;
            //Debug.Log("Removed " + n + " points from " + _name +", total score is: " + Score);

            // Update the displayed score
            ScoreManager.instance.UpdateScoreboard();
        }

        public Player():this(new GameObject()){}
        public Player(GameObject g):this(g, 0){}
        public Player(GameObject g, int score) : this(g, score, (GameManager.Instance.Players._players.Count+1).ToString()) {}

        public Player(GameObject g, int score, string name)
        {
            gameObject = g;
            Score = score;
            _name += name;
            gameObject.name = _name;
        }

        public Player GetOtherPlayer() {
            return (this == GameManager.Instance.Players.Get(0)) ? 
                GameManager.Instance.Players.Get(1): 
                GameManager.Instance.Players.Get(0);
        }
    }
    
    public Player Get(int index = 0) {
        if (_players.Count - 1 < index)
            return AddPlayer();
        return _players[index];
    }

    public Player AddPlayer()
    {
        Player p = new Player();
        _players.Add(p);
        return p;
    }

    public Player AddPlayer(GameObject player)
    {
        Player p = new Player(player);
        _players.Add(p);

        return p;
    }

    public Player AddPlayer(GameObject player, string name)
    {
        Player p = new Player(player, 0, name);
        _players.Add(p);

        Debug.Log("Added player " + name);

        return p;
    }

    private Player GetFirstOrDefault() { return Get(0); }
    private List<Player> Skip(int n) {
        if (n >= _players.Count)
            return null;
        return _players.GetRange(n, _players.Count-n);
    }

    public Player GetClosestPlayer(Transform t)
    {
        Player g = GetFirstOrDefault();
        foreach (Player p in Skip(1))
            g = (p.gameObject.transform.position - t.position).magnitude < (g.gameObject.transform.position - t.position).magnitude ? p : g;
        return g;
    }
}
