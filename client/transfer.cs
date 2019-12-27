using System;
using System.Numerics;
using System.Collections.Generic;
using Google.Protobuf;
using System.Text;

namespace XuperSDK
{
    public partial class XChainClient
    {
        private const string HEADER_LOG_PREFIX = "CS_";
        private Pb.Header GetDefaultHeader()
        {
            var rand = new Random((int)DateTime.UtcNow.Ticks);
            var randVal = rand.Next(0, 1000000).ToString();
            var timestamp = (int)(DateTime.UtcNow.Ticks / 10000);
            var logid = HEADER_LOG_PREFIX + timestamp.ToString() + "_" + randVal;
            return new Pb.Header
            {
                Error = Pb.XChainErrorEnum.Success,
                Logid = logid,
            };
        }

        private Pb.UtxoOutput SelectUTXO(string bcname, string address, AccountPublicKey pubkey, BigInteger amount)
        {
            var reqData = new Pb.UtxoInput()
            {
                Header = GetDefaultHeader(),
                Bcname = bcname,
                Address = address,
                TotalNeed = amount.ToString(),
                NeedLock = false,
            };
            var res = client.SelectUTXO(reqData);
            if (res.Header.Error != Pb.XChainErrorEnum.Success)
            {
                Console.WriteLine("Error in select UTXO, errcode=" + (int)res.Header.Error + ", logid=" + reqData.Header.Logid);
                return null;
            }
            return res;
        }

        private Pb.PreExecWithSelectUTXOResponse PreExecWithSelectUTXO(
            string bcname, string address, AccountPrivateKey privkey, BigInteger amount, string contractName,
            string method, SortedDictionary<string, byte[]> args, ContactVMType.Type contractType, string initiator,
            List<string> authRequire)
        {
            var header = GetDefaultHeader();
            var req = new Pb.PreExecWithSelectUTXORequest
            {
                Header = header,
                Bcname = bcname,
                Address = address,
                TotalAmount = (long)amount,
                NeedLock = false,
                Request = new Pb.InvokeRPCRequest
                {
                    Header = header,
                    Bcname = bcname,
                    Initiator = initiator,
                },
            };
            if (authRequire != null)
            {
                req.Request.AuthRequire.AddRange(authRequire);
            }

            var invoke = new Pb.InvokeRequest
            {
                ModuleName = ContactVMType.GetNameByType(contractType),
                MethodName = method,
            };
            if (contractName != null)
            {
                invoke.ContractName = contractName;
            }
            foreach (var item in args)
            {
                invoke.Args.Add(item.Key, ByteString.CopyFrom(item.Value));
            }
            req.Request.Requests.Add(invoke);
            var res = client.PreExecWithSelectUTXO(req);
            if (res.Header.Error != Pb.XChainErrorEnum.Success)
            {
                Console.WriteLine("PreExecWithSelectUTXO failed, error code=" + (int)res.Header.Error);
                return null;
            }
            return res;
        }

        private Transaction AssembleTx(Pb.UtxoOutput utxo, XCAccount account, List<string> authRequire,
            string to, BigInteger amount, Pb.InvokeResponse contractInvoke, string desc)
        {
            var tx = new Transaction();
            // check param
            if (amount < 0 || account == null)
            {
                return null;
            }
            // if have utxo, assemble utxo input/ouput
            if (utxo != null)
            {
                // assemble TxInputs
                tx.TxInputs = new List<TxInput>();
                var total = BigInteger.Parse(utxo.TotalSelected);
                for (var i = 0; i < utxo.UtxoList.Count; i++)
                {
                    var utxoItem = utxo.UtxoList[i];
                    var input = new TxInput
                    {
                        FromAddr = utxoItem.ToAddr.ToByteArray(),
                        Amount = utxoItem.Amount.ToByteArray(),
                        RefTxid = utxoItem.RefTxid.ToByteArray(),
                        RefOffset = utxoItem.RefOffset,
                    };
                    tx.TxInputs.Add(input);
                }

                tx.TxOutputs = new List<TxOutput>();
                // Assemble utxo Output for transferring to
                if (amount > 0 && to != "")
                {
                    // utxo check
                    if (amount > total)
                    {
                        Console.WriteLine("Utxo use greater than utxo selected" + ", selected=" + total + ", use=", amount);
                        return null;
                    }
                    var output = new TxOutput()
                    {
                        ToAddr = Encoding.ASCII.GetBytes(to),
                        Amount = amount.ToByteArray(),
                    };
                    Array.Reverse(output.Amount, 0, output.Amount.Length);
                    tx.TxOutputs.Add(output);
                    total -= amount;
                }

                // Assemble contract fee
                if (contractInvoke != null && contractInvoke.GasUsed > 0)
                {
                    var gasUsed = new BigInteger(contractInvoke.GasUsed);
                    if (gasUsed > total)
                    {
                        Console.WriteLine("Utxo use greater than utxo selected" + ", selected=" + total + ", use=", gasUsed);
                        return null;
                    }
                    var output = new TxOutput()
                    {
                        ToAddr = Encoding.ASCII.GetBytes("$"),
                        Amount = gasUsed.ToByteArray(),
                    };
                    Array.Reverse(output.Amount, 0, output.Amount.Length);
                    tx.TxOutputs.Add(output);
                    total -= gasUsed;
                }

                // charge utxo to user
                if (total > 0)
                {
                    var chargeOutput = new TxOutput()
                    {
                        ToAddr = Encoding.ASCII.GetBytes(account.Address),
                        Amount = total.ToByteArray(),
                    };
                    Array.Reverse(chargeOutput.Amount, 0, chargeOutput.Amount.Length);
                    tx.TxOutputs.Add(chargeOutput);
                }
            }

            // Assemble contracts
            if (contractInvoke != null)
            {
                if (contractInvoke.Inputs.Count > 0)
                {
                    tx.TxInputsExt = new List<TxInputExt>();
                }
                if (contractInvoke.Outputs.Count > 0)
                {
                    tx.TxOutputsExt = new List<TxOutputExt>();
                }
                if (contractInvoke.Requests.Count > 0)
                {
                    tx.ContractRequests = new List<InvokeRequest>();
                }
                // TODO: transfer within contract is not supported
                foreach (var input in contractInvoke.Inputs)
                {
                    var inputExt = new TxInputExt
                    {
                        Bucket = input.Bucket,
                        Key = input.Key.ToByteArray(),
                        RefTxid = input.RefTxid.ToByteArray(),
                        RefOffset = input.RefOffset,
                    };
                    tx.TxInputsExt.Add(inputExt);
                }
                foreach (var output in contractInvoke.Outputs)
                {
                    var outputExt = new TxOutputExt
                    {
                        Bucket = output.Bucket,
                        Key = output.Key.ToByteArray(),
                        Value = output.Value.ToByteArray(),
                    };
                    tx.TxOutputsExt.Add(outputExt);
                }
                foreach (var request in contractInvoke.Requests)
                {
                    var invoke = new InvokeRequest
                    {
                        ModuleName = request.ModuleName,
                        ContractName = request.ContractName,
                        MethodName = request.MethodName,
                    };
                    foreach (var arg in request.Args)
                    {
                        invoke.Args.Add(arg.Key, arg.Value.ToByteArray());
                    }
                    foreach (var limit in request.ResourceLimits)
                    {
                        invoke.ResourceLimits.Add(new ResourceLimit
                        {
                            Type = (ResourceType)limit.Type,
                            Limit = limit.Limit,
                        });
                    }
                    tx.ContractRequests.Add(invoke);
                }
            }

            // Assemble other data
            tx.Desc = Encoding.ASCII.GetBytes(desc);
            tx.Version = 1;
            tx.Coinbase = false;
            tx.Autogen = false;
            tx.Initiator = account.Address;
            if (authRequire != null && authRequire.Count > 0)
            {
                tx.AuthRequire = authRequire;
            }
            var digestHash = XDigest.MakeDigestHash(tx);
            var sign = XCrypto.SignHash(account.PrivateKey, digestHash);
            var signInfo = new SignatureInfo()
            {
                PublicKey = account.PublicKey.RawKey,
                Sign = sign,
            };
            tx.InitiatorSigns = new List<SignatureInfo>();
            tx.InitiatorSigns.Add(signInfo);
            if (authRequire != null && authRequire.Count > 0)
            {
                tx.AuthRequireSigns = new List<SignatureInfo>();
                tx.AuthRequireSigns.Add(signInfo);
            }
            var txid = XDigest.MakeTransactionID(tx);
            tx.Txid = txid;
            return tx;
        }

        private Pb.CommonReply PostTx(string bcname, Transaction tx)
        {
            var pbtx = XConvert.LocalTxToPbTx(tx);
            var reqTx = new Pb.TxStatus
            {
                Txid = pbtx.Txid,
                Tx = pbtx,
                Bcname = bcname,
            };
            var postRes = client.PostTx(reqTx);
            if (postRes.Header.Error != Pb.XChainErrorEnum.Success)
            {
                Console.WriteLine("post tx failed, txid=" + tx.Txid + " err=" + (int)postRes.Header.Error);
                return null;
            }
            return postRes;
        }
    }
}