using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Phantom.Utils.Monads;

namespace Phantom.Utils.Rpc.Runtime.Tls;

/// <summary>
/// <para>
/// .NET uses the operating system's native TLS implementation, which is much worse on Windows than it is on Linux.
/// </para>
/// <para>
/// On Linux, the client and server will use TLS 1.3 with ECDSA. On other operating systems, the requirements are reduced for the purposes of local development.
/// The client and server are not designed to be able to communicate if they run on different operating systems.
/// </para>
/// </summary>
static class TlsSupport {
	public static SslProtocols SupportedProtocols => OperatingSystem.IsLinux() ? SslProtocols.Tls13 : SslProtocols.None /* OS default */;
	
	public static X509Certificate2 CreateSelfSignedCertificate(X500DistinguishedName distinguishedName) {
		if (OperatingSystem.IsLinux()) {
			using var keys = ECDsa.Create();
			return CreateSelfSignedCertificate(new CertificateRequest(distinguishedName, keys, HashAlgorithmName.SHA512));
		}
		else {
			using var keys = RSA.Create(keySizeInBits: 4096);
			return CreateSelfSignedCertificate(new CertificateRequest(distinguishedName, keys, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1));
		}
	}
	
	private static X509Certificate2 CreateSelfSignedCertificate(CertificateRequest request) {
		return request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.MaxValue);
	}
	
	public static Either<X509Certificate2, DisallowedAlgorithmError> LoadPkcs12FromFile(string path) {
		X509KeyStorageFlags storageFlags = OperatingSystem.IsLinux() ? X509KeyStorageFlags.EphemeralKeySet : X509KeyStorageFlags.DefaultKeySet;
		X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(path, password: null, storageFlags);
		
		if (CheckAlgorithm(certificate) is {} unsupportedCertificateAlgorithm) {
			return Either.Right(unsupportedCertificateAlgorithm);
		}
		else {
			return Either.Left(certificate);
		}
	}
	
	public static DisallowedAlgorithmError? CheckAlgorithm(X509Certificate2 certificate) {
		if (OperatingSystem.IsLinux() && certificate.GetECDsaPublicKey() == null) {
			string actualAlgorithm = certificate.GetKeyAlgorithm();
			Oid actualAlgorithmOid = Oid.FromOidValue(actualAlgorithm, OidGroup.PublicKeyAlgorithm);
			return new DisallowedAlgorithmError("ECC", actualAlgorithmOid.FriendlyName ?? actualAlgorithm);
		}
		
		return null;
	}
}
