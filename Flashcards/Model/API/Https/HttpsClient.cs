using System.Linq;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Flashcards.Model.API.Https {
	public class HttpException : IOException {
		public HttpException() { }
		public HttpException(string message) : base(message) { }
		public HttpException(string message, Exception inner) : base(message, inner) { }
	}

	public class HttpsRequest {
		public string Method { get; set; }
		public string Path { get; set; }

		public string ContentType { get; set; }
		public string Authorization { get; set; }
		public byte[] PostData { get; set; }
		public string UserAgent { get; set; }

		public HttpsRequest(string method, string path) {
			Method = method;
			Path = path;
		}

		public void BasicAuthorization(string userName, string userPassword) {
			Authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + userPassword));
		}
	}

	public class ContentType {
		public string MediaType { get; set; }
		public string CharSet { get; set; }

		public ContentType(string httpSpec) {
			var match = Regex.Match(httpSpec, "^([^;]+)(;\\s*charset=(.+))?$");
			if (match.Success) {
				MediaType = match.Groups[1].Value;
				if (match.Groups[3].Success)
					CharSet = match.Groups[3].Value;
			} else {
				throw new HttpException("The Content-Type header of the response was invalid.");
			}
		}
	}

	public enum TransferEncoding {
		None,
		Chunked
	}

	static class StreamExtensions {
		public static void CopyToSafe(this Stream src, Stream dest, int bufferSize = 1024) {
			src.CopyNSafe(-1, dest, bufferSize);
		}

		public static void CopyNSafe(this Stream src, int n, Stream dest, int bufferSize = 1024) {
			//if (!src.CanRead)
			//	throw new ArgumentException("CopyStream: Cannot read from source stream", "src");
			if (!dest.CanWrite)
				throw new ArgumentException("CopyStream: Cannot write to destination stream", "dest");

			var buffer = new byte[bufferSize];

			while (n != 0) {
				int bytesToRead = bufferSize;
				if (n > 0 && n < bufferSize)
					bytesToRead = n;

				int bytesInBuffer;
				try {
					bytesInBuffer = src.Read(buffer, 0, bytesToRead);
				} catch (EndOfStreamException) {
					bytesInBuffer = 0;
				}

				if (bytesInBuffer == 0)
					break;

				dest.Write(buffer, 0, bytesInBuffer);
				if (n > 0)
					n -= bytesInBuffer;
			}
		}

		// Uses only CRLF newlines.
		public static string ReadLineUTF8(this Stream stream) {
			var ms = new MemoryStream();
			int last = 0;
			while (true) {
				int b = stream.ReadByte();
				if (last == 13 && b == 10)
					break;
				if (last == 13)
					ms.WriteByte(13);

				if (b == -1)
					break;

				// Delay writing of CR until we see if the next char is LF.
				if (b != 13)
					ms.WriteByte((byte)b);
				last = b;
			}

			ms.Position = 0;
			return new StreamReader(ms, Encoding.UTF8).ReadToEnd();
		}
	}

	public class HttpsResponse {
		public ContentType ContentType { get; private set; }
		public int ContentLength { get; set; }
		public int StatusCode { get; set; }
		public byte[] Body { get; set; }
		public TransferEncoding TransferEncoding { get; set; }

		public HttpsResponse() {
			TransferEncoding = TransferEncoding.None;
		}

		public string DecodeTextBody() {
			if (Body == null)
				return null;

			if (ContentType == null)
				throw new HttpException("The Content-Type of the response message body was not specified.");

			// application/json... is a counter-example.
			//if (!ContentType.MediaType.ToLowerInvariant().StartsWith("text/"))
			//    throw new HttpException("Tried to decode a non-textual response body as text.");

			string charset = ContentType.CharSet ?? "ISO-8859-1";
			Encoding encoding;
			try { 
				encoding = Encoding.GetEncoding(charset);
			} catch (ArgumentException e) {
				throw new HttpException("Unknown response body charset: " + charset, e);
			}

			return new string(encoding.GetChars(Body));
		}

		public void AddHeader(string field, string value) {
			switch (field.ToLowerInvariant()) {
				case "content-type": ContentType = new ContentType(value); break;
				case "transfer-encoding":
					switch (value.ToLowerInvariant()) {
						case "chunked":
							TransferEncoding = TransferEncoding.Chunked;
							break;
					}
					break;
				case "content-length":
					int len;
					if (int.TryParse(value, out len)) {
						if (len < 0)
							throw new HttpException("ContentLength was negative.");

						ContentLength = len;
					} else {
						throw new HttpException("ContentLength was in an invalid format.");
					}
					break;
			}
		}

		public void SetBody(MemoryStream ms) {
			// Read the entity body from the stream in the response's transfer encoding.
			switch (TransferEncoding) {
				case TransferEncoding.Chunked:
					Body = ReadChunked(ms);
					break;
				default:
					var body = new MemoryStream();
					ms.CopyTo(body);
					Body = body.ToArray();
					break;
			}
		}

		private byte[] ReadChunked(MemoryStream ms) {
			var body = new MemoryStream();
			while (true) {
				string chunkLenSpec = ms.ReadLineUTF8();
				int chunkLen = Convert.ToInt32(chunkLenSpec, 16);
				
				// No need to process the trailer. It can only contain metadata which we probably don't need.
				if (chunkLen <= 0)
					return body.ToArray();

				ms.CopyNSafe(chunkLen, body);
				var emptyLine = ms.ReadLineUTF8();
				if (!string.IsNullOrEmpty(emptyLine))
					throw new HttpException("The response body was chunked incorrectly");
			}
		}
	}

	class NoCloseStreamWriter : StreamWriter {
		public NoCloseStreamWriter(Stream str) : base(str) { }
		public NoCloseStreamWriter(Stream str, Encoding encoding) : base(str, encoding) { }

		protected override void Dispose(bool disposing) {
			base.Flush();

			// Don't close the stream.
			base.Dispose(false);
		}
	}

	public class HttpsClient {
		readonly string host;
		readonly Stream tlsStream;

		public HttpsClient(string host, int port, bool verifyCertificates) {
			this.host = host;
			tlsStream = TlsStream.Connect(new NetworkStream(host, port), verifyCertificates);
		}

		public HttpsResponse MakeRequest(HttpsRequest request) {
			using (var writer = new NoCloseStreamWriter(tlsStream, Encoding.GetEncoding("ISO-8859-1"))) {
				writer.NewLine = "\r\n";
				writer.WriteLine(request.Method + " " + request.Path + " HTTP/1.0");

				writer.WriteLine("Host: " + host);

				if (request.PostData != null) {
					if (request.ContentType == null)
						throw new HttpException("HttpsRequest did not specifify a ContentType");
					
					writer.WriteLine("Content-Type: " + request.ContentType);
					writer.WriteLine("Content-Length: " + request.PostData.Length);
				}

				if (request.Authorization != null)
					writer.WriteLine("Authorization: " + request.Authorization);

				writer.WriteLine();

				if (request.PostData != null) {
					//writer.WriteLine();
					writer.Flush();
					tlsStream.Write(request.PostData, 0, request.PostData.Length);
				}
			}

			var response = new HttpsResponse();

			var ms = new MemoryStream();
			tlsStream.CopyToSafe(ms);
			ms.Position = 0;

			var statusLine = ms.ReadLineUTF8();
			var parts = statusLine.Split();
			if (parts.Length < 2 || !parts[0].StartsWith("HTTP"))
				throw new HttpException("Invalid HTTP response");

			int statusCode;
			if(!int.TryParse(parts[1], out statusCode))
				throw new HttpException("Invalid HTTP response");
			response.StatusCode = statusCode;

			while(true) {
				var line = ms.ReadLineUTF8();
				if (line.Length == 0)
					break;

				var match = Regex.Match(line, "^([^:]+):\\s*(.*)$");
				if (match.Success)
					response.AddHeader(match.Groups[1].Value, match.Groups[2].Value);
			}

			response.SetBody(ms);

			tlsStream.Dispose();

			return response;
		}
	}
}