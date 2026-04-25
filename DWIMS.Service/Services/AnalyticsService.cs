using DWIMS.Data;
using DWIMS.Service.Analytics;
using DWIMS.Service.Analytics.Dtos;
using DWIMS.Service.Common;
using DWIMS.Service.CurrentUser;
using DWIMS.Service.User;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class AnalyticsService(
    AppDbContext context,
    ICurrentUserService currentUser) : IAnalyticsService
{
    public async Task<Result<AnalyticsSummaryDto>> GetSummaryAsync(AnalyticsFilterDto filter, CancellationToken cancellationToken = default)
    {
        var resolvedFilter = ResolveFilter(filter);
        
        var submissions = BuildSubmissionsBase(resolvedFilter);
        var responses = BuildResponsesBase(resolvedFilter);

        var responseTime = await GetResponseTimeAsync(responses, cancellationToken);
        var cycleTime = await GetCycleTimeAsync(submissions, cancellationToken);
        var volume = await GetVolumeAsync(submissions, cancellationToken);
        
        return Result<AnalyticsSummaryDto>.Success(new AnalyticsSummaryDto
        {
            ResponseTime = responseTime,
            CycleTime = cycleTime,
            Volume = volume
        });
    }

    private AnalyticsFilterDto ResolveFilter(AnalyticsFilterDto filter)
    {
        if (currentUser.isSuperAdministrator)
            return filter;

        var adminDepartmentIds = currentUser.Roles
            .Where(role => role.Value == GeneralRole.Administrator)
            .Select(role => role.Key)
            .ToHashSet();

        if (filter.DepartmentId.HasValue
            && !adminDepartmentIds.Contains(filter.DepartmentId.Value))
        {
            return filter with
            {
                DepartmentId = adminDepartmentIds.FirstOrDefault()
            };
        }
        
        return filter;
    }

    private IQueryable<Data.Submission> BuildSubmissionsBase(AnalyticsFilterDto filter)
    {
        var query = context.Submissions
            .Include(s => s.Process)
            .AsQueryable();
        
        if (filter.DepartmentId.HasValue)
            query = query.Where(s => s.Step.DepartmentId == filter.DepartmentId.Value);
        else if (!currentUser.isSuperAdministrator)
        {
            var departmentIds = currentUser.Roles
                .Where(role => role.Value >= GeneralRole.Administrator)
                .Select(role => role.Key)
                .ToList();

            query = query.Where(
                submission => departmentIds.Contains(submission.Process.DepartmentId));
        }

        if (filter.ProcessId.HasValue)
            query = query.Where(submission => submission.ProcessId == filter.ProcessId.Value);
        
        if (filter.From.HasValue)
            query = query.Where(submission => submission.SubmittedOn >= filter.From.Value);
        
        if (filter.To.HasValue)
            query = query.Where(submission => submission.SubmittedOn <= filter.To.Value);
        
        return query;
    }

    private IQueryable<Response> BuildResponsesBase(AnalyticsFilterDto filter)
    {
        var query = context.Responses
            .Where(response => response.CompletedOn.HasValue &&
                               response.Result.HasValue)
            .AsQueryable();
        
        if (filter.DepartmentId.HasValue)
            query = query.Where(response => 
                response.Submission.Process.DepartmentId == filter.DepartmentId.Value);
        else if (!currentUser.isSuperAdministrator)
        {
            var departmentIds = currentUser.Roles
                .Where(role => role.Value >= GeneralRole.Administrator)
                .Select(role => role.Key)
                .ToList();
            
            query = query.Where(response => 
                departmentIds.Contains(response.Submission.Process.DepartmentId));
        }
        
        if (filter.ProcessId.HasValue)
            query = query
                .Where(response => 
                    response.Submission.ProcessId == filter.ProcessId.Value);
        
        if (filter.From.HasValue)
            query = query
                .Where(response => 
                    response.CompletedOn >= filter.From.Value);
        
        if (filter.To.HasValue)
            query = query
                .Where(response => 
                    response.CompletedOn <= filter.To.Value);
        
        return query;   
    }

    private static async Task<ResponseTimeDto> GetResponseTimeAsync(
        IQueryable<Response> responses,
        CancellationToken cancellationToken = default)
    {
        var durations = await responses
            .Select(response => new
            {
                response.Step.Title,
                Minutes = EF.Functions.DateDiffMinute(
                    response.ActivatedOn, 
                    response.CompletedOn!.Value) 
            })
            .ToListAsync(cancellationToken);

        if (durations.Count == 0)
            return new ResponseTimeDto
            {
                AverageResponseTime = 0,
                MinResponseTime = 0,
                MaxResponseTime = 0,
                ByStep = []
            };

        var byStep = durations
            .GroupBy(duration => duration.Title)
            .Select(step => new StepResponseTimeDto(
                step.Key,
                step.Average(duration => duration.Minutes),
                step.Min(duration => duration.Minutes),
                step.Max(duration => duration.Minutes),
                step.Count()))
            .OrderBy(step => step.StepName)
            .ToList();

        return new ResponseTimeDto
        {
            AverageResponseTime = Math.Round(durations.Average(duration => duration.Minutes), 2),
            MinResponseTime = durations.Min(duration => duration.Minutes),
            MaxResponseTime = durations.Max(duration => duration.Minutes),
            ByStep = byStep
        };
    }

    private static async Task<CycleTimeDto> GetCycleTimeAsync(
        IQueryable<Data.Submission> submissions,
        CancellationToken cancellationToken = default)
    {
        var terminal = await submissions
            .Where(submission =>
                submission.CompletedOn.HasValue &&
                (submission.Status == Status.Approve ||
                 submission.Status == Status.Reject))
            .Select(submission => new
            {
                Date = DateOnly.FromDateTime(submission.CompletedOn!.Value),
                Minutes = EF.Functions.DateDiffMinute(
                    submission.SubmittedOn,
                    submission.CompletedOn!.Value)
            })
            .ToListAsync(cancellationToken);

        if (terminal.Count == 0)
            return new CycleTimeDto
            {
                AverageCycleTime = 0,
                MinCycleTime = 0,
                MaxCycleTime = 0,
                ByDay = []
            };
        
        var byDay = terminal
            .GroupBy(x => x.Date)
            .Select(y => new DailyAverageDto(
                y.Key,
                Math.Round(y.Average(x => x.Minutes), 2)))
            .OrderBy(z => z.Date)
            .ToList();
        
        return new CycleTimeDto
        {
            AverageCycleTime = Math.Round(terminal.Average(x => x.Minutes), 2),
            MinCycleTime = terminal.Min(x => x.Minutes),
            MaxCycleTime = terminal.Max(x => x.Minutes),
            ByDay = byDay
        };   
    }

    private static async Task<LeadTimeDto> GetLeadTimeAsync(
        IQueryable<Data.Submission> submissions,
        CancellationToken cancellationToken = default)
    {
        var terminal = await submissions
            .Where(submission =>
                submission.CompletedOn.HasValue &&
                (submission.Status == Status.Approve ||
                 submission.Status == Status.Reject))
            .Select(submission => new
            {
                Date = DateOnly.FromDateTime(submission.CompletedOn!.Value),
                Minutes = EF.Functions.DateDiffMinute(
                    submission.SubmittedOn,
                    submission.CompletedOn!.Value)
            })
            .ToListAsync(cancellationToken);

        if (terminal.Count == 0)
            return new LeadTimeDto
            {
                AverageLeadTime = 0,
                MinLeadTime = 0,
                MaxLeadTime = 0,
                ByDay = []
            };
        
        var byDay = terminal
            .GroupBy(x => x.Date)
            .Select(y => new DailyAverageDto(
                y.Key,
                Math.Round(y.Average(x => x.Minutes), 2)))
            .OrderBy(z => z.Date)
            .ToList();
        
        return new LeadTimeDto
        {
            AverageLeadTime = Math.Round(terminal.Average(x => x.Minutes), 2),
            MinLeadTime = terminal.Min(x => x.Minutes),
            MaxLeadTime = terminal.Max(x => x.Minutes),
            ByDay = byDay
        };   
    }

    private static async Task<VolumeDto> GetVolumeAsync(
        IQueryable<Data.Submission> submissions,
        CancellationToken cancellationToken = default)
    {
        var statusCounts = await submissions
            .GroupBy(submission => submission.Status)
            .Select(x => new
            {
                Status = x.Key,
                Count = x.Count()
            })
            .ToListAsync(cancellationToken);

        var byProcess = (await submissions
            .Select(submission => new { submission.ProcessId, submission.Process.Title, submission.Status })
            .ToListAsync(cancellationToken))
            .GroupBy(x => new { x.ProcessId, x.Title })
            .Select(g => new ProcessVolumeDto(
                g.Key.ProcessId,
                g.Key.Title,
                Total: g.Count(),
                Approved: g.Count(y => y.Status == Status.Approve),
                Rejected: g.Count(y => y.Status == Status.Reject)
            ))
            .OrderByDescending(z => z.Total)
            .ToList();

        var daily = (await submissions
            .Select(submission => submission.SubmittedOn)
            .ToListAsync(cancellationToken))
            .GroupBy(d => DateOnly.FromDateTime(d))
            .Select(g => new DailyCountDto(g.Key, g.Count()))
            .OrderBy(date => date.Date)
            .ToList();

        int Count(Status status) => statusCounts.FirstOrDefault(submission =>
            submission.Status == status)?.Count ?? 0;

        var approved = Count(Status.Approve);
        var total = statusCounts.Sum(status => status.Count);

        return new VolumeDto
        {
            SubmissionCount = total,
            ApprovedCount = approved,
            RejectedCount = Count(Status.Reject),
            PendingCount = Count(Status.Review),
            CancelledCount = Count(Status.Cancel),
            ApprovalRate = total > 0 
                ? Math.Round((double)approved / total, 4) 
                : 0,
            ByProcess = byProcess,
            ByDay = daily
        };
    }
}