using System.Collections.Generic;
using System.Linq;
using Game.Ghosts;
using static Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Core.Behaviors {
    
public class GhostBehavior : AgentBehaviour
{
    private static readonly Color DEAD_COLOR = Color.white;
    private static readonly Vector2 NO_TARGET = new Vector2(99999, 99999);
    private const float HEIGHT = 0;

    private Color _color;
    private Pathfinder _pathfinder;
    private CircularMap _map;  //TODO : Change this
    private GameObject _player;
    private Vector2 _fleeingTarget = NO_TARGET;
    private bool isDead;

    public new void Awake()
    {
        base.Awake();
    }

    public void Start()
    {
        tag = "Ghost";
        _player = GameObject.FindGameObjectWithTag("Player");
        _map = GameManager.Instance.Map();
        _pathfinder = new Pathfinder(_map, this);
        if (!_player.GetComponent<PlayerBehavior>().HasPower()) _pathfinder.SetTarget(ToVector2(transform.localPosition),ToVector2(_player.transform.localPosition), false);
    }
    
    // Update is called once per frame
    void Update()
    {

        // ---------------------------------------
        
        // TODO : Remove all this, used only for debugging purposes

        if (Input.GetKeyDown("e"))
        {
            string place_breakpoint_here = "For debugging";
        }
        
        if (Input.GetKeyDown("space"))
        {
            transform.localPosition = ToVector3(GameManager.Instance.Map().RandomPosition(), HEIGHT);
            Debug.Log($"Repositioned ghosts. May be teleported into already occupied space");
        }

        if (Input.GetKeyDown("d"))
        {
            Vector2 currentPos = ToVector2(transform.localPosition);
            Vector2 target = new Vector2(7.3f, -7.3f);
            Debug.Log($"Distance from currentPos{currentPos} to target{target} is {_pathfinder.DistanceBetween(currentPos, target, false)}");
        }

        if (Input.GetKeyDown("q"))
        {
            Pathfinder.GHOST_IS_BLOCKING = false;
            foreach (GameObject ghost in GameObject.FindGameObjectsWithTag("Ghost"))
            {
                ghost.GetComponent<GhostBehavior>().Start();
            }
            Debug.Log($"Ghosts are now NOT blocking!");
        }

        if (Input.GetKeyDown("p"))
        {
            _player.GetComponent<PlayerBehavior>().LosePowerUp();
            ICollection<GameObject> obstacles = GameObject.FindGameObjectsWithTag("Ghost").ToList();
            obstacles.Remove(gameObject);
            obstacles.Remove(_player);
            _pathfinder.SetObstacles(obstacles);
            Debug.Log($"Player has power-up is {_player.GetComponent<PlayerBehavior>().HasPower()}");
        }
    }

    public bool IsWaiting() => _pathfinder.IsWaiting();

    public float DistanceToTarget() => _pathfinder.DistanceToTarget();

    public void Die()
    {
        _pathfinder.Freeze();
        agent.SetVisualEffect(VisualEffect.VisualEffectConstAll, DEAD_COLOR, 0);
    }

    public void Relive()
    {
        _pathfinder.SetTarget(Position2(), ToVector2(_player.transform.localPosition), false);
    }

    public Vector2 NewFleeingTarget()
    {
        List<GameObject> obstacles = GameObject.FindGameObjectsWithTag("Ghost").ToList();
        obstacles.Add(GameObject.FindGameObjectWithTag("Player"));
        _pathfinder.SetObstacles(obstacles);
        _fleeingTarget = _pathfinder.GenerateFleeingTarget(Position2());
        _pathfinder.SetTarget(Position2(), _fleeingTarget, true);
        return _fleeingTarget;
    }

    public void AssignColor(Color newColor)
    {
        _color = newColor;
        agent.SetVisualEffect(VisualEffect.VisualEffectConstAll, _color, 0);
    }

    public override Steering GetSteering()
    {
        bool isFleeing = GameManager.Instance.Player().HasPower();
        if (isFleeing && _fleeingTarget.Equals(NO_TARGET))
        {
            List<GameObject> obstacles = GameObject.FindGameObjectsWithTag("Ghost").ToList();
            obstacles.Add(GameObject.FindGameObjectWithTag("Player"));
            _pathfinder.SetObstacles(obstacles);
            _fleeingTarget = _pathfinder.GenerateFleeingTarget(Position2());
        }
        if (isFleeing)
        {
            //_fleeingTarget = _pathfinder.GenerateFleeingTarget(Position2());
            Pathfinder.GHOST_IS_BLOCKING = true;
            _pathfinder.SetTarget(Position2(), _fleeingTarget, true);
            //_pathfinder.SetFleeing(Position2(), _previousDirection, _previousPos);
        } else
        {
            _fleeingTarget = NO_TARGET;
            _pathfinder.SetTarget(Position2(), ToVector2(_player.transform.localPosition), false);
        }
        
        if (isFleeing && Vector2.Distance(Position2(), _fleeingTarget) < Pathfinder.TRIGGER_DIST)
        {
            _fleeingTarget = _pathfinder.GenerateFleeingTarget(Position2());
            //Debug.Log($"New fleeing target is {_fleeingTarget}");
        }
        
        // TODO : Call GenerateNewFleeingTarget here instead of in pathfinder.Orientation
        
        // This is necessary since the cellulo is going upwards for no discernible reason
        transform.localPosition = new Vector3(transform.localPosition.x, HEIGHT, transform.localPosition.z);
        
        
        Vector3 direction = ToVector3(_pathfinder.Orientation(ToVector2(transform.localPosition), isFleeing), HEIGHT);
        
        Steering steering = new Steering();

        
        /**************************************************************** /
        /       THE HOLY FORMULA : *DO* *NOT* *TOUCH* *THIS*              /
        /    Also please don't ask how it works because we have no clue   /
        /*****************************************************************/
        steering.linear = Vector3.ClampMagnitude(10000*(2.5f*direction - agent.GetVelocity()), agent.maxAccel);

        return steering;
    }
    
    private Vector2 Position2() => ToVector2(transform.localPosition);

    public Pathfinder GetPathfinder() => _pathfinder;

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