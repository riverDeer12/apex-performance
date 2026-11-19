using ApexPerformance.API.Utilities;

namespace ApexPerformance.Tests;

public class TrainingSessionsTests
{
    private static readonly Guid Coach = Guid.NewGuid();
    private static readonly Guid OtherCoach = Guid.NewGuid();
    private static readonly Guid Session = Guid.NewGuid();
    private static readonly DateTimeOffset Started = new(2026, 11, 19, 17, 0, 0, TimeSpan.Zero);

    private static TrainingSessions.Key Key(Guid? session = null, Guid? createdBy = null, DateTimeOffset? started = null)
        => new(Guid.NewGuid(), session, createdBy ?? Coach, started);

    [Fact]
    public void A_training_is_in_its_own_session()
    {
        var training = Key();
        Assert.True(TrainingSessions.SameSession(training, training));
    }

    [Fact]
    public void Trainings_with_the_session_id_are_one_session()
    {
        var ana = Key(Session, started: Started);
        // Another start (edited) or another creator: the id decides.
        Assert.True(TrainingSessions.SameSession(ana, Key(Session, OtherCoach, Started.AddMinutes(5))));
        Assert.False(TrainingSessions.SameSession(ana, Key(Guid.NewGuid(), started: Started)));
        Assert.False(TrainingSessions.SameSession(ana, Key(started: Started)));
    }

    [Fact]
    public void Older_trainings_by_the_same_coach_and_start()
    {
        var ana = Key(started: Started);
        Assert.True(TrainingSessions.SameSession(ana, Key(started: Started.AddMilliseconds(400))));
        Assert.False(TrainingSessions.SameSession(ana, Key(started: Started.AddSeconds(3))));
        Assert.False(TrainingSessions.SameSession(ana, Key(createdBy: OtherCoach, started: Started)));
        // One with a session id belongs to that session.
        Assert.False(TrainingSessions.SameSession(ana, Key(Session, started: Started)));
        // Not started: on its own.
        Assert.False(TrainingSessions.SameSession(Key(), Key()));
    }
}
