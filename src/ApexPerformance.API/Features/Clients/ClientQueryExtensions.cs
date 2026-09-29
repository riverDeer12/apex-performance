using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Features.Clients;

public static class ClientQueryExtensions
{
    /// <summary>
    /// Only clients whose user account is active. Deactivating
    /// a user only marks the user as deleted, so without this
    /// its client would still show in client lists and pickers.
    /// Not a global query filter so deactivated clients still
    /// show in history (appointments, measurements...).
    /// </summary>
    public static IQueryable<Client> WithActiveUserAccount(this IQueryable<Client> clients)
        => clients.Where(x => x.User != null && !x.User.IsDeleted);
}
