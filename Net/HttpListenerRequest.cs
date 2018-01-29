#region License
/*
 * HttpListenerRequest.cs
 *
 * This code is derived from HttpListenerRequest.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
 * Copyright (c) 2012-2015 sta.blockhead
 * Copyright � 2016 Nivloc Enterprises Ltd
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
#endregion

#region Authors
/*
 * Authors:
 * - Gonzalo Paniagua Javier <gonzalo@novell.com>
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
#if SSHARP
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Cryptography.X509Certificates;
using IAsyncResult = Crestron.SimplSharp.CrestronIO.IAsyncResult;
using AsyncCallback = Crestron.SimplSharp.CrestronIO.AsyncCallback;
using SSMono;
using SSMono.Net;
using SSMono.Web;
using StreamReader = SSMono.IO.StreamReader;
#else
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net;
#endif
using System.Text;

namespace WebSocketSharp.Net
	{
	/// <summary>
	/// Represents an incoming request to a <see cref="HttpListener"/> instance.
	/// </summary>
	/// <remarks>
	/// This class cannot be inherited.
	/// </remarks>
	public class HttpListenerRequest
		{
		#region Private Fields

		private static readonly byte[] _100continue;
		private string[] _acceptTypes;
		private bool _chunked;
		private HttpConnection _connection;
		private Encoding _contentEncoding;
		private byte[] _content;
		private string _contentType;
		private long _contentLength;
		private HttpListenerContext _context;
		private CookieCollection _cookies;
		private WebHeaderCollection _headers;
		private string _httpMethod;
		private Stream _inputStream;
		private Version _protocolVersion;
		private NameValueCollection _queryString;
		private Guid _requestTraceIdentifier;
		private string _uri;
		private Uri _url;
		private Uri _urlReferer;
		private string[] _userLanguages;
		private bool _websocketRequest;
		private bool _websocketRequestSet;

		#endregion

		#region Static Constructor

		static HttpListenerRequest ()
			{
			_100continue = Encoding.ASCII.GetBytes ("HTTP/1.1 100 Continue\r\n\r\n");
			}

		#endregion

		#region protected Constructors

		internal HttpListenerRequest (HttpListenerContext context)
			{
			_context = context;

			_connection = context.Connection;
			_contentLength = -1;
			_headers = new WebHeaderCollection ();
			_requestTraceIdentifier = Guid.NewGuid ();
			}

		protected internal HttpListenerRequest (HttpListenerRequest request)
			{
			_acceptTypes = request._acceptTypes;
			_chunked = request._chunked;
			_connection = request._connection;
			_contentEncoding = request._contentEncoding;
			_content = request._content;
			_contentType = request._contentType;
			_contentLength = request._contentLength;
			_context = request._context;
			_cookies = request._cookies;
			_headers = request._headers;
			_requestTraceIdentifier = request._requestTraceIdentifier;
			_inputStream = request._inputStream;
			_httpMethod = request._httpMethod;
			_queryString = request._queryString;
			_urlReferer = request._urlReferer;
			_uri = request._uri;
			_url = request._url;
			_userLanguages = request._userLanguages;
			_protocolVersion = request._protocolVersion;
			_websocketRequest = request._websocketRequest;
			_websocketRequestSet = request._websocketRequestSet;
			}

		#endregion

		#region Public Properties

		/// <summary>
		/// Gets the media types that are acceptable for the client.
		/// </summary>
		/// <value>
		///   <para>
		///   An array of <see cref="string"/> that contains the names of the media
		///   types specified in the value of the Accept header.
		///   </para>
		///   <para>
		///   <see langword="null"/> if the Accept header is not present.
		///   </para>
		/// </value>
		public string[] AcceptTypes
			{
			get
				{
				var val = _headers["Accept"];
				if (val == null)
					return null;

				if (_acceptTypes == null)
					_acceptTypes = val.SplitHeaderValue (',').Trim ().ToArray ();

				return _acceptTypes;
				}
			}

		/// <summary>
		/// Gets an error code that identifies a problem with the certificate
		/// provided by the client.
		/// </summary>
		/// <value>
		/// An <see cref="int"/> that represents an error code.
		/// </value>
		/// <exception cref="NotSupportedException">
		/// This property is not supported.
		/// </exception>
		public int ClientCertificateError
			{
			get
				{
				throw new NotSupportedException ();
				}
			}

		/// <summary>
		/// Gets the the entity body data included in the request.
		/// </summary>
		/// <value>
		/// A <see cref="Byte"/> that contains the entity body data,
		/// </value>
		public byte[] Content
			{
			get
				{
				if (_content != null)
					return _content;

				var stream = InputStream;

				if (stream == Stream.Null)
					return new byte[0];

				using (var br = new BinaryReader (stream))
					{
					return (_content = br.ReadBytes ((int)ContentLength64));
					}
				}
			}

		/// <summary>
		/// Gets the the entity body data included in the request as a string.
		/// </summary>
		/// <value>
		/// A <see cref="String"/> that contains the entity body data
		/// </value>
		public string ContentString
			{
			get
				{
				return ContentEncoding.GetString (Content);
				}
			}

		/// <summary>
		/// Gets the encoding for the entity body data included in the request.
		/// </summary>
		/// <value>
		///   <para>
		///   A <see cref="Encoding"/> from the charset value of the Content-Type
		///   header.
		///   </para>
		///   <para>
		///   <see cref="Encoding.UTF8"/> if the charset value is not available.
		///   </para>
		/// </value>
		public Encoding ContentEncoding
			{
			get
				{
				return _contentEncoding ?? Encoding.UTF8;
				}
			/*
			set
				{
				_contentEncoding = value;
				}
			*/
			}

		/// <summary>
		/// Gets the length in bytes of the entity body data included in
		/// the request.
		/// </summary>
		/// <value>
		///   <para>
		///   A <see cref="long"/> from the value of the Content-Length header.
		///   </para>
		///   <para>
		///   -1 if the value is not known.
		///   </para>
		/// </value>
		public long ContentLength64
			{
			get
				{
				return _contentLength;
				}
			}

		/// <summary>
		/// Gets the media type of the entity body data included in the request.
		/// </summary>
		/// <value>
		/// A <see cref="string"/> that represents the value of the Content-Type
		/// header.
		/// </value>
		public string ContentType
			{
			get
				{
				return _contentType;
				}
			/*
			set
				{
				_contentType = value.Trim ().ToLower ();
				}
			*/
			}

		/// <summary>
		/// Gets the cookies included in the request.
		/// </summary>
		/// <value>
		///   <para>
		///   A <see cref="CookieCollection"/> that contains the cookies.
		///   </para>
		///   <para>
		///   An empty collection if not included.
		///   </para>
		/// </value>
		public CookieCollection Cookies
			{
			get
				{
				if (_cookies == null)
					_cookies = _headers.GetCookies (false);

				return _cookies;
				}
			}

		/// <summary>
		/// Gets a value indicating whether the request has the entity body data.
		/// </summary>
		/// <value>
		/// <c>true</c> if the request has the entity body data; otherwise,
		/// <c>false</c>.
		/// </value>
		public bool HasEntityBody
			{
			get
				{
				return _contentLength > 0 || _chunked;
				}
			}

		/// <summary>
		/// Gets the headers included in the request.
		/// </summary>
		/// <value>
		/// A <see cref="NameValueCollection"/> that contains the headers.
		/// </value>
		public NameValueCollection Headers
			{
			get
				{
				return _headers;
				}
			}

		/// <summary>
		/// Gets the HTTP method specified by the client.
		/// </summary>
		/// <value>
		/// A <see cref="string"/> that represents the HTTP method specified in
		/// the request line.
		/// </value>
		public string HttpMethod
			{
			get
				{
				return _httpMethod ?? (_httpMethod = _context.Request.HttpMethod);
				}
			}

		/// <summary>
		/// Gets a stream that contains the entity body data included in
		/// the request.
		/// </summary>
		/// <value>
		///   <para>
		///   A <see cref="Stream"/> that contains the entity body data.
		///   </para>
		///   <para>
		///   <see cref="Stream.Null"/> if not included.
		///   </para>
		/// </value>
		public Stream InputStream
			{
			get
				{
				if (!HasEntityBody)
					return Stream.Null;

				if (_inputStream == null)
					{
					_inputStream = _connection.GetRequestStream (_contentLength, _chunked);
					}

				return _inputStream;
				}
			}

		/// <summary>
		/// Gets a value indicating whether the client that sent the request is authenticated.
		/// </summary>
		/// <value>
		/// Gets a value indicating whether the client is authenticated.
		/// </value>
		public bool IsAuthenticated
			{
			get
				{
				return _context.User != null;
				}
			}

		/// <summary>
		/// Gets a value indicating whether the request is sent from the local computer.
		/// </summary>
		/// <value>
		/// <c>true</c> if the request is sent from the same computer as the server;
		/// otherwise, <c>false</c>.
		/// </value>
		public bool IsLocal
			{
			get
				{
				return _connection.IsLocal;
				}
			}

		/// <summary>
		/// Gets a value indicating whether a secure connection is used to send
		/// the request.
		/// </summary>
		/// <value>
		/// <c>true</c> if the connection is a secure connection; otherwise,
		/// <c>false</c>.
		/// </value>
		public bool IsSecureConnection
			{
			get
				{
				return _connection.IsSecure;
				}
			}

		/// <summary>
		/// Gets a value indicating whether the request is a WebSocket handshake
		/// request.
		/// </summary>
		/// <value>
		/// <c>true</c> if the request is a WebSocket handshake request; otherwise,
		/// <c>false</c>.
		/// </value>
		public bool IsWebSocketRequest
			{
			get
				{
				if (!_websocketRequestSet)
					{
					_websocketRequest = _httpMethod == "GET"
											&& _protocolVersion > HttpVersion.Version10
											&& _headers.Upgrades ("websocket");

					_websocketRequestSet = true;
					}

				return _websocketRequest;
				}
			}

		/// <summary>
		/// Gets a value indicating whether a persistent connection is requested.
		/// </summary>
		/// <value>
		/// <c>true</c> if the request specifies that the connection is kept open;
		/// otherwise, <c>false</c>.
		/// </value>
		public bool KeepAlive
			{
			get
				{
				return _headers.KeepsAlive (_protocolVersion);
				}
			}

		/// <summary>
		/// Gets the endpoint to which the request is sent.
		/// </summary>
		/// <value>
		/// A <see cref="System.Net.IPEndPoint"/> that represents the server IP
		/// address and port number.
		/// </value>
		public IPEndPoint LocalEndPoint
			{
			get
				{
				return _connection.LocalEndPoint;
				}
			}

		/// <summary>
		/// Gets the HTTP version specified by the client.
		/// </summary>
		/// <value>
		/// A <see cref="Version"/> that represents the HTTP version specified in
		/// the request line.
		/// </value>
		public Version ProtocolVersion
			{
			get
				{
				return _protocolVersion;
				}
			}

		/// <summary>
		/// Gets the query string included in the request.
		/// </summary>
		/// <value>
		///   <para>
		///   A <see cref="NameValueCollection"/> that contains the query
		///   parameters.
		///   </para>
		///   <para>
		///   An empty collection if not included.
		///   </para>
		/// </value>
		public NameValueCollection QueryString
			{
			get
				{
				if (_queryString == null)
					{
					_queryString = HttpUtility.InternalParseQueryString (_url.Query, Encoding.UTF8);
					}

				return _queryString;
				}
			}

		/// <summary>
		/// Gets the raw URL (without the scheme, host, and port) requested by
		/// the client.
		/// </summary>
		/// <value>
		///   <para>
		///   A <see cref="string"/> that represents the raw URL specified in
		///   the request.
		///   </para>
		///   <para>
		///   It includes the query string if present.
		///   </para>
		/// </value>
		public string RawUrl
			{
			get
				{
				return _url.PathAndQuery;
				}
			}

		/// <summary>
		/// Gets the endpoint from which the request is sent.
		/// </summary>
		/// <value>
		/// A <see cref="System.Net.IPEndPoint"/> that represents the client IP
		/// address and port number.
		/// </value>
		public IPEndPoint RemoteEndPoint
			{
			get
				{
				return _connection.RemoteEndPoint;
				}
			}

		/// <summary>
		/// Gets the trace identifier of the request.
		/// </summary>
		/// <value>
		/// A <see cref="Guid"/> that represents the trace identifier.
		/// </value>
		public Guid RequestTraceIdentifier
			{
			get
				{
				return _requestTraceIdentifier;
				}
			}

		/// <summary>
		/// Gets the URL requested by the client.
		/// </summary>
		/// <value>
		/// A <see cref="Uri"/> that represents the URL specified in the request.
		/// </value>
		public Uri Url
			{
			get
				{
				return _url;
				}
			}

		/// <summary>
		/// Gets the URI of the resource from which the requested URL was obtained.
		/// </summary>
		/// <value>
		///   <para>
		///   A <see cref="Uri"/> from the value of the Referer header.
		///   </para>
		///   <para>
		///   <see langword="null"/> if the Referer header is not present.
		///   </para>
		/// </value>
		public Uri UrlReferrer
			{
			get
				{
				return _urlReferer;
				}
			}

		/// <summary>
		/// Gets the user agent from which the request is originated.
		/// </summary>
		/// <value>
		///   <para>
		///   A <see cref="string"/> that represents the value of the User-Agent
		///   header.
		///   </para>
		///   <para>
		///   <see langword="null"/> if the User-Agent header is not present.
		///   </para>
		/// </value>
		public string UserAgent
			{
			get
				{
				return _headers["User-Agent"];
				}
			}

		/// <summary>
		/// Gets the IP address and port number to which the request is sent.
		/// </summary>
		/// <value>
		/// A <see cref="string"/> that represents the server IP address and port
		/// number.
		/// </value>
		public string UserHostAddress
			{
			get
				{
				return _connection.LocalEndPoint.ToString ();
				}
			}

		/// <summary>
		/// Gets the server host name requested by the client.
		/// </summary>
		/// <value>
		///   <para>
		///   A <see cref="string"/> that represents the value of the Host header.
		///   </para>
		///   <para>
		///   It includes the port number if provided.
		///   </para>
		///   <para>
		///   <see langword="null"/> if the Host header is not present.
		///   </para>
		/// </value>
		public string UserHostName
			{
			get
				{
				return _headers["Host"];
				}
			}

		/// <summary>
		/// Gets the natural languages that are acceptable for the client.
		/// </summary>
		/// <value>
		///   <para>
		///   An array of <see cref="string"/> that contains the names of the
		///   natural languages specified in the value of the Accept-Language
		///   header.
		///   </para>
		///   <para>
		///   <see langword="null"/> if the Accept-Language header is not present.
		///   </para>
		/// </value>
		public string[] UserLanguages
			{
			get
				{
				var val = _headers["Accept-Language"];
				if (val == null)
					return null;

				if (_userLanguages == null)
					_userLanguages = val.Split (',').Trim ().ToArray ();

				return _userLanguages;
				}
			}

		#endregion

		#region Internal Methods

		internal void AddHeader (string headerField)
			{
			var colon = headerField.IndexOf (':');
			if (colon < 1)
				{
				_context.ErrorMessage = "Invalid header field";
				return;
				}

			var name = headerField.Substring (0, colon).Trim ();
			if (name.Length == 0 || !name.IsToken ())
				{
				_context.ErrorMessage = "Invalid header name";
				return;
				}

			var val = colon < headerField.Length - 1
						 ? headerField.Substring (colon + 1).Trim ()
						 : String.Empty;

			_headers.InternalSet (name, val, false);

			var lower = name.ToLower (CultureInfo.InvariantCulture);
			if (lower == "content-length")
				{
				long len;
#if SSHARP
				if (!Int64Ex.TryParse (val, out len))
#else
				if (!Int64.TryParse (val, out len))
#endif
					{
					_context.ErrorMessage = "Invalid Content-Length header";
					return;
					}

				if (len < 0)
					{
					_context.ErrorMessage = "Invalid Content-Length header";
					return;
					} 
				
				_contentLength = len;

				return;
				}

			if (lower == "content-type")
				{
				try
					{
					_contentType = HttpUtility.GetMimeType (val);
					_contentEncoding = HttpUtility.GetEncoding (val);
					}
				catch
					{
					_context.ErrorMessage = "Invalid Content-Type header";
					}

				return;
				}

			if (lower == "referer")
				{
				var referer = val.ToUri ();
				if (referer == null)
					{
					_context.ErrorMessage = "Invalid Referer header";
					return;
					}

				_urlReferer = referer;
				return;
				}
			}

		internal void FinishInitialization ()
			{
			var host = _headers["Host"];
			var hasHost = host != null && host.Length > 0;
			if (_protocolVersion > HttpVersion.Version10 && !hasHost)
				{
				_context.ErrorMessage = "Invalid Host header";
				return;
				}

			_url = HttpUtility.CreateRequestUrl (
						_uri,
						hasHost ? host : UserHostAddress,
						IsWebSocketRequest,
						IsSecureConnection
					 );

			if (_url == null)
				{
				_context.ErrorMessage = "Invalid request url";
				return;
				}

			var transferEnc = _headers["Transfer-Encoding"];
			if (transferEnc != null)
				{
				if (_protocolVersion < HttpVersion.Version11)
					{
					_context.ErrorMessage = "Invalid Transfer-Encoding header";
					return;
					}

				var comparison = StringComparison.OrdinalIgnoreCase;
				if (!transferEnc.Equals ("chunked", comparison))
					{
					_context.ErrorMessage = String.Empty;
					_context.ErrorStatus = 501;

					return;
					}

				_chunked = true;
				}

			if (_contentLength == -1 && !_chunked)
				{
				if (_httpMethod == "POST" || _httpMethod == "PUT")
					{
					_context.ErrorMessage = String.Empty;
					_context.ErrorStatus = 411;

					return;
					}
				}

			var expect = _headers["Expect"];
			if (_protocolVersion > HttpVersion.Version10 && expect != null)
				{
				var comparison = StringComparison.OrdinalIgnoreCase;
				if (!expect.Equals ("100-continue", comparison))
					{
					_context.ErrorMessage = "Invalid Expect header";
					return;
					}

				var output = _connection.GetResponseStream ();
				output.InternalWrite (_100continue, 0, _100continue.Length);
				}
			}

		internal bool FlushInput ()
			{
			if (!HasEntityBody)
				return true;

			var len = 2048;
			if (_contentLength > 0 && _contentLength < len)
				len = (int)_contentLength;

			var buff = new byte[len];

			while (true)
				{
				try
					{
					var ares = InputStream.BeginRead (buff, 0, len, null, null);
					if (!ares.IsCompleted)
						{
						var timeout = 100;
						if (!ares.AsyncWaitHandle.WaitOne (timeout))
							return false;
						}

					if (InputStream.EndRead (ares) <= 0)
						return true;
					}
				catch
					{
					return false;
					}
				}
			}

		internal bool IsUpgradeRequest (string protocol)
			{
			return _headers.Upgrades (protocol);
			}

		internal void SetRequestLine (string requestLine)
			{
			var parts = requestLine.Split (new[] { ' ' }, 3);
			if (parts.Length < 3)
				{
				_context.ErrorMessage = "Invalid request line (parts)";
				return;
				}

			var method = parts[0];
			if (method.Length == 0)
				{
				_context.ErrorMessage = "Invalid request line (method)";
				return;
				}

			if (!method.IsToken ())
				{
				_context.ErrorMessage = "Invalid request line (method)";
				return;
				}

			var uri = parts[1];
			if (uri.Length == 0)
				{
				_context.ErrorMessage = "Invalid request line (uri)";
				return;
				}

			var rawVer = parts[2];
			if (rawVer.Length != 8)
				{
				_context.ErrorMessage = "Invalid request line (version)";
				return;
				}

			if (rawVer.IndexOf ("HTTP/") != 0)
				{
				_context.ErrorMessage = "Invalid request line (version)";
				return;
				}

			Version ver;
			if (!rawVer.Substring (5).TryCreateVersion (out ver))
				{
				_context.ErrorMessage = "Invalid request line (version)";
				return;
				}

			if (ver.Major < 1)
				{
				_context.ErrorMessage = "Invalid request line (version)";
				return;
				}

			_httpMethod = method;
			_uri = uri;
			_protocolVersion = ver;
			}

		#endregion

		#region Public Methods

		/// <summary>
		/// Begins getting the client's X.509 v.3 certificate asynchronously.
		/// </summary>
		/// <remarks>
		/// This asynchronous operation must be completed by calling the
		/// <see cref="EndGetClientCertificate"/> method. Typically, that method is invoked by the
		/// <paramref name="requestCallback"/> delegate.
		/// </remarks>
		/// <returns>
		/// An <see cref="IAsyncResult"/> that contains the status of the asynchronous operation.
		/// </returns>
		/// <param name="requestCallback">
		/// An <see cref="AsyncCallback"/> delegate that references the method(s) called when the
		/// asynchronous operation completes.
		/// </param>
		/// <param name="state">
		/// An <see cref="object"/> that contains a user defined object to pass to the
		/// <paramref name="requestCallback"/> delegate.
		/// </param>
		/// <exception cref="NotImplementedException">
		/// This method isn't implemented.
		/// </exception>
		public IAsyncResult BeginGetClientCertificate (AsyncCallback requestCallback, object state)
			{
			// TODO: Not implemented.
			throw new NotImplementedException ();
			}

		/// <summary>
		/// Ends an asynchronous operation to get the client's X.509 v.3 certificate.
		/// </summary>
		/// <remarks>
		/// This method completes an asynchronous operation started by calling the
		/// <see cref="BeginGetClientCertificate"/> method.
		/// </remarks>
		/// <returns>
		/// A <see cref="X509Certificate2"/> that contains the client's X.509 v.3 certificate.
		/// </returns>
		/// <param name="asyncResult">
		/// An <see cref="IAsyncResult"/> obtained by calling the
		/// <see cref="BeginGetClientCertificate"/> method.
		/// </param>
		/// <exception cref="NotImplementedException">
		/// This method isn't implemented.
		/// </exception>
		public X509Certificate2 EndGetClientCertificate (IAsyncResult asyncResult)
			{
			// TODO: Not implemented.
			throw new NotImplementedException ();
			}

		/// <summary>
		/// Gets the client's X.509 v.3 certificate.
		/// </summary>
		/// <returns>
		/// A <see cref="X509Certificate2"/> that contains the client's X.509 v.3 certificate.
		/// </returns>
		/// <exception cref="NotImplementedException">
		/// This method isn't implemented.
		/// </exception>
		public X509Certificate2 GetClientCertificate ()
			{
			// TODO: Not Implemented.
			throw new NotImplementedException ();
			}

		/// <summary>
		/// Returns a <see cref="string"/> that represents the current
		/// <see cref="HttpListenerRequest"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="string"/> that represents the current <see cref="HttpListenerRequest"/>.
		/// </returns>
		public override string ToString ()
			{
			var buff = new StringBuilder (64);
			buff.AppendFormat ("{0} {1} HTTP/{2}\r\n", _httpMethod, _uri, _protocolVersion);
			buff.Append (_headers.ToString ());

			return buff.ToString ();
			}

		#endregion
		}
	}
