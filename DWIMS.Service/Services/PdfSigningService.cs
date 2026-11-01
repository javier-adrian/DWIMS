using System.Security.Cryptography.X509Certificates;
using DWIMS.Service.Common;
using DWIMS.Service.Logs;
using iText.Bouncycastle.Crypto;
using iText.Bouncycastle.X509;
using iText.Commons.Bouncycastle.Cert;
using iText.Kernel.Crypto;
using iText.Kernel.Pdf;
using iText.Signatures;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Pkcs;

namespace DWIMS.Service.Services;

public class PdfSigningService(
    IOptions<PdfSigningOptions> options,
    ILogService logService) : IPdfSigningService
{
    private readonly object _gate = new();
    
    public byte[]? Seal(byte[] pdf)
    {
        var settings = options.Value;

        if (string.IsNullOrEmpty(settings.CertificatePath))
            return null;
        
        lock (_gate)
        {
            var tsa = string.IsNullOrEmpty(settings.TsaUrl)
                ? null
                : new TSAClientBouncyCastle(settings.TsaUrl);

            if (tsa is not null)
            {
                try
                {
                    return Sign(pdf, settings, tsa);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    logService.LogAsync(
                        "PDF Seal Timestamp Failed",
                        "Submission",
                        Guid.Empty,
                        $"TSA {settings.TsaUrl} unreachable - sealing without timestamp. {e.Message}").Wait();
                }
            }
            
            return Sign(pdf, settings, null);
        }
    }
    
    private static byte[] Sign(byte[] pdf, PdfSigningOptions options, TSAClientBouncyCastle? tsa)
    {
        using var pfx = File.OpenRead(options.CertificatePath!);
        var store = new Pkcs12StoreBuilder().Build();
        store.Load(pfx, (options.CertificatePassword ?? string.Empty).ToCharArray());
        
        var alias = store.Aliases.OfType<string>().First(a => store.IsKeyEntry(a));
        var key = new PrivateKeyBC(store.GetKey(alias).Key);
        var chain = store.GetCertificateChain(alias)
            .Select(entry => new X509CertificateBC(entry.Certificate))
            .Cast<IX509Certificate>()
            .ToArray();
        
        using var reader = new PdfReader(new MemoryStream(pdf, writable: false));
        using var destination = new MemoryStream();
        
        var signer = new PdfSigner(reader, destination, new StampingProperties().UseAppendMode());

        signer.SetSignerProperties(new SignerProperties()
            .SetCertificationLevel(AccessPermissions.NO_CHANGES_PERMITTED)
            .SetSignatureCreator("DWIMS")
            .SetReason("Document issuance seal"));
        
        signer.SignDetached(
            new BouncyCastleDigest(),
            new PrivateKeySignature(key, DigestAlgorithms.SHA256),
            chain,
            null,
            null,
            tsa,
            16384,
            PdfSigner.CryptoStandard.CMS);
        
        return destination.ToArray();
    }
}