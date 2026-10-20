namespace DWIMS.Service.Common;

public sealed record EmailAttachment(
    byte[] Content, 
    string FileName,
    string ContentType);