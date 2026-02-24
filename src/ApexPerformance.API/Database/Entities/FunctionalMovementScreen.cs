using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public class FunctionalMovementScreen : BaseEntity
{
    public required string DeepSquat { get; set; }
    public required string HurdleStep { get; set; }
    public required string InLineLunge { get; set; }
    public required string ActiveStraightLegRaise { get; set; }
    public required string TrunkStabilityPushUp { get; set; }
    public required string RotaryStability { get; set; }
    public required string ShoulderMobility { get; set; }
    public required Client Client { get; set; }
    public Guid ClientId { get; set; }
}