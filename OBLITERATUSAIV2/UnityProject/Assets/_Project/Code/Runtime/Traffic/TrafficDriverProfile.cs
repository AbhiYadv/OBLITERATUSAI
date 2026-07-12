namespace ObliteratusAI.Traffic
{
    /// <summary>
    /// Seeded per-vehicle variation. Small differences prevent every car in a
    /// queue from accelerating, braking, and following in perfect lockstep.
    /// </summary>
    public readonly struct TrafficDriverProfile
    {
        public TrafficDriverProfile(
            float accelerationScale,
            float brakingScale,
            float followingGap,
            float reactionDelay,
            float cornerSpeed,
            float yellowCommitDistance)
        {
            AccelerationScale = accelerationScale;
            BrakingScale = brakingScale;
            FollowingGap = followingGap;
            ReactionDelay = reactionDelay;
            CornerSpeed = cornerSpeed;
            YellowCommitDistance = yellowCommitDistance;
        }

        public float AccelerationScale { get; }
        public float BrakingScale { get; }
        public float FollowingGap { get; }
        public float ReactionDelay { get; }
        public float CornerSpeed { get; }
        public float YellowCommitDistance { get; }
    }
}
