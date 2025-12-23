using Phantom.Common.Data;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Runtime.Tls;
using Serilog;

namespace Phantom.Controller;

abstract class AuthTokenFile {
	private static ILogger Logger { get; } = PhantomLogger.Create<AuthTokenFile>();
	
	private readonly string fileName;
	private readonly RpcServerCertificate certificate;
	
	private AuthTokenFile(string name, RpcServerCertificate certificate) {
		this.fileName = name + ".auth";
		this.certificate = certificate;
	}
	
	public async Task<ConnectionKey?> CreateOrLoad(string folderPath) {
		string filePath = Path.Combine(folderPath, fileName);
		
		if (File.Exists(filePath)) {
			try {
				return await ReadKeyFiles(filePath);
			} catch (IOException e) {
				Logger.Fatal(e, "Error reading auth token file: {FileName}", fileName);
				return null;
			} catch (Exception) {
				Logger.Fatal("Auth token file contains invalid data: {FileName}", fileName);
				return null;
			}
		}
		
		try {
			return await GenerateKeyFiles(filePath);
		} catch (Exception e) {
			Logger.Fatal(e, "Error creating auth token file: {FileName}", fileName);
			return null;
		}
	}
	
	private async Task<ConnectionKey?> ReadKeyFiles(string filePath) {
		var authToken = AuthToken.FromBytes(await ReadKeyFile(filePath));
		Logger.Information("Loaded auth token file: {FileName}", fileName);
		
		var connectionKey = new ConnectionKey(certificate.Thumbprint, authToken);
		LogConnectionKey(TokenGenerator.EncodeBytes(connectionKey.ToBytes().AsSpan()));
		return connectionKey;
	}
	
	private static Task<byte[]> ReadKeyFile(string filePath) {
		Files.RequireMaximumFileSize(filePath, maximumBytes: 64);
		return File.ReadAllBytesAsync(filePath);
	}
	
	private async Task<ConnectionKey> GenerateKeyFiles(string filePath) {
		var authToken = AuthToken.Generate();
		
		await Files.WriteBytesAsync(filePath, authToken.ToBytes().AsMemory(), FileMode.Create, Chmod.URW_GR);
		Logger.Information("Created auth token file: {FileName}", fileName);
		
		var connectionKey = new ConnectionKey(certificate.Thumbprint, authToken);
		LogConnectionKey(TokenGenerator.EncodeBytes(connectionKey.ToBytes().AsSpan()));
		return connectionKey;
	}
	
	protected abstract void LogConnectionKey(string commonKeyEncoded);
	
	internal sealed class Web(string name, RpcServerCertificate certificate) : AuthTokenFile(name, certificate) {
		protected override void LogConnectionKey(string commonKeyEncoded) {
			Logger.Information("Web key: {WebKey}", commonKeyEncoded);
		}
	}
}
