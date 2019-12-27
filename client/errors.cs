
namespace XuperSDK
{
    /// <summary>enum <c>ErrorCode</c> is the error code of XChainClient requests</summary>
    public enum ErrorCode
    {
        /// <value>Enum <c>Success</c> represents request execute successfully</value>
        Success = 0,

        /// <value>Enum <c>ParamError</c> represents parameter error</value>
        ParamError,

        /// <value>Enum <c>ConnectFailed</c> represents failed to connect to the node</value>
        ConnectFailed,

        /// <value>Enum <c>UTXONotEnough</c> represents the user account's UTXO is not enough</value>
        UTXONotEnough,

        /// <value>Enum <c>SelectUTXOError</c> represents the select UTXO failed</value>
        SelectUTXOError,

        /// <value>Enum <c>PostError</c> represents post transaction failed</value>
        PostError,

        /// <value>Enum <c>UnknownError</c> represents unknown error</value>
        UnknownError,
    }

    /// <summary>enum <c>XChainError</c> is the error of XChainClient requests</summary>
    public class XChainError
    {
        /// <value>Property <c>ErrorCode</c> represents the error code</value>
        public ErrorCode ErrorCode { get; set; }

        /// <value>Property <c>ErrorMessage</c> represents the error message</value>
        public string ErrorMessage { get; set; }

        private static string GetErrorMessageByCode(ErrorCode code)
        {
            string res = "";
            switch (code)
            {
                case ErrorCode.Success:
                    res = "Success";
                    break;
                case ErrorCode.ParamError:
                    res = "Invalid parameters, please check the params again";
                    break;
                case ErrorCode.ConnectFailed:
                    res = "Failed to connect to service endpoint, please check the xchain config";
                    break;
                case ErrorCode.UTXONotEnough:
                    res = "UTXO not enough, please check your balance and make sure UTXO is not frozen";
                    break;
                case ErrorCode.SelectUTXOError:
                    res = "Select UTXO failed, cannot get UTXO result of this account";
                    break;
                case ErrorCode.PostError:
                    res = "Failed to post this transaction, please check internal error message";
                    break;
                default:
                    res = "Unknown error";
                    break;
            }

            return res;
        }

        /// <summary> get error by error code </summary>
        public static XChainError GetErrorByCode(ErrorCode code)
        {
            var error = new XChainError
            {
                ErrorCode = code,
            };

            return error;
        }
    }
}