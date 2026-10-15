namespace DWIMS.Service.Common;

public sealed record SignatureStamp(byte[] Svg, string Name, DateTime? CompletedOn = null);