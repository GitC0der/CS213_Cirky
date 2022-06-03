// using Game.Ghosts;
using static CircularMap;
using static Utils;
// using static Game.Ghosts.Pathfinder;
using UnityEngine;
using UnityEngine.Assertions.Comparers;

namespace Core.Behaviors {
    
public class GhostBehavior : AgentBehaviour
{
    private Pathfinder _pathfinder;
    private CircularMap _map;  //TODO : Change this
    private GameObject _player;
    
    public new void Awake()
    {
        base.Awake();
    }

    public void Start()
    {
        tag = "Ghost";

        _map = GameManager.Instance.Map();
        _pathfinder = new Pathfinder(_map);
        _player = GameObject.FindGameObjectWithTag("Player");
        GoTo(_player.transform.localPosition);
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoTo(Vector3 target)
    {
        // For debugging purposes
        if (Input.GetKeyDown("space"))
        {
            string debugStopHere = "Place breakpoint here!";
        }
        _pathfinder.SetTarget(ToVector2(transform.localPosition), ToVector2(target));
        
        Debug.Log(_map.IsCheating(ToVector2(target)) ? "Cheating!" : "NOT cheating!");
    }
    

    public override Steering GetSteering()
    {
        GoTo(_player.transform.localPosition);
        
        // This is necessary since the cellulo is going upwards for no discernible reason
        transform.localPosition = new Vector3(transform.localPosition.x, 0, transform.localPosition.z);
        
        
        Vector3 direction = ToVector3(_pathfinder.Orientation(ToVector2(transform.localPosition)), 0);
        
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