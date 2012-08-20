using System.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Org.BouncyCastle.Crypto.Tls;
using System.Net;

namespace Flashcards.Model.API.Https {
	public static class TlsStream {
		public static Stream Connect(NetworkStream ns, bool verifyCertificates) {
			var handler = new TlsProtocolHandler(ns);
			handler.Connect(new TlsClient(verifyCertificates));
			return handler.Stream;
		}
	}

	public class TlsClient : DefaultTlsClient {
		readonly bool verifyCertificates;
		public TlsClient(bool verifyCertificates) {
			this.verifyCertificates = verifyCertificates;
		}

		public class Authentication : TlsAuthentication {
			// Certificate changed on 14/08/2012.

			//static readonly byte[] signature = new byte [] { 
			//    0x38, 0x31, 0xcd, 0xa4, 0x7c, 0x62, 0xe4, 0x85, 0xd1, 0x38, 0xd9, 0xdd, 0x1e, 0x2d, 0x70, 0x0a, 
			//    0xb6, 0xd5, 0xe4, 0x4d, 0xf8, 0xa9, 0x28, 0x1b, 0x24, 0xe3, 0xd7, 0x54, 0x56, 0x9e, 0x81, 0x0b, 
			//    0x56, 0x3a, 0xf6, 0x5d, 0xb7, 0x76, 0xdf, 0x51, 0x31, 0xb4, 0x57, 0x86, 0x67, 0x1c, 0xcf, 0x7b, 
			//    0x25, 0xbf, 0xb9, 0xca, 0x9d, 0x96, 0xef, 0xb2, 0x89, 0xcc, 0x02, 0x97, 0x5c, 0x3c, 0xb2, 0xaa, 
			//    0x8f, 0xd0, 0x18, 0x85, 0xa4, 0x61, 0xa0, 0xce, 0x25, 0x8b, 0xa7, 0x6d, 0xd0, 0x52, 0x23, 0x08, 
			//    0xd7, 0x7d, 0xca, 0x56, 0xee, 0x68, 0x37, 0x44, 0xc9, 0xf3, 0xb8, 0x4b, 0x8e, 0xeb, 0x9d, 0xb5, 
			//    0x3b, 0x63, 0x2e, 0x19, 0xa9, 0x0c, 0x78, 0xc6, 0x59, 0x71, 0xf4, 0xbb, 0x30, 0x23, 0x5d, 0xf4, 
			//    0xab, 0xb7, 0xde, 0xd1, 0x6b, 0x30, 0xb2, 0x60, 0x0d, 0xc9, 0x11, 0x97, 0x01, 0x1a, 0xf4, 0xab, 
			//    0xad, 0x85, 0xa1, 0xde, 0x34, 0x27, 0x45, 0x07, 0xbf, 0x92, 0xff, 0x71, 0x03, 0x13, 0xec, 0xad, 
			//    0xed, 0x17, 0x1d, 0xef, 0xa0, 0xcf, 0xd9, 0xe6, 0x9d, 0xaa, 0x33, 0xa3, 0x5e, 0x02, 0x52, 0xf9, 
			//    0x45, 0xe2, 0x46, 0xf2, 0xf6, 0x30, 0xfc, 0x8d, 0x6f, 0xec, 0x0e, 0xe2, 0xb7, 0xb6, 0x50, 0x50, 
			//    0x52, 0xb0, 0x2e, 0x33, 0x0e, 0xfb, 0x53, 0x8b, 0xdb, 0xbd, 0x9a, 0x08, 0x06, 0x1b, 0x19, 0x4b, 
			//    0x13, 0xd9, 0x59, 0x3c, 0x67, 0x40, 0x33, 0x31, 0x44, 0x3d, 0xf1, 0x30, 0xac, 0xfe, 0x7c, 0xf7, 
			//    0x5c, 0xad, 0xab, 0x15, 0x66, 0xbb, 0xbe, 0x87, 0xa1, 0x23, 0x05, 0x90, 0x2a, 0xdb, 0x15, 0x9e, 
			//    0x8c, 0x56, 0x37, 0x08, 0x75, 0x86, 0x23, 0xe6, 0xf3, 0xb0, 0x13, 0x29, 0x8d, 0xf8, 0xaf, 0xed, 
			//    0x29, 0x05, 0xc7, 0xf0, 0x07, 0x52, 0xce, 0x89, 0x5c, 0xfa, 0xa4, 0x57, 0xad, 0x68, 0xe5, 0x1e
			//};

			static readonly byte[] signature = Convert.FromBase64String("qhKjRmbSnI6m/QuRhRq5zmzvMXrOlMel12Amz9mXPB6rWeYn87kAyU3b9WF4Sj2oMTaBN0sXL+ex3zet55YAIhHskQkMjP6pJBDXrqxkC2pnp3D3/TI3rZuqDFeYG9Bqj5BNhCJkjjLByKwMZGQydZ7xQhQ6BcFl/LTsxk4eRyQQ5IJOIzpCyF/uD9aQZZoiq5gSQaNkAz74eUihLTQjMGJtBPfVHSgFW7kRcddmBcC3M5thB6qlHySEkKcRllUdcyGrN7AGL9JRNEmVaeIQQiLOWtuYcxa3L3zXLCMxhy1IDs4YwDmcOfjOuJoHrE3PwhqJk1FYkjrq2SEYaFQy7Q==");

			readonly bool verifyCertificates;
			public Authentication(bool verifyCertificates) {
				this.verifyCertificates = verifyCertificates;
			}

			public void NotifyServerCertificate(Certificate serverCertificate) {
				if (!verifyCertificates)
					return;

				// TODO: Throw an exception if the certificate is invalid.
				var certs = serverCertificate.GetCerts();
				if (certs.Length > 0) {
					var now = DateTime.UtcNow;
					if (!certs[0].Signature.GetBytes().SequenceEqual(signature))
						throw new TlsException("The server certificate could not be verified.");
					if (now > certs[0].EndDate.ToDateTime() || now < certs[0].StartDate.ToDateTime())
						throw new TlsException("The server certificate has expired or is not yet valid");
				} else {
					throw new TlsException("Server certificate is invalid.");
				}
			}

			public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest) {
				return null;
			}
		}

		public override TlsAuthentication GetAuthentication() {
			return new Authentication(verifyCertificates);
		}
	}

	public class NetworkStream : Stream {
		readonly Socket socket;

		readonly byte[] readBuffer;
		int readOffset;
		int bytesInReadBuffer;
		bool eof;

		readonly AutoResetEvent readEvent;
		readonly AutoResetEvent writeEvent;

		Exception error;

		public NetworkStream(string host, int port)
			: this (ConnectSocketSync(new DnsEndPoint(host, port))) {
		}

		static Socket ConnectSocketSync(EndPoint ep) {
			var connected = new AutoResetEvent(false);
			var args = new SocketAsyncEventArgs();
			Exception error = null;
			args.RemoteEndPoint = ep;
			args.Completed +=
				(s, e) => {
					if (e.SocketError != SocketError.Success)
						error = new SocketException((int) e.SocketError);
					connected.Set();
				};

			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			if(!socket.ConnectAsync(args))
				connected.Set();

			connected.WaitOne();

			if (!socket.Connected)
				throw error ?? new SocketException();

			return socket;
		}

		public NetworkStream(Socket socket) {
			this.socket = socket;

			readEvent = new AutoResetEvent(false);
			writeEvent = new AutoResetEvent(false);

			readBuffer = new byte[1024];
			BytesRead = BytesWritten = 0;
		}

		public override void Close() {
			Dispose(true);
		}

		protected override void Dispose(bool disposing) {
			if (disposing)
				socket.Dispose();

			base.Dispose(disposing);
		}

		public override bool CanRead {
			get { return true; }
		}

		public override bool CanSeek {
			get { return false; }
		}

		public override bool CanWrite {
			get { return true; }
		}

		public override void Flush() {
			// There is no write buffer to flush.
		}

		public override long Length {
			get { throw new NotSupportedException(); }
		}

		public override long Position {
			get {
				throw new NotSupportedException();
			}
			set {
				throw new NotSupportedException();
			}
		}

		public override int Read(byte[] buffer, int offset, int count) {
			if (error != null)
				throw error;

			if (eof && BytesAvailable == 0)
				return 0;

			int totalBytesRead = 0;

			int bytesRead = FillBuffer(buffer, offset, count);
			if (bytesRead > 0) {
				offset += bytesRead;
				count -= bytesRead;
				totalBytesRead += bytesRead;
			}

			while (count > 0) {
				if (!BufferData()) {
					if (error != null)
						throw error;
					return totalBytesRead;
				}

				bytesRead = FillBuffer(buffer, offset, count);
				offset += bytesRead;
				count -= bytesRead;
				totalBytesRead += bytesRead;
			}

			return totalBytesRead;
		}

		int BytesAvailable {
			get {
				return Math.Max(0, bytesInReadBuffer - readOffset);
			}
		}

		int FillBuffer(byte[] buffer, int offset, int count) {
			if (BytesAvailable <= 0)
				return 0;

			int bytesToRead = Math.Min(count, BytesAvailable);
			Array.Copy(readBuffer, readOffset, buffer, offset, bytesToRead);
			readOffset += bytesToRead;
			return bytesToRead;
		}

		bool BufferData() {
			var args = new SocketAsyncEventArgs();
			args.SetBuffer(readBuffer, 0, readBuffer.Length);
			args.Completed += ReceivedData;
			socket.ReceiveAsync(args);

			if (!readEvent.WaitOne(ReadTimeout))
				error = new TimeoutException("NetworkStream.Read() timed out");

			return bytesInReadBuffer > 0;
		}

		void ReceivedData(object sender, SocketAsyncEventArgs e) {
			//Debug.WriteLine("NetworkStream: Received {0} bytes with status {1}", e.BytesTransferred, e.SocketError);

			if (e.SocketError == SocketError.Success && e.BytesTransferred > 0 && ((Socket) sender).Connected) {
				bytesInReadBuffer = e.BytesTransferred;
				BytesRead += e.BytesTransferred;
				readOffset = 0;
			} else {
				bytesInReadBuffer = 0;
				if (e.SocketError != SocketError.Disconnecting && e.SocketError != SocketError.Success)
					error = new SocketException((int)e.SocketError);
				else
					eof = true;
			}

			readEvent.Set();
		}

		public override long Seek(long offset, SeekOrigin origin) {
			throw new NotSupportedException();
		}

		public override void SetLength(long value) {
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count) {
			if (error != null)
				throw error;

			var args = new SocketAsyncEventArgs();
			args.SetBuffer(buffer, offset, count);
			args.Completed += DataWritten;
			socket.SendAsync(args);

			if (!writeEvent.WaitOne(WriteTimeout))
				error = new TimeoutException("NetworkStream.Write() timed out");

			if (error != null)
				throw error;
		}

		void DataWritten(object sender, SocketAsyncEventArgs e) {
			//Debug.WriteLine("NetworkStream: Wrote {0} bytes with status {1}", e.BytesTransferred, e.SocketError);

			if (e.SocketError != SocketError.Success)
				error = new SocketException((int)e.SocketError);

			BytesWritten += e.BytesTransferred;
			writeEvent.Set();
		}

		int readTimeout = -1;
		public override int ReadTimeout {
			get {
				return readTimeout;
			}
			set {
				readTimeout = value;
			}
		}

		int writeTimeout = -1;
		public override int WriteTimeout {
			get {
				return writeTimeout;
			}
			set {
				writeTimeout = value;
			}
		}

		public int BytesWritten { get; private set; }
		public int BytesRead { get; private set; }
	}
}