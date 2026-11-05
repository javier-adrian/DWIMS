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
    private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;
        if (sortedValues.Count == 1) return Math.Round(sortedValues[0], 2);
        
        var rank = (int)Math.Ceiling(percentile / 100.0 * sortedValues.Count);
        var index = Math.Clamp(rank - 1, 0, sortedValues.Count - 1);
        return Math.Round(sortedValues[index], 2);
    }

    private async Task<Dictionary<Guid, (string Name, bool IsActive)>> GetUserNameMapAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0) return [];
        
        var users = await context.Users
            .IgnoreQueryFilters()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.IsDeleted })
            .ToListAsync(cancellationToken);

        return users
            .ToDictionary(
                x => x.Id, 
                x => ($"{x.FirstName} {x.LastName}".Trim(), !x.IsDeleted));
    }
    
    public async Task<Result<AnalyticsSummaryDto>> GetSummaryAsync(AnalyticsFilterDto filter, CancellationToken cancellationToken = default)
    {
        AnalyticsFilterDto resolvedFilter = filter;
        
        var submissions = BuildSubmissionsBase(resolvedFilter);
        var responses = BuildResponsesBase(resolvedFilter);

        var responseTime = await GetResponseTimeAsync(responses, cancellationToken);
        var cycleTime = await GetCycleTimeAsync(submissions, cancellationToken);
        var volume = await GetVolumeAsync(submissions, cancellationToken);
        
        var reviewers = await GetReviewerPerformanceAsync(resolvedFilter, cancellationToken);
        var pendingAging = await GetPendingAgingAsync(resolvedFilter, cancellationToken);
        
        return Result<AnalyticsSummaryDto>.Success(new AnalyticsSummaryDto
        {
            ResponseTime = responseTime,
            CycleTime = cycleTime,
            Volume = volume,
            Reviewers = reviewers,
            PendingAging = pendingAging
        });
    }

    private IQueryable<Data.Submission> BuildSubmissionsBase(AnalyticsFilterDto filter)
    {
        var query = context.Submissions
            .Include(s => s.Process)
            .AsQueryable();
        
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
                               (response.Result == Status.Approve || 
                                response.Result == Status.Reject) &&
                               !response.IsDeleted &&
                               !response.Step.IsDeleted &&
                               !response.Submission.IsDeleted)
            .AsQueryable();
        
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
        
        var sorted = durations
            .Select(duration => (double)duration.Minutes)
            .OrderBy(v => v)
            .ToList();
        
        var byStep = durations
            .GroupBy(duration => duration.Title)
            .Select(step =>
            {
                var groupSorted = step
                    .Select(d => (double)d.Minutes)
                    .OrderBy(d => d)
                    .ToList();
                
                return new StepResponseTimeDto(
                    step.Key,
                    step.Average(duration => duration.Minutes),
                    Percentile(groupSorted, 50),
                    step.Min(duration => duration.Minutes),
                    step.Max(duration => duration.Minutes),
                    step.Count());
            })
            .OrderBy(step => step.StepName)
            .ToList();

        return new ResponseTimeDto
        {
            MedianResponseTime = Percentile(sorted, 50),
            P90ResponseTime = Percentile(sorted, 90),
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
        
        var sorted = terminal
            .Select(x => (double)x.Minutes)
            .OrderBy(x => x)
            .ToList();

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
            MedianCycleTime = Percentile(sorted, 50),
            P90CycleTime = Percentile(sorted, 90),
            AverageCycleTime = Math.Round(terminal.Average(x => x.Minutes), 2),
            MinCycleTime = terminal.Min(x => x.Minutes),
            MaxCycleTime = terminal.Max(x => x.Minutes),
            ByDay = byDay
        };   
    }

    // private static async Task<LeadTimeDto> GetLeadTimeAsync(
    //     IQueryable<Data.Submission> submissions,
    //     CancellationToken cancellationToken = default)
    // {
    //     var terminal = await submissions
    //         .Where(submission =>
    //             submission.CompletedOn.HasValue &&
    //             (submission.Status == Status.Approve ||
    //              submission.Status == Status.Reject))
    //         .Select(submission => new
    //         {
    //             Date = DateOnly.FromDateTime(submission.CompletedOn!.Value),
    //             Minutes = EF.Functions.DateDiffMinute(
    //                 submission.SubmittedOn,
    //                 submission.CompletedOn!.Value)
    //         })
    //         .ToListAsync(cancellationToken);
    //
    //     if (terminal.Count == 0)
    //         return new LeadTimeDto
    //         {
    //             AverageLeadTime = 0,
    //             MinLeadTime = 0,
    //             MaxLeadTime = 0,
    //             ByDay = []
    //         };
    //     
    //     var byDay = terminal
    //         .GroupBy(x => x.Date)
    //         .Select(y => new DailyAverageDto(
    //             y.Key,
    //             Math.Round(y.Average(x => x.Minutes), 2)))
    //         .OrderBy(z => z.Date)
    //         .ToList();
    //     
    //     return new LeadTimeDto
    //     {
    //         AverageLeadTime = Math.Round(terminal.Average(x => x.Minutes), 2),
    //         MinLeadTime = terminal.Min(x => x.Minutes),
    //         MaxLeadTime = terminal.Max(x => x.Minutes),
    //         ByDay = byDay
    //     };   
    // }

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
        // Approval rate is decided-only: approved / (approved + rejected).
        // Pending and cancelled say nothing about the outcome mix yet.
        var decided = approved + Count(Status.Reject);

        return new VolumeDto
        {
            SubmissionCount = total,
            ApprovedCount = approved,
            RejectedCount = Count(Status.Reject),
            PendingCount = Count(Status.Review) + Count(Status.OnHold),
            CancelledCount = Count(Status.Cancel),
            ApprovalRate = decided > 0
                ? Math.Round((double)approved / decided, 4)
                : 0,
            ByProcess = byProcess,
            ByDay = daily
        };
    }
    
    private async Task<IReadOnlyList<ReviewerPerformanceDto>> GetReviewerPerformanceAsync(
        AnalyticsFilterDto filter,
        CancellationToken cancellationToken)
    {
        var completed = await BuildResponsesBase(filter)
            .Where(response => response.AssigneeId.HasValue)
            .Select(response => new
            {
                response.AssigneeId,
                Minutes = EF.Functions.DateDiffMinute(
                    response.ActivatedOn,
                    response.CompletedOn!.Value)
            })
            .ToListAsync(cancellationToken);
        
        var open = await context.Responses
            .Where(response => !response.IsDeleted &&
                               response.CompletedOn == null &&
                               !response.Submission.IsDeleted &&
                               (response.Submission.Status == Status.Review ||
                                response.Submission.Status == Status.OnHold) &&
                               response.AssigneeId.HasValue)
            .Where(response => 
                filter.ProcessId == null || 
                response.Submission.ProcessId == filter.ProcessId)
            .GroupBy(response => response.AssigneeId!.Value)
            .Select(group => new {AssigneeId = group.Key, Count = group.Count()})
            .ToListAsync(cancellationToken);
        
        var openByReviewer = open.ToDictionary(
            x => x.AssigneeId, 
            x => x.Count);
        
        var reviewerIds = completed
            .Select(x => x.AssigneeId!.Value)
            .Concat(open.Select(x => x.AssigneeId))
            .Distinct()
            .ToList();
        
        var names = await GetUserNameMapAsync(reviewerIds, cancellationToken);
        
        return reviewerIds
            .Select(id => 
            {
                var minutes = completed
                    .Where(x => x.AssigneeId == id)
                    .Select(x => (double)x.Minutes)
                    .OrderBy(m => m)
                    .ToList();
                
                names.TryGetValue(id, out var user);

                return new ReviewerPerformanceDto(
                    id,
                    user.Name is { Length: > 0 } ? user.Name : "(unknown user)",
                    user.IsActive, 
                    openByReviewer.GetValueOrDefault(id),
                    minutes.Count(),
                    minutes.Count > 0 ? Math.Round(minutes.Average(), 2) : 0,
                    Percentile(minutes, 50),
                    Percentile(minutes, 90));
            })
            .OrderByDescending(reviewer => reviewer.OpenAssignments)
            .ThenByDescending(reviewer => reviewer.CompletedReviews)
            .ToList();
    }

    private async Task<PendingAgingDto> GetPendingAgingAsync(
        AnalyticsFilterDto filter,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        
        var pending = await context.Responses
            .Where(response => !response.IsDeleted &&
                               response.CompletedOn == null &&
                               !response.Submission.IsDeleted && 
                               (response.Submission.Status == Status.Review ||
                                response.Submission.Status == Status.OnHold))
            .Where(response => filter.ProcessId == null || response.Submission.ProcessId == filter.ProcessId)
            .Select(response => new
            {
                response.Submission.Status,
                response.ActivatedOn,
                ProcessName = response.Submission.Process.Title,
                StepName = response.Step.Title,
                response.AssigneeId
            })
            .ToListAsync(cancellationToken);
        
        var names = await GetUserNameMapAsync(
            pending
                .Where(p => p.AssigneeId.HasValue)
                .Select(p => p.AssigneeId!.Value).Distinct().ToList(),
            cancellationToken);
        
        double WaitingHours(DateTime activatedOn) => Math.Max(0, (now - activatedOn).TotalHours);
        
        var hours = pending.Select(p => WaitingHours(p.ActivatedOn)).ToList();
        
        var oldest = pending
            .OrderBy(p => p.ActivatedOn)
            .Take(5)
            .Select(p => new PendingItemDto(
                p.ProcessName,
                p.StepName,
                p.AssigneeId.HasValue && names.TryGetValue(p.AssigneeId.Value, out var assignee)
                    ? assignee.Name
                    : "(unassigned)",
                Math.Round(WaitingHours(p.ActivatedOn), 1),
                p.Status == Status.OnHold))
            .ToList();

        return new PendingAgingDto
        {
            Buckets =
            [
                new AgingBucketDto("0-1 day", hours.Count(h => h < 24)),
                new AgingBucketDto("1-3 days", hours.Count(h => h >= 24 && h < 72)),
                new AgingBucketDto("3-7 days", hours.Count(h => h >= 72 && h < 168)),
                new AgingBucketDto("7+ days", hours.Count(h => h >= 168)),
            ],
            Oldest = oldest,
            TotalPending = pending.Count
        };
    }
}