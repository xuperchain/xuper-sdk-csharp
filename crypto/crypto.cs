using System;
using System.Numerics;
using System.Security.Cryptography;

namespace XuperSDK
{
    /// <summary>class <c>ECDSASignature</c> is the signature of ECDSA</summary>
    public class ECDSASignature
    {
        /// <value>Property <c>R</c> represents the random value.</value>
        public BigInteger R { get; set; }

        /// <value>Property <c>S</c> represents the signature value.</value>
        public BigInteger S { get; set; }
    }

    /// <summary>class <c>XCrypto</c> contains utilities of Cryptographic</summary>
    public class XCrypto
    {
        /// <summary>SignHash sign data using given private key and return ASN.1 DER encoded signature</summary>
        public static byte[] SignHash(AccountPrivateKey privateKey, byte[] data)
        {
            var ecdsa = loadECDSAFromPrivateKey(privateKey);
            var sign = ecdsa.SignHash(data);
            var halfLen = sign.Length / 2;
            // encode sign using ASN.1
            var r = new byte[halfLen];
            var s = new byte[halfLen];
            Array.Copy(sign, 0, r, 0, halfLen);
            Array.Copy(sign, halfLen, s, 0, halfLen);
            return Asn1EncodeSign(r, s);
        }

        /// <summary>VerifySign verify ASN.1 DER encoded signature using given public key with data</summary>
        public static bool VerifySign(AccountPublicKey publicKey, byte[] sign, byte[] data)
        {
            var decodedSign = Asn1DecodeSign(sign);
            var ecdsa = loadECDSAFromPublicKey(publicKey);
            return ecdsa.VerifyHash(data, decodedSign);
        }

        // Encode ECDSA signature using ASN.1 DER, note the r/s should in big-endian encoding
        private static byte[] Asn1EncodeSign(byte[] r, byte[] s)
        {
            if (r.Length <= 0 || s.Length <= 0)
            {
                throw new System.ArgumentException();
            }
            var rlen = r.Length;
            var slen = s.Length;
            // padding in DER format
            if (r[0] > 0x80) { rlen++; }
            if (s[0] > 0x80) { slen++; }

            // begin make ASN.1 encoding
            var encodedData = new byte[slen + rlen + 6];
            encodedData[0] = 0x30;                                  // ECDSA identifier
            encodedData[1] = (byte)(rlen + slen + 4);               // data length
            encodedData[2] = 0x02;                                  // Integer identifier
            encodedData[3] = (byte)rlen;                            // r length
            if (r[0] > 0x80)
            {
                // add padding zero for r
                encodedData[4] = 0x00;
                Array.Copy(r, 0, encodedData, 5, rlen - 1);         // r value
            }
            else
            {
                Array.Copy(r, 0, encodedData, 4, rlen);             // r value
            }
            encodedData[4 + rlen] = 0x02;                           // Integer identifier
            encodedData[5 + rlen] = (byte)slen;                     // s length
            if (s[0] > 0x80)
            {
                // add padding zero for s
                encodedData[6 + rlen] = 0x00;
                Array.Copy(s, 0, encodedData, 7 + rlen, slen - 1);  // s value
            }
            else
            {
                Array.Copy(s, 0, encodedData, 6 + rlen, slen);      // s value
            }

            return encodedData;
        }

        /// <summary>Double SHA256 the data</summary>
        public static byte[] DoubleSha256(byte[] data)
        {
            return ComputeSha256Hash(ComputeSha256Hash(data));
        }

        /// <summary>SHA256 of the data</summary>
        public static byte[] ComputeSha256Hash(byte[] data)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(data);
                return bytes;
            }
        }

        private static byte[] Asn1EncodeSign(BigInteger r, BigInteger s)
        {
            var rBytes = r.ToByteArray();
            var sBytes = s.ToByteArray();
            Array.Reverse(rBytes, 0, rBytes.Length);
            Array.Reverse(sBytes, 0, sBytes.Length);
            return Asn1EncodeSign(rBytes, sBytes);
        }

        private static byte[] Asn1DecodeSign(byte[] sign)
        {
            if (sign.Length <= 8)
            {
                throw new System.ArgumentException();
            }
            var rlen = sign[3];
            if (sign.Length <= rlen + 6)
            {
                throw new System.ArgumentException();
            }
            var slen = sign[rlen + 5];
            var rBytes = new byte[rlen];
            var sBytes = new byte[slen];
            Array.Copy(sign, 4, rBytes, 0, rlen);
            Array.Copy(sign, rlen + 6, sBytes, 0, slen);
            if (rBytes[0] == 0x00)
            {
                rlen--;
            }
            if (sBytes[0] == 0x00)
            {
                slen--;
            }
            var decodedSign = new byte[rlen + slen];
            Array.Copy(rBytes, (rBytes[0] == 0x00) ? 1 : 0, decodedSign, 0, rlen);
            Array.Copy(sBytes, (sBytes[0] == 0x00) ? 1 : 0, decodedSign, rlen, slen);
            return decodedSign;
        }



        private static ECDsa loadECDSAFromPrivateKey(AccountPrivateKey privateKey)
        {
            var ecparams = new ECParameters();
            ecparams.Curve = ECCurve.NamedCurves.nistP256;
            var ecPoint = new ECPoint();
            ecPoint.X = XConvert.GetECBytesFromBigInteger(privateKey.X);
            ecPoint.Y = XConvert.GetECBytesFromBigInteger(privateKey.Y);
            ecparams.Q = ecPoint;
            ecparams.D = XConvert.GetECBytesFromBigInteger(privateKey.D);
            return ECDsa.Create(ecparams);
        }

        private static ECDsa loadECDSAFromPublicKey(AccountPublicKey publicKey)
        {
            var param = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = XConvert.GetECBytesFromBigInteger(publicKey.X),
                    Y = XConvert.GetECBytesFromBigInteger(publicKey.Y),
                },
            };
            return ECDsa.Create(param);
        }
    }
}