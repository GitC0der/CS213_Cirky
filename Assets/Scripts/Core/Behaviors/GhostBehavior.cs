using static CircularMap;
using static Utils;
using static Pathfinder;
using UnityEngine;
using UnityEngine.Assertions.Comparers;

//namespace Core.Behaviors;


public class GhostBehavior : AgentBehaviour
{
    public const float _height = 1;
    private bool _isFleeing;
    private Pathfinder _pathfinder;
    private Vector2 _target;
    private CircularMap _map;  //TODO : Change this
    private GameObject _player;
    
    public new void Awake()
    {
        base.Awake();
    }

    public void Start()
    {
        tag = "Ghost";
        _isFleeing = false;
        _map = GenerateMap();
        _pathfinder = new Pathfinder(_map);
        //_target = _map.Rings()[2].PointAt(130);    // Original Debugging target
        _target = _map.Rings()[0].PointAt(320);
        _pathfinder.ComputePath(ToVector2(transform.localPosition), _target);
        _player = GameObject.FindGameObjectWithTag("Player");
        GoTo(_player.transform.localPosition);
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoTo(Vector3 target)
    {
        _pathfinder = new Pathfinder(_map);
        _pathfinder.ComputePath(ToVector2(transform.localPosition), ToVector2(target));
    }
    

    public override Steering GetSteering()
    {
        // TODO : Use this when merging nodes is working
        const float BUG_MARGIN = 1.5f;
        float distanceGhost = _pathfinder.DistanceToClosestNode(ToVector2(transform.localPosition));
        float distancePlayer = _pathfinder.DistanceToClosestNode(ToVector2(_player.transform.localPosition));
        if (distanceGhost > BUG_MARGIN && distancePlayer > BUG_MARGIN)
        {
            GoTo(_player.transform.localPosition);
        }
        
        // This is necessary since the cellulo is going up for no discernable reason
        transform.localPosition = new Vector3(transform.localPosition.x, 0, transform.localPosition.z);
        
        //Vector3 direction = transform.parent.TransformDirection(ToVector3(ring.Direction(ToVector2(transform.localPosition), true), 0).normalized);
        
        Vector3 direction = ToVector3(_pathfinder.Orientation(ToVector2(transform.localPosition), _target), 0);
        
        Steering steering = new Steering();
        //steering.linear = Vector3.ClampMagnitude(1000*transform.TransformDirection(direction), agent.maxAccel);

        //steering.linear = Vector3.ClampMagnitude(10000*(direction - agent.GetVelocity()), agent.maxAccel);
        
        /**************************************************************** /
        /       THE HOLY FORMULA : *DO* *NOT* *TOUCH* *THIS*              /
        /*****************************************************************/
        steering.linear = Vector3.ClampMagnitude(10000*(2.5f*direction - agent.GetVelocity()), agent.maxAccel);
        
        

        //Debug.Log($"Direction is {direction}, Steering is {steering.linear}");
        return steering;
    }

    private CircularMap GenerateMap()
    {
        //DEBUGGING : Ghost position = (9.14, 0, -6)
        //          : Target position = (6.94, 0, -4.50)
        CircularMap map = new CircularMap(new Vector2(7.18f, -5.16f));
        map.AddRing(new Vector2(7.88f, -5.16f));
        map.AddRing(new Vector2(9.3f, -5.16f));
        map.AddRing(new Vector2(10.87f, -5.16f));
        map.AddPassage(new Vector2(9.72f, -3.69f));
        map.AddPassage(new Vector2(4.73f, -3.69f));
        map.AddPassage(new Vector2(7.19f, -3.9f));
        map.AddPassage(new Vector2(7.19f, -6.45f));
        map.AddPassage(new Vector2(7.19f, -7.94f));
        return map;
    }
}