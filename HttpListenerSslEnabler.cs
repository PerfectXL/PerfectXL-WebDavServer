using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using NLog;
using PerfectXL.WebDavServer.Certificates;
using SslCertBinding.Net;

namespace PerfectXL.WebDavServer
{
    internal class HttpListenerSslEnabler
    {
        public enum BindingResult
        {
            Unknown,
            Success,
            Failed
        }

        private static readonly Logger MyLogger = LogManager.GetCurrentClassLogger();

        internal static BindingResult BindCertificate()
        {
            try
            {
                var endPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), AppSettings.Port);
                var certificateThumbprint = GetCertificateThumbprint();
                GuidAttribute guidAttribute = GetGuidAttribute();
                if (guidAttribute == null)
                {
                    throw new NullReferenceException(nameof(GetGuidAttribute));
                }

                var appId = new Guid(guidAttribute.Value);

                var certificateBinding = new CertificateBinding(certificateThumbprint, StoreName.My, endPoint, appId)
                {
                    Options = {DoNotVerifyCertificateRevocation = true, NoUsageCheck = true}
                };
                ICertificateBindingConfiguration config = new CertificateBindingConfiguration();
                config.Bind(certificateBinding);

                return BindingResult.Success;
            }
            catch (Exception e)
            {
                MyLogger.Error(e, $"Error while binding the certificate to the requested port. {e.Message}");
                MyLogger.Error(e, "Re-run the command as administrator.");
                return BindingResult.Failed;
            }
        }

        internal static bool HasBinding()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), AppSettings.Port);
            var certificateThumbprint = GetCertificateThumbprint();
            ICertificateBindingConfiguration config = new CertificateBindingConfiguration();
            var binding = config.Query(endPoint);
            return binding.Length > 0 && binding.Any(x => x.Thumbprint == certificateThumbprint);
        }

        private static string GetCertificateThumbprint()
        {
            return BitConverter.ToString(CertificateHelper.GetHashForValidCertificate()).Replace("-", "");
        }

        private static GuidAttribute GetGuidAttribute()
        {
            return (GuidAttribute) Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), true).FirstOrDefault();
        }
    }
}