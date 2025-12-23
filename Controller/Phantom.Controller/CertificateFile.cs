using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Phantom.Utils.Monads;
using Phantom.Utils.Rpc.Runtime.Tls;
using Serilog;

namespace Phantom.Controller;

sealed class CertificateFile(string name) {
	private static ILogger Logger { get; } = PhantomLogger.Create<CertificateFile>();
	
	private readonly string fileName = name + ".pfx";
	
	public async Task<RpcServerCertificate?> CreateOrLoad(string folderPath) {
		string filePath = Path.Combine(folderPath, fileName);
		
		if (File.Exists(filePath)) {
			try {
				return Read(filePath);
			} catch (IOException e) {
				Logger.Fatal(e, "Error reading certificate file: {FileName}", fileName);
				return null;
			} catch (Exception) {
				Logger.Fatal("Certificate file contains invalid data: {FileName}", fileName);
				return null;
			}
		}
		
		try {
			return await Generate(filePath);
		} catch (Exception e) {
			Logger.Fatal(e, "Error creating certificate file: {FileName}", fileName);
			return null;
		}
	}
	
	private RpcServerCertificate? Read(string filePath) {
		switch (RpcServerCertificate.Load(filePath)) {
			case Left<RpcServerCertificate, DisallowedAlgorithmError>(var rpcServerCertificate):
				Logger.Information("Loaded certificate file: {FileName}", fileName);
				return rpcServerCertificate;
			
			case Right<RpcServerCertificate, DisallowedAlgorithmError>(var error):
				Logger.Fatal("Certificate file {FileName} was expected to use {ExpectedAlgorithmName}, instead it uses {ActualAlgorithmName}.", fileName, error.ExpectedAlgorithmName, error.ActualAlgorithmName);
				return null;
		}
		
		Logger.Fatal("Certificate file could not be loaded: {FileName}", fileName);
		return null;
	}
	
	private async Task<RpcServerCertificate> Generate(string filePath) {
		byte[] certificateBytes = RpcServerCertificate.CreateAndExport("phantom-controller");
		
		await Files.WriteBytesAsync(filePath, certificateBytes, FileMode.Create, Chmod.URW_GR);
		Logger.Information("Created certificate file: {FileName}", fileName);
		
		return RpcServerCertificate.Load(filePath).RequireLeft;
	}
}
