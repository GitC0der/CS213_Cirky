using System.Linq;
using Core.Behaviors;
using UnityEngine;

//Input Keys
public enum InputKeyboard{
    Arrows, 
    WASD
}
public class PlayerBehavior : AgentBehaviour
{
    private AudioSource _audioSource;
    public AudioClip _hurtSound;
    public AudioClip _hitMetalSound;
    
    private readonly Color _color = Color.green;
    private Color _currentColor = Color.green;
    private readonly Color _blinkingColor = Color.blue;
    
    public InputKeyboard inputKeyboard;

    private bool _isHurt;
    private float _hurtTime;

    private float _grabbedPowerTime;
    private bool _hasPower;

    private float repeatRate = 0f;

    private int _score = 0;
    public int Score
    {
        get => _score;
    }

    public void Start()
    {
        agent.MoveOnIce();
        SetColor(_color);

        // TODO : Used for debugging and to give a color to the celullos on start-up
        //GrabPowerUp();
        _hurtTime = Time.time;
        _isHurt = true;
        foreach (GhostBehavior ghost in GameManager.Instance.Ghosts())
        {
            ghost.MoveAway();
        }
        
        _grabbedPowerTime = Time.time-19f;

        GameManager.Instance.Players.AddPlayer(gameObject, gameObject.name);
        
        _audioSource = (gameObject.GetComponent<AudioSource>() != null) ? gameObject.GetComponent<AudioSource>() : gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        repeatRate = Random.Range(2f, 5f);
        Invoke("SpawnPowerUp", repeatRate);
    }

    private void SpawnPowerUp()
    {
        Invoke("SpawnPowerUp", repeatRate = Random.Range(5f, 10f));
        //GrabPowerUp();
        GameObject.Instantiate(GameObject.Find("Gem"), Utils.ToVector3(GameManager.Instance.Map.RandomPosition(), this.transform.position.y), Quaternion.identity);
    }
    private void SpawnPowerUpDEBUG()
    {
        //Invoke("SpawnPowerUp", repeatRate = Random.Range(5f, 10f));
        //GrabPowerUp();
        GameObject.Instantiate(GameObject.Find("Gem"), Utils.ToVector3(GameManager.Instance.Map.RandomPosition(), this.transform.position.y), Quaternion.identity);
    }


    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Gem")
        {
            GrabPowerUp();
            GameObject.Destroy(other.gameObject);
        }
    }

    public void Update()
    {
        // TODO : Remove all this, used only for debugging purposes
        if (Input.GetKeyDown("g"))
        {
            Vector2 localMapPos = GameManager.Instance.Map.RandomPosition(5);
            Vector3 worldPos = GameManager.Instance.Map.FromMapToEnvironment(localMapPos);
            Instantiate(GameObject.Find("Gem"), worldPos, Quaternion.identity);
        }

        // ---------------------------------------

        if (_isHurt && Time.time - _hurtTime > GameRules.PLAYER_IMMUNITY_DURATION)
        {
            StopImmunity();
        }

        if (_hasPower && Time.time - _grabbedPowerTime > GameRules.POWERUP_DURATION)
        {
            LosePowerUp();
        }

        if (HasPower())
        {
            BlinkPowerUp();
            return;
        }
        if (_isHurt)
        {
            BlinkImmunity();
            return;
        }
        SetColor(_color);
    }

    public bool IsHurt() => _isHurt;

    public void GetHurt()
    {
        _hurtTime = Time.time;
        _isHurt = true;
        _audioSource.clip = _hurtSound;
        _audioSource.Play();
        GameManager.Instance.Players.Get().RemoveScore(GameRules.PLAYER_KILLED_PENALTY);
        foreach (GhostBehavior ghost in GameManager.Instance.Ghosts())
        {
            ghost.MoveAway();
        }
    }

    public void StopImmunity()
    {
        _isHurt = false;
        foreach (GhostBehavior ghost in GameManager.Instance.Ghosts())
        {
            ghost.GoToPlayer();
        }
    }
    

    //private void OnCollisionStay(Collision other)
    void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("Ghost")) return;
        GhostBehavior ghost = other.gameObject.GetComponent<GhostBehavior>();
        
        if (ghost.IsAlive() && _hasPower)
        {
            ghost.Die();
            GameManager.Instance.Players.Get(0).AddScore(GameRules.GHOST_KILLED_BONUS);
            return;
        }

        if (ghost.IsAlive() && !_hasPower && !_isHurt)
        {
            GetHurt();
            return;
        }

        if (!ghost.IsAlive())
        {
            _audioSource.clip = _hitMetalSound;
            _audioSource.Play();
            return;
        }
        
    }

    public void LosePowerUp()
    {
        _hasPower = false;
        _grabbedPowerTime = Time.time - 2 * GameRules.POWERUP_DURATION;
        foreach (GhostBehavior ghost in GameManager.Instance.Ghosts())
        {
            ghost.UpdateBehavior();
        }
    }

    public void GrabPowerUp()
    {
        StopImmunity();
        _hasPower = true;
        _grabbedPowerTime = Time.time;
        GameManager.Instance.Players.Get().AddScore(GameRules.POWERUP_BONUS);
        foreach (GhostBehavior ghost in GameManager.Instance.Ghosts())
        {
            ghost.FleePlayer(false);
        }
    }

    public bool HasPower() => _hasPower;

    public float PowerTimeLeft() => GameRules.POWERUP_DURATION - (Time.time - _grabbedPowerTime);

    public float ImmunityTimeLeft() => GameRules.PLAYER_IMMUNITY_DURATION - (Time.time - _hurtTime);

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

    private void BlinkPowerUp()
    {
        Blink(PowerTimeLeft(), GameRules.POWERUP_DURATION, 40, 200, 5, _blinkingColor, _color);
    }

    private void BlinkImmunity()
    {
        Blink(ImmunityTimeLeft(), GameRules.PLAYER_IMMUNITY_DURATION, 25, 25, 1, Color.white, _color);
    }

    /// <summary>
    ///     Makes the cellulo lights blink
    /// </summary>
    /// <param name="timeLeft">Time left before it stops to blink</param>
    /// <param name="totalTime">Total duration of blinking</param>
    /// <param name="startSpeed">Blinking speed of the lights in the beginning, >=0</param>
    /// <param name="endSpeed">Blinking speed of the lights in end, >=0</param>
    /// <param name="offSet">The higher the later the speed starts increasing, >=0</param>
    /// <param name="color1">One of the blinking colors</param>
    /// <param name="color2">The other blinking color</param>
    private void Blink(float timeLeft, float totalTime, float startSpeed, float endSpeed, float offSet, Color color1, Color color2) {
        float timeRatio = 1 - (timeLeft / totalTime);

        // Function of type f(x) = ax^k + bx that decides which color to display when the cellulo is blinking
        float colorID = ((endSpeed - startSpeed)/offSet*Mathf.Pow(timeRatio, offSet) + startSpeed*timeRatio) % 2; 
        Color newColor = (colorID > 1) ? color1 : color2;
        SetColor(newColor);
    }
    
    public void SetColor(Color newColor)
    {
        if (_currentColor != newColor)
        {
            _currentColor = newColor;
            agent.SetVisualEffect(VisualEffect.VisualEffectConstAll, newColor, 0);
        }
    }
}