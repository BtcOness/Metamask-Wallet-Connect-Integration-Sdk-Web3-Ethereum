using Leguar.TotalJSON;
using Nethereum.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.Networking;
using static GlobalData;

class TransactionTokenItem
{
    public string toAddr;
    public string fromAddr;
    public string tokenAddr;
    public decimal amount;
    public string tokenName;
    public bool isSpam;

    public TransactionTokenItem(string fromAddr, string toAddr, string tokenAddr, string amount, bool isSpam)
    {
        this.toAddr = toAddr;
        this.fromAddr = fromAddr;
        this.tokenAddr = tokenAddr;
        this.amount = decimal.Parse(amount);
        this.isSpam = isSpam;
        
        if (tokenDecimals.TryGetValue(tokenAddr, out int value))
        {
            this.amount = this.amount / Mathf.FloorToInt(Mathf.Pow(10, value));
        }
        else
        {
            this.amount = this.amount / Mathf.FloorToInt(Mathf.Pow(10, 9));
        }

        if(addressConstants.TryGetValue(tokenAddr, out string tokenName))
        {
            this.tokenName = tokenName;
        }
        else
        {
            this.tokenName = "";
        }
    }

    public override string ToString()
    {
        return $"{tokenName}-{fromAddr}-{toAddr}-{amount}";
    }
}

class TransactionItem
{
    public string transactionHash;
    public string fromAddr, toAddr;
    public int Nonce;
    public decimal amount;
    public decimal transactionFee, gasPrice;
    public DateTime timeStamp;
    public TransactionTokenItem tokenItem;

    public TransactionItem(string transactionHash, string fromAddr, string toAddr, string nonce, string amount, string gas, string gasPrice, string timeStamp)
    {
        this.transactionHash = transactionHash;
        this.fromAddr = fromAddr;
        this.toAddr = toAddr;
        this.Nonce = int.Parse(nonce);
        this.amount = UnitConversion.Convert.FromWei(BigInteger.Parse(amount));
        this.gasPrice = UnitConversion.Convert.FromWei(BigInteger.Parse(gasPrice), UnitConversion.EthUnit.Gwei);
        this.timeStamp = DateTime.Parse(timeStamp);
        this.transactionFee = decimal.Multiply(int.Parse(gas), UnitConversion.Convert.FromWei(BigInteger.Parse(gasPrice)));
        tokenItem = null;
    }

    public TransactionItem(string transactionHash, string fromAddr, string toAddr, string tokenAddr, string amount, string timeStamp, bool isSpam)
    {
        this.transactionHash = transactionHash;
        this.fromAddr = fromAddr;
        this.toAddr = tokenAddr;
        this.Nonce = 0;
        this.amount = 0;
        this.gasPrice = 0;
        this.timeStamp = DateTime.Parse(timeStamp);
        this.transactionFee = 0;
        tokenItem = new TransactionTokenItem(fromAddr, toAddr, tokenAddr, amount, isSpam);
    }

    public override string ToString()
    {
        return $"{transactionHash}-{fromAddr}-{toAddr}-{Nonce}-{amount}-{gasPrice}-{timeStamp}-{transactionFee}\n" +
            $"token: {tokenItem}";
    }
}

public class TransactionActivity : MonoBehaviour
{
    public static event Action<bool, string> onSuccessGetActivities;

    List<TransactionItem> transactionList = new List<TransactionItem>();

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GetActivityListByAddress()
    {
        transactionList.Clear();
        string currencySymbol = TCGWallet.Instance.CurrencySymbol;
        WalletNetwork networkChain = TCGWallet.Instance.walletNetwork;

        string userWalletAddress = TCGWallet.Instance.GetCurrentWalletAddress();
        if (userWalletAddress == "") return;
        StartCoroutine(GetNativeActivitiesAsync(userWalletAddress, networkChain));
        StartCoroutine(GetTokenActivitiesAsync(userWalletAddress, networkChain));
    }

    IEnumerator GetNativeActivitiesAsync(string walletAddress, WalletNetwork network)
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
        string url = $"https://deep-index.moralis.io/api/v2/{walletAddress}?chain={chain}";

        UnityWebRequest uwr = UnityWebRequest.Get(url);
        uwr.SetRequestHeader("accept", "application/json");
        uwr.SetRequestHeader("X-API-Key", moralisAPIKey);
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
            onSuccessGetActivities?.Invoke(false, "Network Error!");
        }
        else
        {
            string result = uwr.downloadHandler.text;
            JSON json = JSON.ParseString(result);
            JArray jsonArray = json.GetJArray("result");
            foreach (JValue jValue in jsonArray.Values)
            {
                JSON resultJson = JSON.ParseString(jValue.CreateString());
                string hash = resultJson.GetString("hash");
                string fromAddr = resultJson.GetString("from_address");
                string toAddr = resultJson.GetString("to_address");
                string nonce = resultJson.GetString("nonce");
                string amount = resultJson.GetString("value");
                string gas = resultJson.GetString("receipt_gas_used");
                string gasPrice = resultJson.GetString("gas_price");
                string time = resultJson.GetString("block_timestamp");

                transactionList.Add(new TransactionItem(
                    hash,
                    fromAddr,
                    toAddr,
                    nonce,
                    amount,
                    gas,
                    gasPrice,
                    time
                ));
            }
        }
    }

    IEnumerator GetTokenActivitiesAsync(string walletAddress, WalletNetwork network)
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
        string url = $"https://deep-index.moralis.io/api/v2/{walletAddress}/erc20/transfers?chain={chain}";

        UnityWebRequest uwr = UnityWebRequest.Get(url);
        uwr.SetRequestHeader("accept", "application/json");
        uwr.SetRequestHeader("X-API-Key", moralisAPIKey);
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
            onSuccessGetActivities?.Invoke(false, "Network Error!");
        }
        else
        {
            string result = uwr.downloadHandler.text;
            JSON json = JSON.ParseString(result);
            JArray jsonArray = json.GetJArray("result");
            foreach (JValue jValue in jsonArray.Values)
            {
                JSON resultJson = JSON.ParseString(jValue.CreateString());
                string hash = resultJson.GetString("transaction_hash");
                string fromAddr = resultJson.GetString("from_address");
                string toAddr = resultJson.GetString("to_address");
                string tokenAddr = resultJson.GetString("address");
                string amount = resultJson.GetString("value");
                bool isSpam = resultJson.GetBool("possible_spam");
                string time = resultJson.GetString("block_timestamp");

                int tokenIndex = transactionList.FindIndex(x => x.transactionHash == hash);

                if (tokenIndex > -1)
                {
                    transactionList[tokenIndex].tokenItem = new TransactionTokenItem(fromAddr, toAddr, tokenAddr, amount, isSpam);
                }
                else
                {
                    transactionList.Add(new TransactionItem(hash, fromAddr, toAddr, tokenAddr, amount, time, isSpam));
                }
            }
        }

        transactionList = transactionList.OrderByDescending(o => o.timeStamp).ToList();
        onSuccessGetActivities?.Invoke(true, "");

        print(transactionList[0]);
    }
}
