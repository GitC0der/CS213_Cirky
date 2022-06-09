using System;
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
    private static readonly Color FLEEING_COLOR = Color.yellow;
    private static readonly Vector2 NO_TARGET = new Vector2(99999, 99999);
    private const float HEIGHT = 0;
    
    private AudioSource _audioSource;
    public AudioClip _eatenSound;

    private Color _color;
    private Color _currentColor;
    private Pathfinder _pathfinder;
    private CircularMap _map;  //TODO : Change this
    private PlayerBehavior _player;
    private Vector2 _fleeingTarget = NO_TARGET;
    private bool _isFleeing;
    private float _deathTime;
    private bool _isDead;
    

    public new void Awake()
    {
        base.Awake();
    }

    public void Start()
    {
        tag = "Ghost";
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerBehavior>();
        _map = GameManager.Instance.Map();
        _pathfinder = new Pathfinder(_map, this);
        GameObject otherGhost = GameManager.Instance.OtherGhost(gameObject);
        if (_color == default)
        {
            _color = Color.red;
            SetColor(_color);
            otherGhost.GetComponent<GhostBehavior>()._color = Color.blue;
            otherGhost.GetComponent<GhostBehavior>().SetColor(Color.blue);
        }

        _deathTime = -2 * GameRules.GHOST_DEATH_DURATION;
        _isDead = false;
        _isFleeing = false;
        _audioSource = (gameObject.GetComponent<AudioSource>() != null) ? gameObject.GetComponent<AudioSource>() : gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        /*
        if (!_player.GetComponent<PlayerBehavior>().HasPower())
        {
            _pathfinder.SetTarget(ToVector2(transform.localPosition),ToVector2(_player.transform.localPosition), false);
        }
        else
        {
            FleePlayer();
        }
        */
    }
    
    // Update is called once per frame
    void Update()
    {
        if (_isDead && Time.time - _deathTime > GameRules.GHOST_DEATH_DURATION)
        {
            Revive();
        }
        
        // ---------------------------------------
        
        // TODO : Remove all this, used only for debugging purposes

        if (Input.GetKeyDown("q"))
        {
            foreach (GameObject ghost in GameObject.FindGameObjectsWithTag("Ghost"))
            {
                ghost.GetComponent<GhostBehavior>().Start();
            }
            Debug.Log($"Ghosts are now NOT blocking!");
        }

        if (Input.GetKeyDown("p"))
        {
            _player.GrabPowerUp();
            Debug.Log($"Player grabbed a power-up!");
        }
    }

    public bool IsWaiting() => _pathfinder.IsWaiting();

    public float DistanceToTarget() => _pathfinder.DistanceToTarget();

    public void Die()
    {
        _deathTime = Time.time;
        _isDead = true;
        _pathfinder.Freeze();
        _audioSource.clip = _eatenSound;
        _audioSource.Play();
        SetColor(DEAD_COLOR);
        
        /*
        if (GameManager.Instance.AllGhostsDead())
        {
            _player.LosePowerUp();
            foreach (GhostBehavior ghost in GameManager.Instance.Ghosts())
            {
                ghost.Relive();
            }
        }
        */
    }

    public void Revive()
    {
        _isDead = false;
        SetColor(_color);
        if (_player.HasPower())
        {
            FleePlayer();
            return;
        }
        if (_player.IsHurt())
        {
            MoveAway();
            return;
        }
        GoToPlayer();
        
        //_pathfinder.SetTarget(Position2(), ToVector2(_player.transform.localPosition), false);
        
        
    }

    public void SetColor(Color newColor)
    {
        if (_currentColor != newColor)
        {
            _currentColor = newColor;
            agent.SetVisualEffect(VisualEffect.VisualEffectConstAll, newColor, 0);
        }
    }

    public float GoToPlayer()
    {
        return GoTo(ToVector2(_player.transform.localPosition));
    }

    /// Makes the cellulo go to a specified target 
    public float GoTo(Vector2 target)
    {
        SetColor(_color);
        _isFleeing = false;
        _fleeingTarget = NO_TARGET;
        return _pathfinder.SetTarget(Position2(), target, false);
    }

    /// Leaves the player alone (i.e when the player just died)
    public Vector2 MoveAway()
    {
        SetColor(_color);
        _isFleeing = true;
        _pathfinder.ChangeBlockingRules(true, true);
        _fleeingTarget = _pathfinder.GenerateFleeingTarget(Position2());
        _pathfinder.SetTarget(Position2(), _fleeingTarget, true);
        return _fleeingTarget;
    }

    /// Makes the ghost flee the player (i.e when the player has a power-up)
    public Vector2 FleePlayer()
    {
        SetColor(FLEEING_COLOR);
        _isFleeing = true;
        _pathfinder.ChangeBlockingRules(true, true);
        _fleeingTarget = _pathfinder.GenerateFleeingTarget(Position2());
        _pathfinder.SetTarget(Position2(), _fleeingTarget, true);
        return _fleeingTarget;
    }

    public bool IsAlive() => !_isDead;

    public override Steering GetSteering()
    {
        if (!IsAlive())
        {
            Steering newSteering = new Steering();
            newSteering.linear = ToVector3(new Vector2(0.0f, 0.0f), 0);
            return newSteering;
        }

        if (_isFleeing && _fleeingTarget.Equals(NO_TARGET))
        {
            FleePlayer();
        }
        if (_isFleeing)
        {
            //_fleeingTarget = _pathfinder.GenerateFleeingTarget(Position2());
            _pathfinder.ChangeBlockingRules(true, true);
            _pathfinder.SetTarget(Position2(), _fleeingTarget, true);
            //_pathfinder.SetFleeing(Position2(), _previousDirection, _previousPos);
        } else
        {
            _fleeingTarget = NO_TARGET;
            _pathfinder.SetTarget(Position2(), ToVector2(_player.transform.localPosition), false);
        }
        
        if (_isFleeing && Vector2.Distance(Position2(), _fleeingTarget) < Pathfinder.TRIGGER_DIST)
        {
            _fleeingTarget = _pathfinder.GenerateFleeingTarget(Position2());
            //Debug.Log($"New fleeing target is {_fleeingTarget}");
        }
        
        
        // This is necessary since the cellulo is going upwards for no discernible reason
        transform.localPosition = new Vector3(transform.localPosition.x, HEIGHT, transform.localPosition.z);
        
        
        Vector3 direction = ToVector3(_pathfinder.Orientation(ToVector2(transform.localPosition), _isFleeing), HEIGHT);
        
        Steering steering = new Steering();

        
        /**************************************************************** /
        /       THE HOLY FORMULA : *DO* *NOT* *TOUCH* *THIS*              /
        /    Also please don't ask how it works because we have no clue   /
        /*****************************************************************/
        steering.linear = Vector3.ClampMagnitude(10000*(2.5f*direction - agent.GetVelocity()), agent.maxAccel);

        return steering;
    }
    
    private Vector2 Position2() => ToVector2(transform.localPosition);

    [Obsolete("---- Don't use this ----")]
    public Pathfinder GetPathfinder() => _pathfinder;
    
    /*
    private void Blink(float timeLeft) {
        float timeRatio = 1 - (timeLeft / GameRules.POWERUP_DURATION);
        
        const float startSpeed = 40;    // Blinking speed of the lights in the beginning, >=0
        const float endSpeed = 200;   // Blinking speed of the lights in end, >=0
        //const float offSet = 2.27f;     // The higher the later the speed starts increasing, >=0
        const float offSet = 5f;     // The higher the later the speed starts increasing, >=0

        // Function of type f(x) = ax^k + bx that decides which color to display when the cellulo is blinking
        float colorID = ((endSpeed - startSpeed)/offSet*Mathf.Pow(timeRatio, offSet) + startSpeed*timeRatio) % 2; 
        _currentColor = (colorID > 1) ? _blinkingColor : _color;
        if (_currentColor != _previousColor) agent.SetVisualEffect(VisualEffect.VisualEffectConstAll, _currentColor, 0);
        _previousColor = _currentColor;
    }
    */

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