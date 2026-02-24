using System.ComponentModel.DataAnnotations.Schema;
using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public class Administrator : UserType
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    [NotMapped] public string FullName => $"{FirstName} {LastName}";
}