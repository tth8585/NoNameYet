using System;

namespace TTH.Combat.Status
{
    [Serializable]
    public sealed class StatusInstance
    {
        public StatusId Id;
        public float DurationSeconds;      // <=0 means infinite
        [NonSerialized] public float RemainingSeconds;

        public bool IsExpired => DurationSeconds > 0f && RemainingSeconds <= 0f;

        public StatusInstance(StatusId id, float durationSeconds)
        {
            Id = id;
            DurationSeconds = durationSeconds;
            RemainingSeconds = durationSeconds;
        }
    }
}
