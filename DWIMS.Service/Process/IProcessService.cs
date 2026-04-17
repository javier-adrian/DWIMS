using DWIMS.Service.Common;
using DWIMS.Service.Process.Dtos;
using DWIMS.Service.Process.Requests;

namespace DWIMS.Service.Process;

public interface IProcessService
{
    Task<Result<IReadOnlyList<ProcessSummaryDto>>> GetProcessesAsync(
        CancellationToken cancellationToken = default);

    Task<Result<ProcessDetailDto>> GetProcessAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateProcessAsync(
        CreateProcessRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateProcessAsync(
        Guid id,
        UpdateProcessRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteProcessAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> AddStepAsync(
        Guid processId,
        AddStepRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateStepAsync(
        Guid processId,
        Guid stepId,
        UpdateStepRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteStepAsync(
        Guid processId,
        Guid stepId,
        CancellationToken cancellationToken = default);
    
    Task<Result<Guid>> AddFieldAsync(
        Guid processId,
        AddFieldRequest request,
        CancellationToken cancellationToken = default);
}