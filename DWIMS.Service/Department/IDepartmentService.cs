using DWIMS.Service.Common;
using DWIMS.Service.Department.Dtos;
using DWIMS.Service.Department.Requests;

namespace DWIMS.Service.Department;

public interface IDepartmentService
{
    Task<Result<IReadOnlyList<DepartmentDto>>> GetDepartmentAsync(
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateDepartmentAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateDepartmentAsync(
        Guid id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteDepartmentAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    
    Task<Result> AssignRoleAsync(
        Guid departmentId,
        AssignRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<DepartmentMemberDto>>> GetMembersAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);
}