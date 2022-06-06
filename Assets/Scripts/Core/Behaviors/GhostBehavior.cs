using System.Collections.Generic;
using System.Linq;
using Game.Ghosts;
using static Utils;
using UnityEngine;

namespace Core.Behaviors {
    
public class GhostBehavior : AgentBehaviour
{
    private const float HEIGHT = 0;
    private Pathfinder _pathfinder;
    private CircularMap _map;  //TODO : Change this
    private GameObject _player;
    
    //DEBUG
    private static float lastSwitchTime = 0;

    public new void Awake()
    {
        base.Awake();
    }

    public void Start()
    {
        tag = "Ghost";
        _player = GameObject.FindGameObjectWithTag("Player");
        _map = GameManager.Instance.Map();

        ICollection<GameObject> obstacles = GameObject.FindGameObjectsWithTag("Ghost").ToList();
        obstacles.Remove(gameObject);
        obstacles.Remove(_player);
        if (!Pathfinder.GHOST_IS_BLOCKING) obstacles = new List<GameObject>();
        
        _pathfinder = new Pathfinder(_map, obstacles);
        GoTo(_player.transform.localPosition);
    }
    
    // Update is called once per frame
    void Update()
    {
        // TODO : Remove all this, used only for debugging purposes
        if (Input.GetKeyDown("space"))
        {
            transform.localPosition = ToVector3(GameManager.Instance.Map().RandomPosition(), HEIGHT);
            Debug.Log($"Repositioned ghosts. May be teleported into already occupied space");
        }

        if (Input.GetKeyDown("d"))
        {
            Vector2 currentPos = ToVector2(transform.localPosition);
            Vector2 target = new Vector2(7.3f, -7.3f);
            Debug.Log($"Distance from currentPos{currentPos} to target{target} is {_pathfinder.DistanceBetween(currentPos, target)}");
        }

        if (Input.GetKeyDown("q"))
        {
            Pathfinder.GHOST_IS_BLOCKING = true;
            foreach (GameObject ghost in GameObject.FindGameObjectsWithTag("Ghost"))
            {
                ghost.GetComponent<GhostBehavior>().Start();
            }
        }
    }

    public void GoTo(Vector3 target)
    {
        _pathfinder.SetTarget(ToVector2(transform.localPosition), ToVector2(target));
        
        //Debug.Log(_map.IsCheating(ToVector2(target)) ? "Cheating!" : "NOT cheating!");
    }

    public bool IsWaiting() => _pathfinder.IsWaiting();

    public float DistanceToTarget() => _pathfinder.DistanceToCurrentTarget();

    public override Steering GetSteering()
    {
        GoTo(_player.transform.localPosition);
        
        // This is necessary since the cellulo is going upwards for no discernible reason
        transform.localPosition = new Vector3(transform.localPosition.x, HEIGHT, transform.localPosition.z);
        
        
        Vector3 direction = ToVector3(_pathfinder.Orientation(ToVector2(transform.localPosition)), HEIGHT);
        
        Steering steering = new Steering();

        
        /**************************************************************** /
        /       THE HOLY FORMULA : *DO* *NOT* *TOUCH* *THIS*              /
        /    Also please don't ask how it works because we have no clue   /
        /*****************************************************************/
        steering.linear = Vector3.ClampMagnitude(10000*(2.5f*direction - agent.GetVelocity()), agent.maxAccel);

        return steering;
    }

    /*
    private CircularMap GenerateMap()
    {
        //DEBUGGING : Ghost position = (9.14, 0, -6)
        //          : Target position = (6.94, 0, -4.50)
        CircularMap map = new CircularMap(new Vector2(7.18f, -5.16f));
        map.AddRing(new Vector2(7.88f, -5.16f));
        map.AddRing(new Vector2(9.3f, -5.16f));
        //map.AddRing(new Vector2(9.25f, -5.16f));
        map.AddRing(new Vector2(10.87f, -5.16f));
        map.AddPassage(new Vector2(9.72f, -3.69f));
        map.AddPassage(new Vector2(4.73f, -3.69f));
        map.AddPassage(new Vector2(7.19f, -3.9f));
        map.AddPassage(new Vector2(7.19f, -6.45f));
        map.AddPassage(new Vector2(7.19f, -7.94f));
        return map;
    }
    */
}
}