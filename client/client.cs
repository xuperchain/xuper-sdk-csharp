using System;
using System.IO;
using Grpc.Net.Client;
using System.Numerics;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using Google.Protobuf;

namespace XChainSDK
{
    /// <summary>
    ///     class <c>XChainClient</c> is the client of XChainSDK, use a instance of XChainClient 
    ///     to interact with XuperChain Node.
    ///     Note that please call <c>Init</c> method before other methods to initialize XChainClient.
    ///</summary>
    public partial class XChainClient
    {
        private const string SwitchName = "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport";

        // internal members
        private XCAccount account;
        private Pb.Xchain.XchainClient client;

        // public properties
        // user account info
        /// <value>Property <c>Account</c> represents user account info</value>
        public XCAccount Account
        {
            get
            {
                return account;
            }
            set
            {
                account = value;
            }
        }

        // Initialize XChainClient, must call this method after XChainClient instance created.
        /// <summary>Initialize XChainClient, must call this method after XChainClient instance created.</summary>
        /// <param name="keypath">the folder contains user's address and private key</param> 
        /// <param name="targetHost">the GRPC endpoint of xuperchain node, e.g. 127.0.0.1:37101</param> 
        /// <returns>the balance of given address</returns> 
        public bool Init(string keypath, string targetHost)
        {
            try
            {
                AppContext.SetSwitch(SwitchName, true);
                var channel = GrpcChannel.ForAddress("http://" + targetHost);
                this.client = new Pb.Xchain.XchainClient(channel);
                this.account = new XCAccount();
                return SetAccountByPath(keypath);
            }
            catch (Exception e)
            {
                Console.WriteLine("Init failed, err=" + e.ToString());
                return false;
            }
        }

        // Set the data path of the account address and private key
        /// <summary>Set the data path of the account address and private key</summary>
        /// <param name="path">the folder contains user's address and private key</param> 
        /// <returns>return true if set success</returns> 
        public bool SetAccountByPath(string path)
        {
            try
            {
                using (StreamReader sr = new StreamReader(path + "/address"))
                {
                    var addr = sr.ReadToEnd();
                    this.account.Address = addr;
                }
                using (StreamReader sr = new StreamReader(path + "/private.key"))
                {
                    var privkeyStr = sr.ReadToEnd();
                    var privkey = new AccountPrivateKey();
                    privkey.ParseJSON(privkeyStr);
                    this.account.PrivateKey = privkey;
                }
                using (StreamReader sr = new StreamReader(path + "/public.key"))
                {
                    var pubkeyStr = sr.ReadToEnd();
                    var pubkey = new AccountPublicKey();
                    pubkey.ParseJSON(pubkeyStr);
                    this.account.PublicKey = pubkey;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in SetAccountByPath, err=" + e);
                return false;
            }

            return true;
        }

        // Get the balance of given address
        /// <summary>Get the balance of given address</summary>
        /// <param name="bcname">the name of the blockchain</param> 
        /// <param name="address">the address to query</param> 
        /// <returns>the balance of given address</returns> 
        public BigInteger GetBalance(string bcname, string address)
        {
            if (client == null)
            {
                return new BigInteger(0);
            }
            var reqData = new Pb.AddressStatus
            {
                Address = address,
            };
            var tokenDetail = new Pb.TokenDetail
            {
                Bcname = bcname,
            };
            reqData.Bcs.Add(tokenDetail);
            var res = client.GetBalance(reqData);
            if (res.Header.Error != Pb.XChainErrorEnum.Success)
            {
                return new BigInteger(0);
            }
            var balanceStr = res.Bcs[0].Balance;
            var balance = BigInteger.Parse(balanceStr);
            return balance;
        }

        // Get the balance of current address
        /// <summary>Get the balance of current address</summary>
        /// <param name="bcname">the name of the blockchain</param> 
        /// <returns>the balance of current address</returns> 
        public BigInteger GetBalance(string bcname)
        {
            return GetBalance(bcname, Account.Address);
        }

        // Transfer given amount of UTXO resource to another address
        /// <summary>Transfer given amount of UTXO resource to another address</summary>
        /// <param name="bcname">the name of the blockchain</param> 
        /// <param name="to">the receiver address</param> 
        /// <param name="amount">the amount of UTXO resource</param>
        /// <param name="desc">addtional information attached to this transaction</param>
        /// <return>the response contains the Txid of this transaction</return>
        public Response Transfer(string bcname, string to, BigInteger amount, string desc)
        {
            var response = new Response()
            {
                Error = new XChainError()
                {
                    ErrorCode = ErrorCode.Success,
                    ErrorMessage = "Success",
                }
            };
            if (string.IsNullOrEmpty(bcname) || string.IsNullOrEmpty(to))
            {
                return MakeErrorResponse(ErrorCode.ParamError, null);
            }
            var utxo = SelectUTXO(bcname, this.Account.Address, this.Account.PublicKey, amount);
            if (utxo == null)
            {
                Console.WriteLine("Select utxo failed");
                return MakeErrorResponse(ErrorCode.SelectUTXOError, null);
            }
            var tx = AssembleTx(utxo, this.Account, null, to, amount, null, desc);
            if (tx == null)
            {
                Console.WriteLine("AssembleTx failed");
                return MakeErrorResponse(ErrorCode.UnknownError, null);
            }
            // post transaction
            var postRes = PostTx(bcname, tx);
            if (postRes == null || postRes.Header.Error != Pb.XChainErrorEnum.Success)
            {
                Console.WriteLine("InvokeContract: PostTx failed");
                return MakeErrorResponse(ErrorCode.PostError, null);
            }
            response.Txid = XConvert.ByteArrayToHexString(tx.Txid).ToLower();
            return response;
        }

        // Query a transaction with transaction id
        /// <summary>Query a transaction with transaction id</summary>
        /// <param name="bcname">the name of blockchain</param> 
        /// <param name="txid">transaction id</param> 
        /// <return>the request response, the <c>Data</c> field is a Transaction type</return>
        public Response QueryTx(string bcname, string txid)
        {
            var response = new Response()
            {
                Error = new XChainError()
                {
                    ErrorCode = ErrorCode.Success,
                    ErrorMessage = "Success",
                }
            };
            if (string.IsNullOrEmpty(bcname) || string.IsNullOrEmpty(txid))
            {
                return MakeErrorResponse(ErrorCode.ParamError, null);
            }
            var req = new Pb.TxStatus
            {
                Header = GetDefaultHeader(),
                Bcname = bcname,
                Txid = ByteString.CopyFrom(XConvert.HexStringToByteArray(txid)),
            };
            var tx = client.QueryTx(req);
            if (tx.Header.Error != Pb.XChainErrorEnum.Success)
            {
                Console.WriteLine("query tx failed. errcode=" + (int)tx.Header.Error + ", logid=" + req.Header.Logid);
                return MakeErrorResponse(ErrorCode.UnknownError, null);
            }
            response.Data = XConvert.PbTxToLocalTx(tx.Tx);
            return response;
        }

        // Query a block with block id
        /// <summary>Query a block with block id</summary>
        /// <param name="bcname">the name of blockchain</param> 
        /// <param name="blockid">block id</param> 
        /// <return>the request response, the <c>Data</c> field is a Block type</return>
        public Response QueryBlock(string bcname, string blockid)
        {
            var req = new Pb.BlockID
            {
                Header = GetDefaultHeader(),
                Bcname = bcname,
                Blockid = ByteString.CopyFrom(XConvert.HexStringToByteArray(blockid)),
                NeedContent = true,
            };
            var res = client.GetBlock(req);
            if (res.Header.Error != Pb.XChainErrorEnum.Success)
            {
                Console.WriteLine("query block failed. errcode=" + (int)res.Header.Error +
                    ", logid=" + req.Header.Logid);
                return MakeErrorResponse(ErrorCode.ConnectFailed, null);
            }
            var block = XConvert.PbBlockToLocalBlock(res);
            return MakeResponse("", block);
        }

        // Create contract account using given accountName
        // TODO: Customized ACL is not supproted currently.
        /// <summary>Create contract account using given accountName. 
        ///     Customized ACL is not supproted currently.</summary>
        /// <param name="bcname">the name of the blockchain</param> 
        /// <param name="accountName">the name of the contract account. 
        ///     Note that `accountName` is a 16-digits string, like "0000000000000001"</param> 
        /// <returns>Response instance, the <c>Data</c> field is the complete name of new contract account</returns>
        public Response NewContractAccount(string bcname, string accountName)
        {
            if (string.IsNullOrEmpty(bcname) || string.IsNullOrEmpty(accountName))
            {
                return MakeErrorResponse(ErrorCode.ParamError, null);
            }
            var args = new SortedDictionary<string, byte[]>();
            args["account_name"] = Encoding.ASCII.GetBytes(accountName);
            args["acl"] = Encoding.ASCII.GetBytes("{\"pm\":{\"rule\":1,\"acceptValue\":1.0},\"aksWeight\":{\"" +
                this.Account.Address + "\":1.0}}");
            var invokeRes = InvokeContract(bcname, null, "NewAccount", args, null, "", ContactVMType.Type.XKernel);
            if (invokeRes.Error.ErrorCode != ErrorCode.Success)
            {
                Console.WriteLine("NewContractAccount failed");
                return invokeRes;
            }
            return new Response
            {
                Error = new XChainError
                {
                    ErrorCode = ErrorCode.Success,
                },
                Data = "XC" + accountName + "@" + bcname,
            };
        }

        // DeployWASMContract deploy a WASM contract.
        /// <summary>DeployWASMContract deploy a WASM contract.</summary>
        /// <param name="bcname">the name of blockchain</param> 
        /// <param name="contractName">the name of contract to deploy, unique on blockchain</param> 
        /// <param name="path">the path of built WASM binary</param> 
        /// <param name="accountName">deploy contract using which contract account</param> 
        /// <param name="initArgs">initializing arguments of contract</param> 
        /// <param name="runtime">runtime of the contract, "c" for C/C++, "go" for Golang</param> 
        /// <param name="desc">description info, default to empty</param> 
        public Response DeployWASMContract(string bcname, string contractName, string path, string accountName,
             Dictionary<string, byte[]> initArgs, string runtime, string desc = "")
        {
            if (string.IsNullOrEmpty(bcname) || string.IsNullOrEmpty(contractName) ||
                string.IsNullOrEmpty(path) || string.IsNullOrEmpty(accountName))
            {
                return MakeErrorResponse(ErrorCode.ParamError, null);
            }
            var args = new SortedDictionary<string, byte[]>();
            using (StreamReader sr = new StreamReader(path))
            {
                args["account_name"] = Encoding.ASCII.GetBytes(accountName);
                args["contract_name"] = Encoding.ASCII.GetBytes(contractName);
                using (var ms = new MemoryStream())
                {
                    sr.BaseStream.CopyTo(ms);
                    args["contract_code"] = ms.ToArray();
                }
                using (var ms = new MemoryStream())
                {
                    var contractDesc = new Pb.WasmCodeDesc
                    {
                        Runtime = runtime,
                    };
                    contractDesc.WriteTo(ms);
                    args["contract_desc"] = ms.ToArray();
                }
                args["init_args"] = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(initArgs));
                var authRequire = new List<string>();
                authRequire.Add(accountName + "/" + this.Account.Address);
                var invokeRes = InvokeContract(bcname, contractName, "Deploy", args, authRequire, desc, ContactVMType.Type.XKernel);
                if (invokeRes.Error.ErrorCode != ErrorCode.Success)
                {
                    Console.WriteLine("DeployContract failed");
                    return invokeRes;
                }
                return new Response
                {
                    Error = new XChainError
                    {
                        ErrorCode = ErrorCode.Success,
                    },
                    Data = invokeRes,
                };
            }
        }

        // Invokes contract method with given args
        // TODO: multisig is not supported
        /// <summary>Invokes contract method with given args.
        ///     TODO: multisig is not supported</summary>
        /// <param name="bcname">the name of blockchain</param> 
        /// <param name="contractName">the name of contract to invoke</param> 
        /// <param name="method">the method name of contract to invoke</param> 
        /// <param name="args">the arguments of contract</param> 
        /// <param name="authRequire">add more address if multisig needed, otherwise keep null</param> 
        /// <param name="contractType">the contract VM type, default to WASM</param> 
        /// <param name="desc">description info, default to empty</param> 
        public Response InvokeContract(string bcname, string contractName, string method,
            SortedDictionary<string, byte[]> args, List<string> authRequire = null, string desc = "",
            ContactVMType.Type contractType = ContactVMType.Type.WASM)
        {
            if (string.IsNullOrEmpty(bcname) || string.IsNullOrEmpty(method))
            {
                return MakeErrorResponse(ErrorCode.ParamError, null);
            }
            // pre-execute contract
            var execRes = PreExecWithSelectUTXO(bcname, this.Account.Address,
                this.Account.PrivateKey, 0, contractName, method, args, contractType,
                this.Account.Address, authRequire);
            if (execRes == null)
            {
                Console.WriteLine("InvokeContract: PreExecWithSelectUTXO failed");
                return MakeErrorResponse(ErrorCode.PostError, null);
            }
            // check contract response
            var contractResult = new Dictionary<string, string>();
            for (var i = 0; i < execRes.Response.Responses.Count; i++)
            {
                if (execRes.Response.Responses[i].Status >= 400)
                {
                    Console.WriteLine("Contract execute failed. res=" +
                        JsonConvert.SerializeObject(execRes.Response.Responses[i]));
                    return new Response
                    {
                        Error = new XChainError
                        {
                            ErrorCode = ErrorCode.Success,
                            ErrorMessage = "Success",
                        },
                    };
                }
                contractResult.Add(execRes.Response.Requests[i].ContractName + ":" + execRes.Response.Requests[i].MethodName,
                    Encoding.ASCII.GetString(execRes.Response.Responses[i].Body.ToByteArray()));
            }

            // assemble transaction
            var tx = AssembleTx(execRes.UtxoOutput, this.Account, authRequire, "", 0, execRes.Response, desc);
            if (tx == null)
            {
                Console.WriteLine("InvokeContract: AssembleTx failed");
                return MakeErrorResponse(ErrorCode.UnknownError, null);
            }

            // post transaction
            var postRes = PostTx(bcname, tx);
            if (postRes == null || postRes.Header.Error != Pb.XChainErrorEnum.Success)
            {
                Console.WriteLine("InvokeContract: PostTx failed");
                return MakeErrorResponse(ErrorCode.PostError, null);
            }
            var res = new Response
            {
                Error = new XChainError
                {
                    ErrorCode = ErrorCode.Success,
                },
                Txid = XConvert.ByteArrayToHexString(tx.Txid),
                Data = contractResult,
            };
            return res;
        }

        // Get all contracts of a contract account
        /// <summary>Get all contracts of a contract account</summary>
        /// <param name="bcname">the name of the blockchain</param> 
        /// <param name="account">the name of the contract account</param> 
        /// <returns>Response instance, the <c>Data</c> field is instance of List&lt;ContractStatus&gt;.</returns> 
        public Response GetContractsByAccount(string bcname, string account)
        {
            var req = new Pb.GetAccountContractsRequest
            {
                Header = GetDefaultHeader(),
                Bcname = bcname,
                Account = account,
            };
            var res = client.GetAccountContracts(req);
            if (res == null || res.Header.Error != Pb.XChainErrorEnum.Success)
            {
                return MakeErrorResponse(ErrorCode.UnknownError, null);
            }
            var clist = new List<ContractStatus>();
            foreach (var ac in res.ContractsStatus)
            {
                clist.Add(new ContractStatus
                {
                    Name = ac.ContractName,
                    Desc = ac.Desc.ToBase64(),
                    IsBanned = ac.IsBanned,
                });
            }
            return MakeResponse("", clist);
        }

        // Get all contract accounts in which the ACL contain current address
        /// <summary>Get all contract accounts in which the ACL contain current address</summary>
        /// <param name="bcname">the name of the blockchain</param> 
        /// <returns>Response instance, the <c>Data</c> field is instance of List&lt;string&gt;, 
        /// means list of contract accounts' name.</returns> 
        public Response GetContractAccounts(string bcname)
        {
            var req = new Pb.AK2AccountRequest
            {
                Header = GetDefaultHeader(),
                Bcname = bcname,
                Address = this.Account.Address,
            };
            var res = client.GetAccountByAK(req);
            if (res == null || res.Header.Error != Pb.XChainErrorEnum.Success)
            {
                return MakeErrorResponse(ErrorCode.UnknownError, null);
            }
            var alist = new List<string>();
            foreach (var acc in res.Account)
            {
                alist.Add(acc);
            }
            return MakeResponse("", alist);
        }

        private Response MakeErrorResponse(ErrorCode errorCode, dynamic data)
        {
            var reponse = new Response
            {
                Error = XChainError.GetErrorByCode(errorCode),
                Data = data,
            };
            return reponse;
        }

        private Response MakeResponse(string txid, dynamic data)
        {
            var reponse = new Response
            {
                Error = XChainError.GetErrorByCode(ErrorCode.Success),
                Txid = txid,
                Data = data,
            };
            return reponse;
        }
    }
}