using System;
using System.Numerics;
using System.Text.Json;

namespace XuperSDK
{
    /// <summary>class <c>ContactVMType</c> is the wrapper for contract VM Type</summary>
    public class ContactVMType
    {
        /// <summary>enum <c>Type</c> is the contract VM type</summary>
        public enum Type
        {
            /// <value>Enum <c>WASM</c> represents WASM contract VM</value>
            WASM = 0,

            /// <value>Enum <c>Native</c> represents Native contract VM</value>
            Native,

            /// <value>Enum <c>Native</c> represents EVM contract VM</value>
            EVM,

            /// <value>Enum <c>Native</c> represents Kernel contract VM</value>
            XKernel,
        }

        /// <summary>get contract VM type by contract VM name</summary>
        public static string GetNameByType(Type type)
        {
            switch (type)
            {
                case Type.WASM: return "wasm";
                case Type.Native: return "native";
                case Type.EVM: return "evm";
                case Type.XKernel: return "xkernel";
                default:
                    throw new Exception("Unknown type");
            }
        }

        /// <summary>get contract VM name by contract VM type</summary>
        public static Type GetTypeByName(string name)
        {
            switch (name.ToLower())
            {
                case "wasm": return Type.WASM;
                case "native": return Type.Native;
                case "evm": return Type.EVM;
                case "xkernel": return Type.XKernel;
                default:
                    throw new Exception("Unknown name");
            }

        }
    }

    // Account private key
    /// <summary>class <c>AccountPrivateKey</c> is the account private key</summary>
    public class AccountPrivateKey
    {
        private string rawkey;

        /// <value>Property <c>Curvname</c> represents the name of ECC Curve.</value>
        public string Curvname { get; set; }

        /// <value>Property <c>X</c> represents the X of ECC Key Point.</value>
        public BigInteger X { get; set; }

        /// <value>Property <c>Y</c> represents the Y of ECC Key Point.</value>
        public BigInteger Y { get; set; }

        /// <value>Property <c>D</c> represents the D of ECC Key Point.</value>
        public BigInteger D { get; set; }

        /// <value>Property <c>RawKey</c> represents the raw key in JSON format.</value>
        public string RawKey
        {
            get
            {
                return rawkey;
            }
        }

        /// <summary> parse key from JSON format</summary>
        public void ParseJSON(string json)
        {
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            };

            using (JsonDocument document = JsonDocument.Parse(json, options))
            {
                this.Curvname = document.RootElement.GetProperty("Curvname").GetString();
                var xStr = document.RootElement.GetProperty("X").GetRawText();
                var yStr = document.RootElement.GetProperty("Y").GetRawText();
                var dStr = document.RootElement.GetProperty("D").GetRawText();
                this.X = BigInteger.Parse(xStr);
                this.Y = BigInteger.Parse(yStr);
                this.D = BigInteger.Parse(dStr);
            }
            this.rawkey = json;
        }
    }

    // Account public keys
    /// <summary>class <c>AccountPublicKey</c> is the account public key</summary>
    public class AccountPublicKey
    {
        private string rawkey;

        /// <value>Property <c>Curvname</c> represents the name of ECC Curve.</value>
        public string Curvname { get; set; }

        /// <value>Property <c>X</c> represents the X of ECC Key Point.</value>
        public BigInteger X { get; set; }

        /// <value>Property <c>Y</c> represents the Y of ECC Key Point.</value>
        public BigInteger Y { get; set; }

        /// <value>Property <c>RawKey</c> represents the raw key in JSON format.</value>
        public string RawKey
        {
            get
            {
                return rawkey;
            }
        }

        /// <summary> parse key from JSON format</summary>
        public void ParseJSON(string json)
        {
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            };

            using (JsonDocument document = JsonDocument.Parse(json, options))
            {
                this.Curvname = document.RootElement.GetProperty("Curvname").GetString();
                var xStr = document.RootElement.GetProperty("X").GetRawText();
                var yStr = document.RootElement.GetProperty("Y").GetRawText();
                this.X = BigInteger.Parse(xStr);
                this.Y = BigInteger.Parse(yStr);
            }
            this.rawkey = json;
        }
    }

    // xuperchain user account
    /// <summary>class <c>XCAccount</c> is the xuperchain user account</summary>
    public class XCAccount
    {
        // the address of user account
        /// <value>Property <c>Address</c> represents the address of user account.</value>
        public string Address { get; set; }

        // user account's private key
        /// <value>Property <c>PrivateKey</c> represents the private key of user account.</value>
        public AccountPrivateKey PrivateKey { get; set; }

        // user account's public key
        /// <value>Property <c>PublicKey</c> represents the public key of user account.</value>
        public AccountPublicKey PublicKey { get; set; }
    }

    // contract status 
    /// <summary>class <c>ContractStatus</c> is the status of a contract</summary>
    public class ContractStatus
    {
        /// <value>Property <c>Name</c> represents the contract name</value>
        public string Name { get; set; }

        /// <value>Property <c>Desc</c> represents the contract description</value>
        public string Desc { get; set; }

        /// <value>Property <c>Desc</c> represents whether the contract is banned or not</value>
        public bool IsBanned { get; set; }
    }

    // the response of client
    /// <summary>class <c>Response</c> is the wrapper for XChainClient's response</summary>
    public class Response
    {
        /// <value>Property <c>Error</c> represents the error status of a request</value>
        public XChainError Error { get; set; }

        /// <value>Property <c>Txid</c> represents the Txid of a response, could be empty</value>
        public string Txid { get; set; }

        /// <value>Property <c>Data</c> represents the data of response, 
        ///     it have different type for different method</value>
        public dynamic Data { get; set; }
    }
}