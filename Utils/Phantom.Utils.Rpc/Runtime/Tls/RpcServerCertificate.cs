using System.Security.Cryptography.X509Certificates;
using Phantom.Utils.Monads;

namespace Phantom.Utils.Rpc.Runtime.Tls;

public sealed class RpcServerCertificate {
	public static byte[] CreateAndExport(string commonName) {
		var distinguishedNameBuilder = new X500DistinguishedNameBuilder();
		distinguishedNameBuilder.AddCommonName(commonName);
		var distinguishedName = distinguishedNameBuilder.Build();
		
		using var certificate = TlsSupport.CreateSelfSignedCertificate(distinguishedName);
		return certificate.Export(X509ContentType.Pkcs12);
	}
	
	public static Either<RpcServerCertificate, DisallowedAlgorithmError> Load(string path) {
		return TlsSupport.LoadPkcs12FromFile(path).MapLeft(static certificate => new RpcServerCertificate(certificate));
	}
	
	internal X509Certificate2 Certificate { get; }
	
	public RpcCertificateThumbprint Thumbprint => RpcCertificateThumbprint.From(Certificate);
	
	private RpcServerCertificate(X509Certificate2 certificate) {
		this.Certificate = certificate;
	}
}
