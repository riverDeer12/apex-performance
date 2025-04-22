using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

public abstract class BaseEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set;}
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set;  }
    public DateTimeOffset UpdatedAt { get; set; }
    
    public Guid UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public void  Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.Now;
    }
}