namespace Dsw2026Tpi.Domain.Entities;

public class Availability : EntityBase
{
    public Guid DoctorId { get; private set; }
    public Doctor Doctor { get; private set; }
    public DateTime Date { get; private set; }
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }
    public bool IsAvailable { get; private set; }
   
    
    
    #region Constructor for EF
    private Availability() { }
    #endregion
    
    public Availability(Guid doctorId, DateTime date, TimeSpan startTime, TimeSpan endTime, Guid? id = null) : base(id)
    {
        DoctorId = doctorId;
        Date = date;
        StartTime = startTime;
        EndTime = endTime;
        IsAvailable = true;
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }
    
    public void MarkAsUnavailable()
    {
        IsAvailable = false;
        UpdatedAt = DateTime.Now;
    }
    
    public void MarkAsAvailable()
    {
        IsAvailable = true;
        UpdatedAt = DateTime.Now;
    }
}