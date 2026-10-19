namespace DWIMS.Service.Common;

public sealed record SignatureStamp(byte[] Blob, string MimeType, string Name, DateTime? CompletedOn = null);