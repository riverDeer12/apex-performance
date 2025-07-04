using ApexPerformance.API.Database.Entities.Abstract;
using ApexPerformance.API.Database.Entities.Catalog;

namespace ApexPerformance.API.Database.Entities;

public class AppointmentRequest : BaseEntity
{
    public required Client Client { get; set; }
    
    public Guid ClientId { get; set; }
    
    public required Appointment Appointment { get; set; }
    
    public Guid AppointmentId { get; set; }
    
    public required AppointmentRequestType AppointmentRequestType { get; set; }
    
    public Guid AppointmentRequestTypeId { get; set; }
    
    public required AppointmentRequestStatus AppointmentRequestStatus { get; set; }
    
    public Guid AppointmentRequestStatusId { get; set; }
    
    public string Comment { get; set; }
}