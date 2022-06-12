using System;
using System.Collections;
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
    public AudioClip _doubleKillSound;

    private Color _color;
    private Color _currentColor;
    private Pathfinder _pathfinder;
    private CircularMap _map; 
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
        _map = GameManager.Instance.Map;
        _pathfinder = new Pathfinder(_map, this);
        GameObject otherGhost = GameManager.Instance.OtherGhost(gameObject);
        if (_color == default)
        {
            _color = Color.red;
            SetColor(_color);
            otherGhost.GetComponent<GhostBehavior>()._color = Color.blue;
            otherGhost.GetComponent<GhostBehavior>().SetColor(Color.blue);
        }

        _deathTime = -2 * GameRules.GHOST_DEATH_MIN_DURATION;
        _isDead = false;
        _isFleeing = false;
        _audioSource = (gameObject.GetComponent<AudioSource>() != null) ? gameObject.GetComponent<AudioSource>() : gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        
    }
    
    // Update is called once per frame
    void Update()
    {
        if (_isDead && Time.time - _deathTime > GameRules.GHOST_DEATH_MIN_DURATION && !_player.HasPower()) Revive();
        
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
        if (GameManager.Instance.AllGhostsDead())
        {
            Invoke(nameof(DoubleKill), 2.5f);
        }
    }

    public void UpdateBehavior()
    {
        if (!IsAlive()) return;
        if (_player.HasPower())
        {
            FleePlayer(false);
            return;
        }
        if (_player.IsHurt())
        {
            MoveAway();
            return;
        }
        GoToPlayer();
    }
    public void Revive()
    {
        _isDead = false;
        SetColor(_color);
        UpdateBehavior();
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

    /// <summary>
    ///     Makes the ghost flee the player (i.e when the player has a power-up). This is not an optimal solution, but
    /// it works so it will stay for now
    /// </summary>
    /// <param name="calledFromPathfinder">True ONLY if used from the pathfinder</param>
    /// <returns>The fleeing target</returns>
    public Vector2 FleePlayer(bool calledFromPathfinder)
    {
        if (!calledFromPathfinder) SetColor(FLEEING_COLOR);
        _isFleeing = true;
        _pathfinder.ChangeBlockingRules(true, true);
        _fleeingTarget = _pathfinder.GenerateFleeingTarget(Position2());
        _pathfinder.SetTarget(Position2(), _fleeingTarget, true);
        return _fleeingTarget;
    }

    public bool IsAlive() => !_isDead;

    public override Steering GetSteering()
    {
        // The cellulo doesn't move if it is dead
        if (!IsAlive())
        {
            Steering newSteering = new Steering();
            newSteering.linear = ToVector3(new Vector2(0.0f, 0.0f), 0);
            return newSteering;
        }

        // Sets the fleeing target if there is none
        if (_isFleeing && _fleeingTarget.Equals(NO_TARGET))
        {
            _fleeingTarget = _pathfinder.GenerateFleeingTarget(Position2());
        }
        
        if (_isFleeing)
        {
            _pathfinder.ChangeBlockingRules(true, true);
            _pathfinder.SetTarget(Position2(), _fleeingTarget, true);
        } else {
            _fleeingTarget = NO_TARGET;
            _pathfinder.SetTarget(Position2(), ToVector2(_player.transform.localPosition), false);
        }
        
        // If the celluo reached its fleeing target
        if (_isFleeing && Vector2.Distance(Position2(), _fleeingTarget) < Pathfinder.TRIGGER_DIST)
        {
            _fleeingTarget = _pathfinder.GenerateFleeingTarget(Position2());
        }
        
        
        // This is necessary since the cellulo is going upwards for no discernible reason
        //transform.localPosition = new Vector3(transform.localPosition.x, HEIGHT, transform.localPosition.z);
        
        
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

    private void DoubleKill()
    {
        _player.LosePowerUp();
        _audioSource.clip = _doubleKillSound;
        _audioSource.Play();
        GameManager.Instance.Players.Get().AddScore(GameRules.DOUBLE_KILL_BONUS);
        foreach (GhostBehavior ghost in GameManager.Instance.Ghosts())
        {
            ghost.Revive();
        }
    }

}
}