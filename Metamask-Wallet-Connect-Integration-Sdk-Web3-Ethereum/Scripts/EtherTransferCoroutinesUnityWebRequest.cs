using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.Contracts.CQS;
using Nethereum.Contracts.Extensions;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Unity.Rpc;
using Nethereum.Unity.FeeSuggestions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.TransactionManagers;
using Nethereum.Signer;
using Nethereum.Util;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using PaperPlaneTools;
using Nethereum.Web3;

public class EtherTransferCoroutinesUnityWebRequest : MonoBehaviour {

    public TMP_InputField InputAddressTo;
    public TMP_InputField InputAmount;

    public static event Action<bool, string, string> onSuccessSendTransaction;

    string Url = "http://localhost:8545";
    string PrivateKey = "0xb5b1870957d373ef0eeffecc6e4812c0fd08f554b37b233526acc331bf1544f7";
    string AddressTo = "0x8A922380aE0D287116AaF2aecE6743DA588AF349";
    int ChainID = 1;
    decimal Amount = 0.001m;
    decimal GasPriceGwei = 2;
    string TransactionHash = "";
    decimal BalanceAddressTo = 0m;

    public EtherTransferCoroutinesUnityWebRequest()
    {
        
    }
    // Use this for initialization
    void Start () {
        InputAddressTo.text = AddressTo;
        InputAmount.text = Amount.ToString();
    }

    public void TransferRequest()
    {
        new Alert("Confirm", $"Are you going to send {InputAmount.text} {TCGWallet.Instance.CurrencySymbol} to {InputAddressTo.text}?")
            .SetPositiveButton("Sign In", () => {
                StartCoroutine(TransferEther());
            })
            .Show();
    }

    public enum FeeStrategy
    {
        Legacy,
        TimePreference,
        MedianFeeHistory
    }

    //Sample of new features / requests
    public IEnumerator TransferEther()
    {
        Url = TCGWallet.Instance.RPCUrl;
        PrivateKey = TCGWallet.Instance.GetPrivateKey();
        AddressTo = InputAddressTo.text;
        Amount = Decimal.Parse(InputAmount.text);
        ChainID = TCGWallet.Instance.ChainID;

        //initialising the transaction request sender
        var ethTransfer = new EthTransferUnityRequest(Url, PrivateKey, ChainID);

        var receivingAddress = AddressTo;

        var feeStrategy = FeeStrategy.MedianFeeHistory;

        if (feeStrategy == FeeStrategy.TimePreference)
        {
            Debug.Log("Time Preference");
            var timePreferenceFeeSuggestion = new TimePreferenceFeeSuggestionUnityRequestStrategy(Url);

            yield return timePreferenceFeeSuggestion.SuggestFees();

            if (timePreferenceFeeSuggestion.Exception != null)
            {
                Debug.Log(timePreferenceFeeSuggestion.Exception.Message);
                onSuccessSendTransaction?.Invoke(false, timePreferenceFeeSuggestion.Exception.Message, "");
                yield break;
            }

            //lets get the first one so it is higher priority
            Debug.Log(timePreferenceFeeSuggestion.Result.Length);
            if (timePreferenceFeeSuggestion.Result.Length > 0)
            {
                Debug.Log(timePreferenceFeeSuggestion.Result[0].MaxFeePerGas);
                Debug.Log(timePreferenceFeeSuggestion.Result[0].MaxPriorityFeePerGas);
            }
            var fee = timePreferenceFeeSuggestion.Result[0];

            yield return ethTransfer.TransferEther(receivingAddress, Amount, fee.MaxPriorityFeePerGas.Value, fee.MaxFeePerGas.Value);
            if (ethTransfer.Exception != null)
            {
                Debug.Log("Error transferring Ether using Time Preference Fee Estimation Strategy: " + ethTransfer.Exception.Message);
                onSuccessSendTransaction?.Invoke(false, ethTransfer.Exception.Message, "");
                yield break;
            }
        }

        if(feeStrategy == FeeStrategy.MedianFeeHistory)
        {
            Debug.Log("MedianFeeHistory mode");
            var medianPriorityFeeStrategy = new MedianPriorityFeeHistorySuggestionUnityRequestStrategy(Url);

            yield return medianPriorityFeeStrategy.SuggestFee();

            if (medianPriorityFeeStrategy.Exception != null)
            {
                Debug.Log(medianPriorityFeeStrategy.Exception.Message);
                onSuccessSendTransaction?.Invoke(false, medianPriorityFeeStrategy.Exception.Message, "");
                yield break;
            }
            
            Debug.Log(medianPriorityFeeStrategy.Result.MaxFeePerGas);
            Debug.Log(medianPriorityFeeStrategy.Result.MaxPriorityFeePerGas);
            
            var fee = medianPriorityFeeStrategy.Result;

            yield return ethTransfer.TransferEther(receivingAddress, Amount, fee.MaxPriorityFeePerGas.Value, fee.MaxFeePerGas.Value);
            if (ethTransfer.Exception != null)
            {
                Debug.Log("Error transferring Ether using Median Fee History Fee Estimation Strategy: " + ethTransfer.Exception.Message);
                onSuccessSendTransaction?.Invoke(false, ethTransfer.Exception.Message, "");
                yield break;
            }
        }

        if (feeStrategy == FeeStrategy.Legacy)
        {
            Debug.Log("Legacy mode");
            //I am forcing the legacy mode but also I am including the gas price
            ethTransfer.UseLegacyAsDefault = true;

            yield return ethTransfer.TransferEther(receivingAddress, Amount, GasPriceGwei);

            if (ethTransfer.Exception != null)
            {
                Debug.Log("Error transferring Ether using Legacy Gas Price:  " + ethTransfer.Exception.Message);
                onSuccessSendTransaction?.Invoke(false, ethTransfer.Exception.Message, "");
                yield break;
            }

        }

        TransactionHash = ethTransfer.Result;
        Debug.Log("Transfer transaction hash:" + TransactionHash);

        //create a poll to get the receipt when mined
        var transactionReceiptPolling = new TransactionReceiptPollingRequest(Url);
        //checking every 2 seconds for the receipt
        yield return transactionReceiptPolling.PollForReceipt(TransactionHash, 2);

        TCGWallet.Instance.LoadCurrentWalletBalance();
        onSuccessSendTransaction?.Invoke(true, receivingAddress, InputAmount.text);
        Debug.Log("Transaction mined");

        var balanceRequest = new EthGetBalanceUnityRequest(Url);
        yield return balanceRequest.SendRequest(receivingAddress, BlockParameter.CreateLatest());

        BalanceAddressTo = UnitConversion.Convert.FromWei(balanceRequest.Result.Value);
        Debug.Log("Balance of receiver account:" + BalanceAddressTo);
    }



    // Update is called once per frame
    void Update () {
		
	}



   

   
}
