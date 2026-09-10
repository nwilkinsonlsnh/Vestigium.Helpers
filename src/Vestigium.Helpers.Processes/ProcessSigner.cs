using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Vestigium.Helpers.Processes;

internal static class ProcessSigner
{
    private static readonly Guid GenericVerifyV2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
    private const uint TrustENoSignature = 0x800B0100;

    internal static SignerInfo Read(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            return new SignerInfo(SignerTrust.Denied, null, null, null, null);

        X509Certificate2? cert = null;
        try
        {
            cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(imagePath));
        }
        catch
        {
            cert = null;
        }

        var trust = VerifyTrust(imagePath);
        if (cert is null)
        {
            return trust == 0
                ? new SignerInfo(SignerTrust.Verified, null, null, null, null)
                : new SignerInfo(SignerTrust.NotSigned, null, null, null, null);
        }

        using (cert)
        {
            var publisher = cert.GetNameInfo(X509NameType.SimpleName, false);
            var issuer = cert.GetNameInfo(X509NameType.SimpleName, true);
            if (DateTime.UtcNow > cert.NotAfter.ToUniversalTime())
                return new SignerInfo(SignerTrust.Expired, publisher, issuer, cert.NotBefore, cert.NotAfter);
            if (trust == 0)
                return new SignerInfo(SignerTrust.Verified, publisher, issuer, cert.NotBefore, cert.NotAfter);
            if (trust == TrustENoSignature)
                return new SignerInfo(SignerTrust.NotSigned, null, null, null, null);
            return new SignerInfo(SignerTrust.Untrusted, publisher, issuer, cert.NotBefore, cert.NotAfter);
        }
    }

    private static uint VerifyTrust(string imagePath)
    {
        var file = Marshal.StringToHGlobalUni(imagePath);
        var fileInfoPtr = nint.Zero;
        var dataPtr = nint.Zero;
        try
        {
            var fileInfo = new NativeMethods.WinTrustFileInfo
            {
                Size = (uint)Marshal.SizeOf<NativeMethods.WinTrustFileInfo>(),
                FilePath = file
            };
            fileInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.WinTrustFileInfo>());
            Marshal.StructureToPtr(fileInfo, fileInfoPtr, false);

            var data = new NativeMethods.WinTrustData
            {
                Size = (uint)Marshal.SizeOf<NativeMethods.WinTrustData>(),
                UIChoice = 2,
                RevocationChecks = 0,
                UnionChoice = 1,
                FileInfo = fileInfoPtr,
                ProvFlags = 0x10
            };
            dataPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.WinTrustData>());
            Marshal.StructureToPtr(data, dataPtr, false);
            var action = GenericVerifyV2;
            return NativeMethods.WinVerifyTrust(nint.Zero, ref action, dataPtr);
        }
        catch
        {
            return TrustENoSignature;
        }
        finally
        {
            if (dataPtr != nint.Zero)
                Marshal.FreeHGlobal(dataPtr);
            if (fileInfoPtr != nint.Zero)
                Marshal.FreeHGlobal(fileInfoPtr);
            Marshal.FreeHGlobal(file);
        }
    }
}
