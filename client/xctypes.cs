using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace XChainSDK
{
    /// <summary>class <c>TxInput</c> is UTXO inputs of a transcation</summary>
    public class TxInput
    {
        /// <value>Property <c>RefTxid</c> represents the reference txid of the UTXO.</value>
        [JsonProperty(PropertyName = "ref_txid")]
        public byte[] RefTxid { get; set; }

        /// <value>Property <c>RefOffset</c> represents the offset in reference txid of the UTXO.</value>
        [JsonProperty(PropertyName = "ref_offset")]
        public int RefOffset { get; set; }

        /// <value>Property <c>FromAddr</c> represents the user address.</value>
        [JsonProperty(PropertyName = "from_addr")]
        public byte[] FromAddr { get; set; }

        /// <value>Property <c>Amount</c> represents the byte[] format of UTXO amount</value>
        [JsonProperty(PropertyName = "amount")]
        public byte[] Amount { get; set; }

        /// <value>Property <c>FrozenHeight</c> represents the forzen height</value>
        [JsonProperty(PropertyName = "frozen_height")]
        public Int64 FrozenHeight { get; set; }
    }

    /// <summary>class <c>TxOutput</c> is UTXO outputs of a transcation</summary>
    public class TxOutput
    {
        /// <summary>the amount of UTXO output</summary>
        [JsonProperty(PropertyName = "amount")]
        public byte[] Amount { get; set; }

        /// <summary>the receiver address of UTXO output</summary>
        [JsonProperty(PropertyName = "to_addr")]
        public byte[] ToAddr { get; set; }

        /// <summary>the frozen height of UTXO output</summary>
        [JsonProperty(PropertyName = "frozen_height")]
        public Int64 FrozenHeight { get; set; }
    }

    /// <summary>class <c>TxInputExt</c> is the contract read sets of a transcation</summary>
    public class TxInputExt
    {
        /// <summary>the bucket of contract input</summary>
        [JsonProperty(PropertyName = "bucket")]
        public string Bucket { get; set; }

        /// <summary>the key of contract input</summary>
        [JsonProperty(PropertyName = "key")]
        public byte[] Key { get; set; }

        /// <summary>the ref txid of input</summary>
        [JsonProperty(PropertyName = "ref_txid")]
        public byte[] RefTxid { get; set; }

        /// <summary>the ref txid offset of input</summary>
        [JsonProperty(PropertyName = "ref_offset")]
        public int RefOffset { get; set; }
    }

    /// <summary>class <c>TxOutputExt</c> is the contract write sets of a transcation</summary>
    public class TxOutputExt
    {
        /// <summary>the ref bucket of output</summary>
        [JsonProperty(PropertyName = "bucket")]
        public string Bucket { get; set; }

        /// <summary>the key of output</summary>
        [JsonProperty(PropertyName = "key")]
        public byte[] Key { get; set; }

        /// <summary>the value of output</summary>
        [JsonProperty(PropertyName = "value")]
        public byte[] Value { get; set; }
    }

    /// <summary>Enum <c>ResourceType</c> is the resource type</summary>
    public enum ResourceType
    {
        /// <value>Type <c>CPU</c> represents CPU type</value>
        CPU = 0,
        /// <value>Type <c>MEMORY</c> represents memory type</value>
        MEMORY,
        /// <value>Type <c>DISK</c> represents Disk type</value>
        DISK,
        /// <value>Type <c>XFEE</c> represents the fee used in kernel contract</value>
        XFEE
    }


    /// <summary>class <c>ResourceLimit</c> is the resource limit of a contract request</summary>
    public class ResourceLimit
    {
        /// <summary>the type of resource</summary>
        [JsonProperty(PropertyName = "type")]
        public ResourceType Type;

        /// <summary>the limit of resource</summary>
        [JsonProperty(PropertyName = "limit")]
        public Int64 Limit;
    }

    /// <summary>class <c>InvokeRequest</c> is a contract request</summary>
    public class InvokeRequest
    {
        /// <summary>module name</summary>
        [JsonProperty(PropertyName = "module_name")]
        public string ModuleName { get; set; }

        /// <summary>contract name</summary>
        [JsonProperty(PropertyName = "contract_name")]
        [DefaultValue("")]
        public string ContractName { get; set; }

        /// <summary>method name</summary>
        [JsonProperty(PropertyName = "method_name")]
        public string MethodName { get; set; }

        /// <summary>request arguments</summary>
        [JsonProperty(PropertyName = "args")]
        public SortedDictionary<string, byte[]> Args { get; set; }

        /// <summary>request resource limit</summary>
        [JsonProperty(PropertyName = "resource_limits")]
        public List<ResourceLimit> ResourceLimits { get; set; }

        /// <summary>request resource amount</summary>
        [JsonProperty(PropertyName = "amount")]
        public string Amount { get; set; }

        /// <summary>constructor</summary>
        public InvokeRequest()
        {
            this.Args = new SortedDictionary<string, byte[]>();
            this.ResourceLimits = new List<ResourceLimit>();
        }
    }

    /// <summary>class <c>SignatureInfo</c> is a signature information</summary>
    public class SignatureInfo
    {
        /// <summary>public key</summary>
        public string PublicKey { get; set; }

        /// <summary>signature bytes</summary>
        public byte[] Sign { get; set; }
    }

    // transaction data
    /// <summary>class <c>Transaction</c> is data of a transaction</summary>
    public class Transaction
    {
        /// <summary>transaction ID</summary>
        [JsonProperty(PropertyName = "txid")]
        public byte[] Txid { get; set; }

        /// <summary>block ID</summary>
        [JsonProperty(PropertyName = "blockid")]
        public byte[] Blockid { get; set; }

        /// <summary>UTXO Inputs</summary>
        [JsonProperty(PropertyName = "tx_inputs")]
        public List<TxInput> TxInputs { get; set; }

        /// <summary>UTXO Outputs</summary>
        [JsonProperty(PropertyName = "tx_outputs")]
        public List<TxOutput> TxOutputs { get; set; }

        /// <summary>description</summary>
        [JsonProperty(PropertyName = "desc")]
        public byte[] Desc { get; set; }

        /// <summary>is coinbase transaction</summary>
        [JsonProperty(PropertyName = "coinbase")]
        public bool Coinbase { get; set; }

        /// <summary>Nonce</summary>
        [JsonProperty(PropertyName = "nonce")]
        public string Nonce { get; set; }

        /// <summary>timestamp of this transaction</summary>
        [JsonProperty(PropertyName = "timestamp")]
        public Int64 Timestamp { get; set; }

        /// <summary>Tx Version</summary>
        [JsonProperty(PropertyName = "version")]
        public int Version { get; set; }

        /// <summary>is autogen tx</summary>
        [JsonProperty(PropertyName = "autogen")]
        public bool Autogen { get; set; }

        /// <summary>contract read set</summary>
        [JsonProperty(PropertyName = "tx_inputs_ext")]
        public List<TxInputExt> TxInputsExt { get; set; }

        /// <summary>contract write set</summary>
        [JsonProperty(PropertyName = "tx_outputs_ext")]
        public List<TxOutputExt> TxOutputsExt { get; set; }

        /// <summary>contract requests</summary>
        [JsonProperty(PropertyName = "contract_requests")]
        public List<InvokeRequest> ContractRequests { get; set; }

        /// <summary>initiator of this tx</summary>
        [JsonProperty(PropertyName = "initiator")]
        public string Initiator { get; set; }

        /// <summary>auth requires of this tx</summary>
        [JsonProperty(PropertyName = "auth_require")]
        public List<string> AuthRequire { get; set; }

        /// <summary>signatures of initiator</summary>
        [JsonProperty(PropertyName = "initiator_signs")]
        public List<SignatureInfo> InitiatorSigns { get; set; }

        /// <summary>signatures of auth requires</summary>
        [JsonProperty(PropertyName = "auth_require_signs")]
        public List<SignatureInfo> AuthRequireSigns { get; set; }

        /// <summary>the timestamp when this node received this tx</summary>
        [JsonProperty(PropertyName = "received_timestamp")]
        public Int64 ReceivedTimestamp { get; set; }

        ///<summary>constructor</summary>
        public Transaction()
        {
            this.Nonce = "";
            this.Initiator = "";
        }
    }

    // block status
    /// <summary>Enum <c>BlockStatus</c> is block status type</summary>
    public enum BlockStatus
    {
        /// <value>Type <c>ERROR</c> represents error</value>
        ERROR = 0,  // error

        /// <value>Type <c>TRUNK</c> represents in trunk</value>
        TRUNK,      // in trunk

        /// <value>Type <c>BRANCH</c> represents in a short branch</value>
        BRANCH,     // in a short branch

        /// <value>Type <c>NOEXIST</c> represents not exist</value>
        NOEXIST     // not exist
    }

    // Block data
    /// <summary>class <c>InternalBlock</c> is block data</summary>
    public class InternalBlock
    {
        /// <summary>block id</summary>
        public string Blockid { get; set; }

        /// <summary> the height of this block in trunk chain</summary>
        public Int64 Height { get; set; }

        /// <summary> the version of the block struct</summary>
        public int Version { get; set; }

        /// <summary> nonce is the random value for signature</summary>
        public int Nonce { get; set; }

        /// <summary> the previous block id</summary>
        public string PreHash { get; set; }

        /// <summary> the next block id (if have)</summary>
        public string NextHash { get; set; }

        /// <summary> the address of block producer</summary>
        public string Proposer { get; set; }

        /// <summary> the signature of this block</summary>
        public byte[] Sign { get; set; }

        /// <summary> the public key of the block producer</summary>
        public byte[] PublicKey { get; set; }

        /// <summary> the root of the transactions' merkle tree</summary>
        public string MerkleRoot { get; set; }

        /// <summary> the timestamp when this block is produced</summary>
        public Int64 Timestamp { get; set; }

        /// <summary> the count of the transactions in this block</summary>
        public int TxCount { get; set; }

        /// <summary>the list of transactions in this block</summary>
        public List<Transaction> Transactions { get; set; }

        /// <summary> a list of the transactions' merkle tree</summary>
        public List<string> MerkleTree { get; set; }

        /// <summary> the term of tdpos consensus for this block</summary>
        public Int64 CurTerm { get; set; }

        /// <summary> the block number in CurTerm of tdpos consensus for this block</summary>
        public Int64 CurBlockNum { get; set; }

        /// <summary> a list of failed transactions</summary>
        public Dictionary<string, string> FailedTxs { get; set; }
    }

    // Block wrapper
    /// <summary>class <c>InternalBlock</c> is block wrapper contains block status and data</summary>
    public class Block
    {
        /// <summary> block ID</summary>
        public string Blockid { get; set; }

        /// <summary> block status</summary>
        public BlockStatus Status { get; set; }

        /// <summary> block data</summary>
        public InternalBlock BlockData { get; set; }
    }
}