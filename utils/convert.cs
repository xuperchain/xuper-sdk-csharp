using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Google.Protobuf;

namespace XChainSDK
{
    // XConvert is the converter utilities
    /// <summary>XConvert is the converter utilities</summary>
    public class XConvert
    {
        /// <summary>Convert byte[] to hex string</summary>
        public static string ByteArrayToHexString(byte[] data)
        {
            if (data == null)
            {
                return "";
            }
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }

        /// <summary>Convert hex string to byte[]</summary>
        public static byte[] HexStringToByteArray(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return new byte[0];
            }
            var len = hex.Length;
            byte[] result = new byte[len / 2];
            for (int i = 0; i < len; i += 2)
                result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return result;
        }

        /// <summary>Convert signed bytes of BigInteger to unsigned bytes</summary>
        protected static byte[] SignedBytesToUnsigned(byte[] signed)
        {
            var len = signed.Length;
            if (signed[len - 1] == 0x0)
            {
                var unsigned = new byte[len - 1];
                Array.Copy(signed, unsigned, len - 1);
                return unsigned;
            }
            return signed;
        }

        /// <summary>Convert byte[] of Golang style BigInteger to .NET BigInteger</summary>
        protected static BigInteger NumberBytesToBigInteger(byte[] number)
        {
            if (number != null)
            {
                Array.Reverse(number, 0, number.Length);
                return new BigInteger(number);
            }
            return new BigInteger(0);
        }

        /// <summary>Utility to convert BigInteger into ECDSA byte[]</summary>
        public static byte[] GetECBytesFromBigInteger(BigInteger val)
        {
            var vBytes = SignedBytesToUnsigned(val.ToByteArray());
            Array.Reverse(vBytes, 0, vBytes.Length);
            return vBytes;
        }

        /// <summary>Convert local Transaction to Protobuf Transaction</summary>
        public static Pb.Transaction LocalTxToPbTx(Transaction tx)
        {
            var pbtx = new Pb.Transaction();
            pbtx.Txid = ByteString.CopyFrom(tx.Txid);
            if (tx.Blockid != null)
            {
                pbtx.Blockid = ByteString.CopyFrom(tx.Blockid);
            }
            if (tx.TxInputs != null && tx.TxInputs.Count > 0)
            {
                foreach (var input in tx.TxInputs)
                {
                    pbtx.TxInputs.Add(new Pb.TxInput
                    {
                        FromAddr = ByteString.CopyFrom(input.FromAddr),
                        Amount = ByteString.CopyFrom(input.Amount),
                        RefOffset = input.RefOffset,
                        RefTxid = ByteString.CopyFrom(input.RefTxid),
                        FrozenHeight = input.FrozenHeight,
                    });
                }
            }

            if (tx.TxOutputs != null && tx.TxOutputs.Count > 0)
            {
                foreach (var output in tx.TxOutputs)
                {
                    pbtx.TxOutputs.Add(new Pb.TxOutput
                    {
                        ToAddr = ByteString.CopyFrom(output.ToAddr),
                        Amount = ByteString.CopyFrom(output.Amount),
                        FrozenHeight = output.FrozenHeight,
                    });
                }
            }
            if (tx.Desc != null)
            {
                pbtx.Desc = ByteString.CopyFrom(tx.Desc);
            }
            pbtx.Coinbase = tx.Coinbase;
            pbtx.Nonce = tx.Nonce;
            pbtx.Timestamp = tx.Timestamp;
            pbtx.Version = tx.Version;
            pbtx.Autogen = tx.Autogen;

            if (tx.TxInputsExt != null && tx.TxInputsExt.Count > 0)
            {
                foreach (var input in tx.TxInputsExt)
                {
                    pbtx.TxInputsExt.Add(new Pb.TxInputExt
                    {
                        Key = ByteString.CopyFrom(input.Key),
                        Bucket = input.Bucket,
                        RefTxid = ByteString.CopyFrom(input.RefTxid),
                        RefOffset = input.RefOffset,
                    });
                }
            }

            if (tx.TxOutputsExt != null && tx.TxOutputsExt.Count > 0)
            {
                foreach (var output in tx.TxOutputsExt)
                {
                    pbtx.TxOutputsExt.Add(new Pb.TxOutputExt
                    {
                        Key = ByteString.CopyFrom(output.Key),
                        Bucket = output.Bucket,
                        Value = ByteString.CopyFrom(output.Value),
                    });
                }
            }

            if (tx.ContractRequests != null && tx.ContractRequests.Count > 0)
            {
                foreach (var cr in tx.ContractRequests)
                {
                    var invokeReq = new Pb.InvokeRequest
                    {
                        ModuleName = cr.ModuleName,
                        ContractName = cr.ContractName,
                        MethodName = cr.MethodName,
                    };
                    foreach (var arg in cr.Args)
                    {
                        invokeReq.Args.Add(arg.Key, ByteString.CopyFrom(arg.Value));
                    }
                    foreach (var limit in cr.ResourceLimits)
                    {
                        invokeReq.ResourceLimits.Add(new Pb.ResourceLimit
                        {
                            Type = (Pb.ResourceType)limit.Type,
                            Limit = limit.Limit,
                        });
                    }
                    pbtx.ContractRequests.Add(invokeReq);
                }
            }

            pbtx.Initiator = tx.Initiator;
            if (tx.InitiatorSigns != null && tx.InitiatorSigns.Count > 0)
            {
                foreach (var sign in tx.InitiatorSigns)
                {
                    pbtx.InitiatorSigns.Add(new Pb.SignatureInfo
                    {
                        PublicKey = sign.PublicKey,
                        Sign = ByteString.CopyFrom(sign.Sign),
                    });
                }
            }
            if (tx.AuthRequire != null && tx.AuthRequire.Count > 0)
            {
                foreach (var addr in tx.AuthRequire)
                {
                    pbtx.AuthRequire.Add(addr);
                }
            }
            if (tx.AuthRequireSigns != null && tx.AuthRequireSigns.Count > 0)
            {
                foreach (var sign in tx.AuthRequireSigns)
                {
                    pbtx.AuthRequireSigns.Add(new Pb.SignatureInfo
                    {
                        PublicKey = sign.PublicKey,
                        Sign = ByteString.CopyFrom(sign.Sign),
                    });
                }
            }

            return pbtx;
        }

        /// <summary>Convert Protobuf Transaction to local Transaction</summary>
        public static Transaction PbTxToLocalTx(Pb.Transaction tx)
        {
            var localTx = new Transaction();
            localTx.Txid = tx.Txid.ToByteArray();
            if (!tx.Blockid.IsEmpty)
            {
                localTx.Blockid = tx.Blockid.ToByteArray();
            }
            if (tx.TxInputs != null && tx.TxInputs.Count > 0)
            {
                localTx.TxInputs = new List<TxInput>();
                foreach (var input in tx.TxInputs)
                {
                    localTx.TxInputs.Add(new TxInput
                    {
                        FromAddr = input.FromAddr.ToByteArray(),
                        Amount = input.Amount.ToByteArray(),
                        RefOffset = input.RefOffset,
                        RefTxid = input.RefTxid.ToByteArray(),
                        FrozenHeight = input.FrozenHeight,
                    });
                }
            }

            if (tx.TxOutputs != null && tx.TxOutputs.Count > 0)
            {
                localTx.TxOutputs = new List<TxOutput>();
                foreach (var output in tx.TxOutputs)
                {
                    localTx.TxOutputs.Add(new TxOutput
                    {
                        ToAddr = output.ToAddr.ToByteArray(),
                        Amount = output.Amount.ToByteArray(),
                        FrozenHeight = output.FrozenHeight,
                    });
                }
            }
            if (tx.Desc != null)
            {
                localTx.Desc = tx.Desc.ToByteArray();
            }
            localTx.Coinbase = tx.Coinbase;
            localTx.Nonce = tx.Nonce;
            localTx.Timestamp = tx.Timestamp;
            localTx.Version = tx.Version;
            localTx.Autogen = tx.Autogen;

            if (tx.TxInputsExt != null && tx.TxInputsExt.Count > 0)
            {
                localTx.TxInputsExt = new List<TxInputExt>();
                foreach (var input in tx.TxInputsExt)
                {
                    localTx.TxInputsExt.Add(new TxInputExt
                    {
                        Key = input.Key.ToByteArray(),
                        Bucket = input.Bucket,
                        RefTxid = input.RefTxid.ToByteArray(),
                        RefOffset = input.RefOffset,
                    });
                }
            }

            if (tx.TxOutputsExt != null && tx.TxOutputsExt.Count > 0)
            {
                localTx.TxOutputsExt = new List<TxOutputExt>();
                foreach (var output in tx.TxOutputsExt)
                {
                    localTx.TxOutputsExt.Add(new TxOutputExt
                    {
                        Key = output.Key.ToByteArray(),
                        Bucket = output.Bucket,
                        Value = output.Value.ToByteArray(),
                    });
                }
            }

            if (tx.ContractRequests != null && tx.ContractRequests.Count > 0)
            {
                localTx.ContractRequests = new List<InvokeRequest>();
                foreach (var cr in tx.ContractRequests)
                {
                    var invokeReq = new InvokeRequest
                    {
                        ModuleName = cr.ModuleName,
                        ContractName = cr.ContractName,
                        MethodName = cr.MethodName,
                    };
                    foreach (var arg in cr.Args)
                    {
                        invokeReq.Args = new SortedDictionary<string, byte[]>();
                        invokeReq.Args.Add(arg.Key, arg.Value.ToByteArray());
                    }
                    foreach (var limit in cr.ResourceLimits)
                    {
                        invokeReq.ResourceLimits = new List<ResourceLimit>();
                        invokeReq.ResourceLimits.Add(new ResourceLimit
                        {
                            Type = (ResourceType)limit.Type,
                            Limit = limit.Limit,
                        });
                    }
                    localTx.ContractRequests.Add(invokeReq);
                }
            }

            localTx.Initiator = tx.Initiator;
            if (tx.InitiatorSigns != null && tx.InitiatorSigns.Count > 0)
            {
                localTx.InitiatorSigns = new List<SignatureInfo>();
                foreach (var sign in tx.InitiatorSigns)
                {
                    localTx.InitiatorSigns.Add(new SignatureInfo
                    {
                        PublicKey = sign.PublicKey,
                        Sign = sign.Sign.ToByteArray(),
                    });
                }
            }
            if (tx.AuthRequire != null && tx.AuthRequire.Count > 0)
            {
                localTx.AuthRequire = new List<string>();
                foreach (var addr in tx.AuthRequire)
                {
                    localTx.AuthRequire.Add(addr);
                }
            }
            if (tx.AuthRequireSigns != null && tx.AuthRequireSigns.Count > 0)
            {
                localTx.AuthRequireSigns = new List<SignatureInfo>();
                foreach (var sign in tx.AuthRequireSigns)
                {
                    localTx.AuthRequireSigns.Add(new SignatureInfo
                    {
                        PublicKey = sign.PublicKey,
                        Sign = sign.Sign.ToByteArray(),
                    });
                }
            }

            return localTx;
        }

        /// <summary>Convert Protobuf Block to local Block</summary>
        public static Block PbBlockToLocalBlock(Pb.Block block)
        {
            if (block == null)
            {
                return null;
            }

            var localBlock = new Block();
            localBlock.Blockid = ByteArrayToHexString(block.Block_.Blockid.ToByteArray());
            localBlock.Status = (BlockStatus)block.Status;
            if (block.Block_ == null)
            {
                localBlock.BlockData = null;
            }
            else
            {
                localBlock.BlockData = new InternalBlock
                {
                    Blockid = localBlock.Blockid,
                    Version = block.Block_.Version,
                    Height = block.Block_.Height,
                    Nonce = block.Block_.Nonce,
                    PreHash = ByteArrayToHexString(block.Block_.PreHash.ToByteArray()),
                    NextHash = ByteArrayToHexString(block.Block_.NextHash.ToByteArray()),
                    Proposer = Encoding.ASCII.GetString(block.Block_.Proposer.ToByteArray()),
                    PublicKey = block.Block_.Pubkey.ToByteArray(),
                    Sign = block.Block_.Sign.ToByteArray(),
                    MerkleRoot = ByteArrayToHexString(block.Block_.ToByteArray()),
                    Timestamp = block.Block_.Timestamp,
                    CurTerm = block.Block_.CurTerm,
                    CurBlockNum = block.Block_.CurBlockNum,
                    MerkleTree = new List<string>(),
                    FailedTxs = new Dictionary<string, string>(),
                    Transactions = new List<Transaction>(),
                    TxCount = block.Block_.TxCount,
                };
                foreach (var mid in block.Block_.MerkleTree)
                {
                    localBlock.BlockData.MerkleTree.Add(ByteArrayToHexString(mid.ToByteArray()));
                }
                foreach (var kvpair in block.Block_.FailedTxs)
                {
                    localBlock.BlockData.FailedTxs.Add(kvpair.Key, kvpair.Value);
                }
                foreach (var tx in block.Block_.Transactions)
                {
                    var localTx = PbTxToLocalTx(tx);
                    localBlock.BlockData.Transactions.Add(localTx);
                }
            }
            return localBlock;
        }
    }
}