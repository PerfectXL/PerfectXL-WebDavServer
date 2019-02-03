using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using BigInteger = Org.BouncyCastle.Math.BigInteger;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace PerfectXL.WebDavServer.Certificates
{
    internal static class SelfSignedCertificate
    {
        private const string Algorithm = "SHA256WithRSA";
        private const X509KeyStorageFlags Flags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet;
        private const int Strength = 2048;

        /// <summary>
        ///     Generates a new self-signed certificate with the given subject, issuer and friendly name.
        /// </summary>
        public static X509Certificate2 Generate(string subjectName, string issuerName, string friendlyName)
        {
            var random = new SecureRandom(new CryptoApiRandomGenerator());

            AsymmetricCipherKeyPair subjectKeyPair = GenerateAsymmetricCipherKeyPair(random);

            X509V3CertificateGenerator certificateGenerator = GetCertificateGenerator(subjectName, issuerName, subjectKeyPair, random);

            X509Certificate certificate = certificateGenerator.Generate(new Asn1SignatureFactory(Algorithm, subjectKeyPair.Private, random));

            return new X509Certificate2(certificate.GetEncoded(), "", Flags) {FriendlyName = friendlyName, PrivateKey = GetPrivateKey(subjectKeyPair)};
        }

        private static AsymmetricCipherKeyPair GenerateAsymmetricCipherKeyPair(SecureRandom random)
        {
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(new KeyGenerationParameters(random, Strength));
            return keyPairGenerator.GenerateKeyPair();
        }

        private static X509V3CertificateGenerator GetCertificateGenerator(string subjectName, string issuerName, AsymmetricCipherKeyPair subjectKeyPair,
            SecureRandom random)
        {
            var certificateGenerator = new X509V3CertificateGenerator();
            certificateGenerator.SetSerialNumber(BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random));
            certificateGenerator.SetIssuerDN(new X509Name(issuerName));
            certificateGenerator.SetSubjectDN(new X509Name(subjectName));
            certificateGenerator.SetNotBefore(DateTime.UtcNow.Date);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(10));
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);
            return certificateGenerator;
        }

        private static RSA GetPrivateKey(AsymmetricCipherKeyPair subjectKeyPair)
        {
            return DotNetUtilities.ToRSA(GetRsaPrivateCrtKeyParameters(subjectKeyPair));
        }

        private static RsaPrivateCrtKeyParameters GetRsaPrivateCrtKeyParameters(AsymmetricCipherKeyPair subjectKeyPair)
        {
            PrivateKeyInfo privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(subjectKeyPair.Private);

            var sequence = (Asn1Sequence)Asn1Object.FromByteArray(privateKeyInfo.ParsePrivateKey().GetEncoded());
            if (sequence.Count != 9)
            {
                throw new PemException("Malformed sequence in RSA private key.");
            }

            RsaPrivateKeyStructure rsa = RsaPrivateKeyStructure.GetInstance(sequence);

            return new RsaPrivateCrtKeyParameters(rsa.Modulus,
                rsa.PublicExponent,
                rsa.PrivateExponent,
                rsa.Prime1,
                rsa.Prime2,
                rsa.Exponent1,
                rsa.Exponent2,
                rsa.Coefficient);
        }
    }
}
