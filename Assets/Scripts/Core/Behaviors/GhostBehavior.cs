using static CircularMap;
using static Utils;
using UnityEngine;
using UnityEngine.Assertions.Comparers;

//namespace Core.Behaviors;


public class GhostBehavior : AgentBehaviour
{
    private bool _isFleeing;
    public new void Awake()
    {
        base.Awake();
    }

    public void Start() {
        tag = "Ghost";
        _isFleeing = false;
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }

    public override Steering GetSteering()
    {
        Vector2 center = new Vector2(7,-5);
        Vector3 center3 = new Vector3(7,0,-5);
        MapRing ring = new MapRing(3, center);
        Vector2 direction = ring.Direction(ToVector2(transform.localPosition), true).normalized;
        //Vector3 direction3 = ToVector3(direction, 0);
        Vector3 direction3 = transform.parent.TransformDirection(ToVector3(ring.Direction(ToVector2(transform.localPosition), true), 0).normalized);
        float distance = (ToVector2(transform.localPosition) - center).magnitude;
        
        Steering steering = new Steering();
        //steering.linear = transform.TransformDirection(direction3);
        //Debug.Log($"Local is {transform.localPosition}, global is {transform.position}, center is {center}");
        
        //steering.linear = 10000*(distance*direction3 - agent.GetVelocity());
        steering.linear = Vector3.ClampMagnitude(10000*(2.5f*direction3 - agent.GetVelocity()), agent.maxAccel);
        
        //steering.linear = transform.TransformDirection(Vector3.ClampMagnitude(steering.linear, agent.maxAccel));
        //steering.linear = (agent.maxAccel*1000/steering.linear.magnitude)*steering.linear;
        //steering.linear = transform.parent.TransformDirection(Vector3.ClampMagnitude(steering.linear, agent.maxAccel));
        //steering.linear = Vector3.ClampMagnitude(direction3, agent.maxAccel);
        
        //Debug.Log($"Direction is {direction}, Steering is {steering.linear} and distance is {distance}");
        return steering;
    }
}