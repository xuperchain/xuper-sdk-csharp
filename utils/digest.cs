using System;
using System.Numerics;
using Newtonsoft.Json;
using System.IO;
using Google.Protobuf;
using System.Text;
using System.Security.Cryptography;

namespace XuperSDK
{
    class XDigest
    {
        public static byte[] MakeDigestHash(Transaction tx)
        {
            return calcDigest(tx, false);
        }

        public static byte[] MakeTransactionID(Transaction tx)
        {
            return calcDigest(tx, true);
        }

        private static byte[] calcDigest(Transaction tx, bool needSigns)
        {
            string encoded = "";
            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
            };
            if (tx.TxInputs != null)
            {
                for (var i = 0; i < tx.TxInputs.Count; i++)
                {
                    var input = tx.TxInputs[i];
                    if (input.RefTxid.Length > 0)
                    {
                        encoded += JsonConvert.SerializeObject(input.RefTxid) + "\n";
                    }
                    encoded += JsonConvert.SerializeObject(input.RefOffset) + "\n";
                    if (input.FromAddr.Length > 0)
                    {
                        encoded += JsonConvert.SerializeObject(input.FromAddr) + "\n";
                    }
                    if (input.Amount.Length > 0)
                    {
                        encoded += JsonConvert.SerializeObject(input.Amount) + "\n";
                    }
                    encoded += JsonConvert.SerializeObject(input.FrozenHeight) + "\n";
                }
            }

            encoded += JsonConvert.SerializeObject(tx.TxOutputs, settings) + "\n";
            if (tx.Desc != null && tx.Desc.Length > 0)
            {
                encoded += JsonConvert.SerializeObject(tx.Desc) + "\n";
            }
            encoded += JsonConvert.SerializeObject(tx.Nonce, settings) + "\n";
            encoded += JsonConvert.SerializeObject(tx.Timestamp) + "\n";
            encoded += JsonConvert.SerializeObject(tx.Version) + "\n";
            if (tx.TxInputsExt != null)
            {
                for (var i = 0; i < tx.TxInputsExt.Count; i++)
                {
                    var input = tx.TxInputsExt[i];
                    encoded += JsonConvert.SerializeObject(input.Bucket) + "\n";
                    if (input.Key.Length > 0)
                    {
                        encoded += JsonConvert.SerializeObject(input.Key) + "\n";
                    }
                    if (input.RefTxid.Length > 0)
                    {
                        encoded += JsonConvert.SerializeObject(input.RefTxid) + "\n";
                    }
                    encoded += JsonConvert.SerializeObject(input.RefOffset) + "\n";
                }
            }

            if (tx.TxOutputsExt != null)
            {
                foreach (var output in tx.TxOutputsExt)
                {
                    encoded += (JsonConvert.SerializeObject(output.Bucket) + "\n");
                    if (output.Key.Length > 0)
                    {
                        encoded += (JsonConvert.SerializeObject(output.Key) + "\n");
                    }
                    if (output.Value.Length > 0)
                    {
                        encoded += (JsonConvert.SerializeObject(output.Value) + "\n");
                    }
                }
            }

            encoded += (JsonConvert.SerializeObject(tx.ContractRequests, settings) + "\n");
            encoded += (JsonConvert.SerializeObject(tx.Initiator) + "\n");
            encoded += (JsonConvert.SerializeObject(tx.AuthRequire) + "\n");

            if (needSigns)
            {
                encoded += (JsonConvert.SerializeObject(tx.InitiatorSigns) + "\n");
                encoded += (JsonConvert.SerializeObject(tx.AuthRequireSigns) + "\n");
            }

            encoded += (JsonConvert.SerializeObject(tx.Coinbase) + "\n");
            encoded += (JsonConvert.SerializeObject(tx.Autogen) + "\n");
            //Console.WriteLine("Debug: digest=\n" + encoded);
            var encodedBytes = Encoding.ASCII.GetBytes(encoded);
            return XCrypto.DoubleSha256(encodedBytes);
        }
    }
}