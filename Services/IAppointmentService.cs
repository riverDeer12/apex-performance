namespace ApexPerformance.API.Services;

public interface IAppointmentService
{
    Task<bool> CheckAppointment();
}