using DWIMS.Service.Process.Dtos;
using DWIMS.Service.Routing.Dtos;
using Microsoft.AspNetCore.Http;

namespace DWIMS.Service.Process.Requests;

public sealed record CreateDocumentSubmissionRequest
(
    string ProcessId,
    IFormFile File,
    IReadOnlyList<RouteEntryDto> Route,
    IReadOnlyList<SlotEntryDto> Slots
);