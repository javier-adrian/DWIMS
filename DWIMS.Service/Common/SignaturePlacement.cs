using DWIMS.Data;

namespace DWIMS.Service.Common;

public sealed record SignaturePlacement(
    Response Response,
    SignatureStamp Stamp);