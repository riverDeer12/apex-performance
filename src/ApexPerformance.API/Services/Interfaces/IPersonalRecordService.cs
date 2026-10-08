namespace ApexPerformance.API.Services.Interfaces;

public interface IPersonalRecordService
{
    /// <summary>
    /// Rebuild records of the clients from their completed trainings.
    /// Errors are logged, so saving a training never fails because of records.
    /// </summary>
    Task RecalculateForClientsAsync(IEnumerable<Guid> clientIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate records from existing trainings once, when there are none yet.
    /// </summary>
    Task CalculateMissingRecordsAsync();
}
