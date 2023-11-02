using Leguar.TotalJSON;
using Nethereum.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Networking;
using static GlobalData;

class TokenBalance
{
    public string tokenAddress;
    public string tokenName;
    public string tokenSymbol;
    public decimal balance;
    public bool isPossibleSpam;

    public TokenBalance(string tokenAddress, string tokenName, string tokenSymbol, int decimalNumber, string balance, bool isPossibleSpam)
    {
        this.tokenAddress = tokenAddress;
        this.tokenName = tokenName;
        this.tokenSymbol = tokenSymbol;
        this.balance = decimal.Parse(balance) / Mathf.FloorToInt(MathF.Pow(10, decimalNumber));
        this.isPossibleSpam = isPossibleSpam;
    }

    public override string ToString()
    {
        return $"{tokenName}-{balance}-{tokenSymbol}";
    }
}

public class CrosschainBalance : MonoBehaviour
{
    public static event Action<bool, string> onSuccessGetBalance;

    List<TokenBalance> balanceList = new List<TokenBalance>();

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadCurrentBalance()
    {
        WalletNetwork networkChain = TCGWallet.Instance.walletNetwork;
        string walletAddress = TCGWallet.Instance.GetCurrentWalletAddress();
        StartCoroutine(GetNativeBalance(walletAddress, networkChain));
    }

    public void LoadTokenBalance()
    {
        balanceList.Clear();
        WalletNetwork networkChain = TCGWallet.Instance.walletNetwork;
        string walletAddress = TCGWallet.Instance.GetCurrentWalletAddress();
        StartCoroutine(GetTokenBalance(walletAddress, networkChain));
    }

    IEnumerator GetNativeBalance(string walletAddress, WalletNetwork network)
    {
        string chain = "eth";
        switch (network)
        {
            case WalletNetwork.Ethereum:
                chain = "eth";
                break;
            case WalletNetwork.Binance:
                chain = "bsc";
                break;
            case WalletNetwork.EthereumTestnet:
                chain = "goerli";
                break;
            case WalletNetwork.BinanceTestnet:
                chain = "bsc%20testnet";
                break;
            default:
                chain = "eth";
                break;
        }

        JSON requestObject = new JSON();
        string url = $"https://deep-index.moralis.io/api/v2/{walletAddress}/balance?chain={chain}";

        UnityWebRequest uwr = UnityWebRequest.Get(url);
        uwr.SetRequestHeader("accept", "application/json");
        uwr.SetRequestHeader("X-API-Key", moralisAPIKey);
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
            onSuccessGetBalance?.Invoke(false, "Network Error!");
        }
        else
        {
            string result = uwr.downloadHandler.text;
            JSON json = JSON.ParseString(result);
            string tmp = json.GetString("balance");
            decimal balance = UnitConversion.Convert.FromWei(BigInteger.Parse(tmp));
        }
    }

    IEnumerator GetTokenBalance(string walletAddress, WalletNetwork network)
    {
        string chain = "eth";
        switch (network)
        {
            case WalletNetwork.Ethereum:
                chain = "eth";
                break;
            case WalletNetwork.Binance:
                chain = "bsc";
                break;
            case WalletNetwork.EthereumTestnet:
                chain = "goerli";
                break;
            case WalletNetwork.BinanceTestnet:
                chain = "bsc%20testnet";
                break;
            default:
                chain = "eth";
                break;
        }

        JSON requestObject = new JSON();
        string url = $"https://deep-index.moralis.io/api/v2/{walletAddress}/erc20?chain={chain}";

        UnityWebRequest uwr = UnityWebRequest.Get(url);
        uwr.SetRequestHeader("accept", "application/json");
        uwr.SetRequestHeader("X-API-Key", moralisAPIKey);
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
            onSuccessGetBalance?.Invoke(false, "Network Error!");
        }
        else
        {
            string result = uwr.downloadHandler.text;
            JSON json = JSON.ParseString("{\"result\":" + result + "}");
            JArray jsonArray = json.GetJArray("result");
            foreach (JValue jValue in jsonArray.Values)
            {
                JSON resultJson = JSON.ParseString(jValue.CreateString());
                string addr = resultJson.GetString("token_address");
                string name = resultJson.GetString("name");
                string symbol = resultJson.GetString("symbol");
                int decimalNumber = resultJson.GetInt("decimals");
                string balance = resultJson.GetString("balance");
                bool isSpam = resultJson.GetBool("possible_spam");
                balanceList.Add(new TokenBalance(addr, name, symbol, decimalNumber, balance, isSpam));
            }
        }

        print(balanceList[0]);
    }
}
