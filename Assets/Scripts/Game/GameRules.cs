public static class GameRules
{
    public const int CHEATING_PENALTY = 30;
    public const int GHOST_KILLED_BONUS = 10;
    public const int POINT_EATEN_BONUS = 5;
    public const int POWERUP_BONUS = 10;
    public const int PLAYER_KILLED_PENALTY = 25;
    public const int DOUBLE_KILL_BONUS = 10;

    public const float POWERUP_DURATION = 17f;
    public const float GHOST_DEATH_MIN_DURATION = 10f;
    public const float PLAYER_IMMUNITY_DURATION = 5;

    public const float GEM_SPAWN_DURATION = 20;
    public const float MIN_GEM_INTERVAL = POWERUP_DURATION + GEM_SPAWN_DURATION + 5;
    public const float MAX_GEM_INTERVAL = MIN_GEM_INTERVAL + 30;
}