# XuperUnion C# SDK

This is a sample C# SDK for XuperUnion.

## 1. Features

Now only support the following operations:

* Query Account Balance
* Query TX
* Transfer
* Create Contract Account
* Deploy WASM Contract
* Invoke Contract
* Query Block
* Query Account's Contract
* Query Address in which Contract Accounts

## 2. Requirements

This SDK is based on `.Net Core 3.x`, please make sure you've installed lastes .Net Core SDK on your environment.

Windows/Linux/MacOS are all supported, but only tested on MacOS :).

## 3. Usage

### 3.1 install

Install XChainSDK from dotnet cli:
```
dotnet add package XChainSDK
```

Or you can install from Visual Studio in the nuget package manager, find "XChainSDKRC" and install it.


### 3.2 play

Create a instance of `XChainSDK.XChainClient`.

The following code snippet initialize SDK client with a private key store at `./data/keys` folder and the XuperUnion node's GRPC endpoint `127.0.0.1:37101`.

```
var client = new XChainClient();
if (!client.Init("./data/keys", "127.0.0.1:37101"))
{
    Console.WriteLine("Create client failed");
    return;
}
```

Please make sure the GRPC endpoint is valid, otherwise exception would be throwed in runtime.