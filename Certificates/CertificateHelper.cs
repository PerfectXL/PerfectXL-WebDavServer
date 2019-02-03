using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PerfectXL.WebDavServer.Certificates
{
    internal static class CertificateHelper
    {
        private const string CommonName = "CN=localhost";
        public const string FriendlyName = "PerfectXL";

        /// <summary>
        ///     Returns a certificate hash for a certificate that is found in the certificate store. If no valid certificate is
        ///     found, a new one is created and its hash is returned.
        /// </summary>
        public static byte[] GetHashForValidCertificate()
        {
            X509Certificate2 certificate2 = FindX509Certificate2(StoreName.My, StoreLocation.LocalMachine);
            if (certificate2 != null)
            {
                return certificate2.GetCertHash();
            }

            CreateNewSelfSignedCertificateInStore();
            return FindX509Certificate2(StoreName.My, StoreLocation.LocalMachine)?.GetCertHash();
        }

        // This does not work. Use CertUtil instead.
        private static void AddCertificateToStore(this X509Certificate2 certificate, StoreName storeName, StoreLocation storeLocation)
        {
            var store = new X509Store(storeName, storeLocation);
            try
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
            }
            finally
            {
                store.Close();
            }
        }

        private static void CreateNewSelfSignedCertificateInStore()
        {
            X509Certificate2 certificate = SelfSignedCertificate.Generate(CommonName, CommonName, FriendlyName);

            string certUtilPath = Path.Combine(Environment.SystemDirectory, "certutil.exe");
            if (!File.Exists(certUtilPath))
            {
                const string message = "Certutil not found. Normally this tool is installed as part of the operating system, so this is strange.";
                throw new FileNotFoundException(message, certUtilPath);
            }

            string tempFile = Path.GetTempFileName();
            try
            {
                const string password = "password";
                File.WriteAllBytes(tempFile, certificate.Export(X509ContentType.Pfx, password));
                Process.Start(certUtilPath, $@"-f -p {password} -importpfx ""{tempFile}"" NoRoot");
            }
            finally
            {
                Task.Delay(1000).ContinueWith(t => File.Delete(tempFile)).Wait();
            }
        }

        private static X509Certificate2 FindX509Certificate2(StoreName storeName, StoreLocation storeLocation)
        {
            var store = new X509Store(storeName, storeLocation);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                return store.Certificates.Cast<X509Certificate2>()
                    .FirstOrDefault(x => x.FriendlyName == FriendlyName && DateTime.UtcNow >= x.NotBefore && DateTime.UtcNow <= x.NotAfter);
            }
            finally
            {
                store.Close();
            }
        }
    }
}