namespace DWIMS.Service.Analytics.Dtos;

public sealed record AgingBucketDto(
    string Label,
    int Count);