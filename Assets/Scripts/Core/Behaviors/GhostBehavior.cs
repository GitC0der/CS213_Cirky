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
        return null;
    }
}