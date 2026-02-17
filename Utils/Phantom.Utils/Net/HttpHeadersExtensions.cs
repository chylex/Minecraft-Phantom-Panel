using System.Net.Http.Headers;
using Phantom.Utils.IO;

namespace Phantom.Utils.Net;

public static class HttpHeadersExtensions {
	private const string ContentLength = "Content-Length";
	
	extension(HttpResponseHeaders headers) {
		public FileSize? ContentLength {
			get {
				if (!headers.TryGetValues(ContentLength, out var values)) {
					return null;
				}
				
				string? value = values.FirstOrDefault();
				return value != null && ulong.TryParse(value, out ulong result) ? new FileSize(result) : null;
			}
		}
	}
}
