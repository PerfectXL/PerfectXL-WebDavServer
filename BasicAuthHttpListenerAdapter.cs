using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using WebDAVSharp.Server;
using WebDAVSharp.Server.Adapters;

namespace PerfectXL.WebDavServer
{
    // Copied from WebDAVSharp.Server.Adapters.HttpListenerAdapter with modifications.
    internal sealed class BasicAuthHttpListenerAdapter : WebDavDisposableBase, IHttpListener
    {
        internal BasicAuthHttpListenerAdapter()
        {
            AdaptedInstance = new HttpListener
            {
                AuthenticationSchemes = AuthenticationSchemes.Basic,
                UnsafeConnectionNtlmAuthentication = false
            };
        }

        public HttpListener AdaptedInstance { get; }

        public IHttpListenerContext GetContext(EventWaitHandle abortEvent)
        {
            if (abortEvent == null)
            {
                throw new ArgumentNullException(nameof(abortEvent));
            }

            IAsyncResult ar = AdaptedInstance.BeginGetContext(null, null);
            var index = WaitHandle.WaitAny(new[] {abortEvent, ar.AsyncWaitHandle});
            if (index != 1)
            {
                return null;
            }

            HttpListenerContext context = AdaptedInstance.EndGetContext(ar);
            return new HttpListenerContextAdapter(context);
        }

        public HttpListenerPrefixCollection Prefixes => AdaptedInstance.Prefixes;

        public void Start()
        {
            AdaptedInstance.Start();
        }

        public void Stop()
        {
            AdaptedInstance.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            if (AdaptedInstance.IsListening)
            {
                AdaptedInstance.Close();
            }
        }

        #region Nested types
        private sealed class HttpListenerContextAdapter : IHttpListenerContext
        {
            private readonly HttpListenerRequestAdapter _request;
            private readonly HttpListenerResponseAdapter _response;

            public HttpListenerContextAdapter(HttpListenerContext context)
            {
                AdaptedInstance = context ?? throw new ArgumentNullException(nameof(context));
                _request = new HttpListenerRequestAdapter(context.Request);
                _response = new HttpListenerResponseAdapter(context.Response);
            }

            public HttpListenerContext AdaptedInstance { get; }
            public IHttpListenerRequest Request => _request;
            public IHttpListenerResponse Response => _response;
        }

        private sealed class HttpListenerRequestAdapter : IHttpListenerRequest
        {
            public HttpListenerRequestAdapter(HttpListenerRequest request)
            {
                AdaptedInstance = request ?? throw new ArgumentNullException(nameof(request));
            }

            public HttpListenerRequest AdaptedInstance { get; }
            public Encoding ContentEncoding => AdaptedInstance.ContentEncoding;
            public long ContentLength64 => AdaptedInstance.ContentLength64;
            public NameValueCollection Headers => AdaptedInstance.Headers;
            public string HttpMethod => AdaptedInstance.HttpMethod;
            public Stream InputStream => AdaptedInstance.InputStream;
            public IPEndPoint RemoteEndPoint => AdaptedInstance.RemoteEndPoint;
            public Uri Url => AdaptedInstance.Url;
        }

        private sealed class HttpListenerResponseAdapter : IHttpListenerResponse
        {
            public HttpListenerResponseAdapter(HttpListenerResponse response)
            {
                AdaptedInstance = response ?? throw new ArgumentNullException(nameof(response));
            }

            public HttpListenerResponse AdaptedInstance { get; }

            public void AppendHeader(string name, string value)
            {
                AdaptedInstance.AppendHeader(name, value);
            }

            public void Close()
            {
                AdaptedInstance.Close();
            }

            public Encoding ContentEncoding
            {
                get => AdaptedInstance.ContentEncoding;
                set => AdaptedInstance.ContentEncoding = value;
            }

            public long ContentLength64
            {
                get => AdaptedInstance.ContentLength64;
                set => AdaptedInstance.ContentLength64 = value;
            }

            public Stream OutputStream => AdaptedInstance.OutputStream;

            public int StatusCode
            {
                get => AdaptedInstance.StatusCode;
                set => AdaptedInstance.StatusCode = value;
            }

            public string StatusDescription
            {
                get => AdaptedInstance.StatusDescription;
                set => AdaptedInstance.StatusDescription = value ?? string.Empty;
            }
        }
        #endregion
    }
}