namespace ApexPerformance.API.Database.Entities;

public class Coach : UserType
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public ICollection<CoachClient> Clients { get; set; }
}