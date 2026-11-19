namespace ApexPerformance.API.Utilities;

/// <summary>
/// Trainings of clients who train at the same time with the coach (one
/// session, one time). Trainings saved by the apps since the session id share
/// it; older ones are grouped by the same creator and the same start to the
/// second. No database here, so it is unit tested.
/// </summary>
public static class TrainingSessions
{
    public record Key(Guid Id, Guid? SessionId, Guid CreatedBy, DateTimeOffset? StartedAt);

    /// <summary>Whether <paramref name="other"/> belongs to the session of <paramref name="training"/>.</summary>
    public static bool SameSession(Key training, Key other)
    {
        if (training.Id == other.Id)
            return true;

        if (training.SessionId is { } sessionId)
            return other.SessionId == sessionId;

        // A training on its own (or saved before the session id): an older session.
        return other.SessionId is null && other.CreatedBy == training.CreatedBy &&
               TrainingEndReminder.SameSession(training.StartedAt, other.StartedAt);
    }
}
