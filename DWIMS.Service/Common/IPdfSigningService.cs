namespace DWIMS.Service.Common;

public interface IPdfSigningService
{
    byte[]? Seal(byte[] pdf);
}