using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Input Keys
public enum InputKeyboard{
    Arrows, 
    WASD
}
public class PlayerBehavior : AgentBehaviour
{
    // TODO : Create power up object
    const float POWER_DURATION = 30f;
    
    public InputKeyboard inputKeyboard;

    private float _grabbedPowerTime;
    private bool _hasPower;
    

    public void Start()
    {
        GrabPowerUp();
    }

    public void Update()
    {
        
        if (Time.time - _grabbedPowerTime > POWER_DURATION)
        {
            _hasPower = false;
        }
    }

    public void GrabPowerUp()
    {
        _hasPower = true;
        _grabbedPowerTime = Time.time;
    }

    public bool HasPower() => _hasPower;

    public float PowerTimeLeft() => POWER_DURATION - (Time.time - _grabbedPowerTime);

    public override Steering GetSteering()
    {
        Steering steering = new Steering();
        float horizontal = Input.GetAxis("Horizontal" + inputKeyboard);
        float vertical = Input.GetAxis("Vertical" + inputKeyboard);

        steering.linear = new Vector3(horizontal, 0, vertical)* agent.maxAccel;
        steering.linear = transform.parent.TransformDirection(Vector3.ClampMagnitude(steering.
            linear, agent.maxAccel));
        return steering;
    }

    public void ChangeControls(int idx)
    {
        inputKeyboard = (InputKeyboard) idx;
    }
}