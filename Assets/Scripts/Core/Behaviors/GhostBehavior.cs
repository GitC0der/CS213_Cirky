using static CircularMap;
using static Utils;
using UnityEngine;

//namespace Core.Behaviors;


public class GhostBehavior : AgentBehaviour
{
    public new void Awake()
    {
        base.Awake();
    }

    public void Start() {
        tag = "Ghost";
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }

    public override Steering GetSteering()
    {
        MapRing ring = new MapRing(5, new Vector2(0, 0));
        Vector2 direction = ring.Direction(ToVector2(transform.localPosition), true);
        Steering steering = new Steering();
        steering.linear = direction;
        steering.linear = transform.parent.TransformDirection(direction);
        return steering;
    }
}