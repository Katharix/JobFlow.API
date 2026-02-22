using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services;

[ScopedService]
public class JobTrackingService
{
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public JobTrackingService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<Result> RecordLocationAsync(JobTrackingUpdateDto dto)
    {
        // ✅ Load job with its client directly
        var job = await _unitOfWork.RepositoryOf<Job>()
            .Query()
            .Include(j => j.OrganizationClient)
            .FirstOrDefaultAsync(j => j.Id == dto.JobId);

        if (job == null)
            return Result.Failure(JobErrors.NotFound);

        if (job.OrganizationClient == null)
            return Result.Failure(Error.NotFound("Client.NotFound", "The client for this job could not be found."));

        // ✅ Create new tracking record
        var record = new JobTracking
        {
            JobId = dto.JobId,
            EmployeeId = dto.EmployeeId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            RecordedAt = dto.Timestamp
        };

        await _unitOfWork.RepositoryOf<JobTracking>().AddAsync(record);
        await _unitOfWork.SaveChangesAsync();

        // ✅ Use Job.Lat/Long as destination coordinates
        if (job.Latitude.HasValue && job.Longitude.HasValue)
        {
            var etaMinutes = CalculateEta(dto.Latitude, dto.Longitude, job.Latitude.Value, job.Longitude.Value);
            if (etaMinutes <= 15)
                await _notificationService.SendClientJobTrackingEtaNotificationAsync(job.OrganizationClient, job,
                    etaMinutes);

            var distanceMiles = CalculateDistance(dto.Latitude, dto.Longitude, job.Latitude.Value, job.Longitude.Value);
            if (distanceMiles <= 0.5)
                await _notificationService.SendClientJobTrackingArrivalNotificationAsync(job.OrganizationClient, job);
        }

        return Result.Success();
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 3958.8; // Earth radius in miles
        var dLat = (lat2 - lat1) * (Math.PI / 180.0);
        var dLon = (lon2 - lon1) * (Math.PI / 180.0);
        lat1 *= Math.PI / 180.0;
        lat2 *= Math.PI / 180.0;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private int CalculateEta(double lat1, double lon1, double lat2, double lon2)
    {
        var miles = CalculateDistance(lat1, lon1, lat2, lon2);
        return (int)Math.Round(miles / 30.0 * 60); // assume 30 mph avg
    }
}