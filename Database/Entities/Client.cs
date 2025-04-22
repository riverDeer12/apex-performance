using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

public class Client : UserType
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    [NotMapped] public string FullName => $"{FirstName} {LastName}";
}