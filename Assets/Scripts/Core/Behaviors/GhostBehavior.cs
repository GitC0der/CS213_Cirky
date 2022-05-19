﻿using static CircularMap;
using static Utils;
using static Pathfinder;
using UnityEngine;
using UnityEngine.Assertions.Comparers;

//namespace Core.Behaviors;


public class GhostBehavior : AgentBehaviour
{
    private bool _isFleeing;
    private Pathfinder _pathfinder;
    private Vector2 _target;
    private CircularMap _map;  //TODO : Change this
    
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
        _target = _map.Center() + new Vector2(0,4);
        //PathfinderUtils.DrawGraph(_pathfinder);
        //_pathfinder.ComputePath(ToVector2(transform.localPosition), _target);
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }

    public override Steering GetSteering()
    {
        Vector2 center = new Vector2(7,-5);
        MapRing ring = new MapRing(3, center);
        Vector3 direction = transform.parent.TransformDirection(ToVector3(ring.Direction(ToVector2(transform.localPosition), true), 0).normalized);
        //Vector3 direction = ToVector3(_pathfinder.Orientation(ToVector2(transform.localPosition), new Vector2(0,4)), 0);
        Steering steering = new Steering();
        //steering.linear = Vector3.ClampMagnitude(1000*transform.TransformDirection(direction), agent.maxAccel);

        //steering.linear = Vector3.ClampMagnitude(10000*(direction - agent.GetVelocity()), agent.maxAccel);
        steering.linear = Vector3.ClampMagnitude(10000*(2.37f*direction - agent.GetVelocity()), agent.maxAccel);
        //steering.linear = transform.TransformDirection(Vector3.ClampMagnitude(steering.linear, agent.maxAccel));
        //steering.linear = (agent.maxAccel*1000/steering.linear.magnitude)*steering.linear;
        //steering.linear = transform.parent.TransformDirection(Vector3.ClampMagnitude(steering.linear, agent.maxAccel));
        //steering.linear = Vector3.ClampMagnitude(direction3, agent.maxAccel);
        float distance = Vector2.Distance(ToVector2(transform.localPosition), center);
        
        Debug.Log($"Direction is {direction}, Steering is {steering.linear} and distance is {distance}");
        return steering;
    }

    private CircularMap GenerateMap()
    {
        CircularMap map = new CircularMap(new Vector2(7.15f, -5), 5.5f);
        map.AddNewPassage(0, new Vector2(0,1));
        map.AddNewPassage(0, new Vector2(1,0));
        return map;
    }
}