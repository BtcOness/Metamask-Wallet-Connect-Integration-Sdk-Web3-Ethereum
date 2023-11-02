using UnityEngine;
#if UNITY_WEBGL
  using Nethereum.Unity.Metamask;
#endif
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using System;
using TMPro;
using PaperPlaneTools;
using static GlobalData;

public class MultiplatformTransferTaskWeb3 : MonoBehaviour
{
    public static event Action<bool, string, string> onSuccessSendTransaction;

    public TMP_InputField InputAddressTo;
    public TMP_InputField InputAmount;

    string Url = "http://localhost:8545";
    BigInteger ChainId = 444444444500;
    string PrivateKey = "0xb5b1870957d373ef0eeffecc6e4812c0fd08f554b37b233526acc331bf1544f7";
    string AddressTo = "0x8a922380ae0d287116aaf2aece6743da588af349";
    private string _selectedAccountAddress;
    decimal Amount = 0.1m;
    decimal BalanceAddressTo = 0m;

    void Start()
    {

        if (IsWebGL())
        {
#if UNITY_WEBGL
            metamaskHost = MetamaskWebglHostProvider.CreateOrGetCurrentInstance();
            metamaskHost.SelectedAccountChanged += MetamaskHost_SelectedAccountChanged;
#endif
        }

        InputAddressTo.text = AddressTo;
        InputAmount.text = Amount.ToString();

    }

    public bool IsWebGL()
    {
#if UNITY_WEBGL
      return true;
#else
      return false;
#endif
    }

    public async void TransferRequest()
    {
        bool isOK = false;
        new Alert("Confirm", $"Are you going to send {InputAmount.text} {TCGWallet.Instance.CurrencySymbol} to {InputAddressTo.text}?")
            .SetPositiveButton("Sign In", () => isOK = true)
            .Show();
        if (!isOK) return;
        //await TransferEtherUsingWeb3inWebGlOrNative();
        await TransferToken();
    }

    public async Task TransferToken()
    {
        WalletNetwork walletNetwork = TCGWallet.Instance.walletNetwork;
        string senderAddress = TCGWallet.Instance.GetCurrentWalletAddress();
        string newAddress = "0x8A922380aE0D287116AaF2aecE6743DA588AF349";
        var amountToSend = 10;
        var web3 = await GetWeb3Async();

        if (walletNetwork == WalletNetwork.Ethereum)
        {
            var bscContract = web3.Eth.GetContract(bscTCG2ABI, bscTCG2ContractAddress);
            var bscTransferFunction = bscContract.GetFunction("transfer");
            var gas = await bscTransferFunction.EstimateGasAsync(senderAddress, null, null, newAddress, amountToSend);
            try
            {
                var receiptAmountSend = await bscTransferFunction.SendTransactionAndWaitForReceiptAsync(senderAddress, gas,
                    null, null, newAddress, amountToSend);
            }catch(Exception e)
            {
                onSuccessSendTransaction?.Invoke(false, e.Message, "");
            }
            TCGWallet.Instance.LoadCurrentWalletBalance();
        }
        else if (walletNetwork == WalletNetwork.Binance) {
            var ethContract = web3.Eth.GetContract(ethTCG2ABI, ethTCG2ContractAddress);
            var ethTransferFunction = ethContract.GetFunction("transfer");
            var gas = await ethTransferFunction.EstimateGasAsync(senderAddress, null, null, newAddress, amountToSend);
            try
            {
                var receiptAmountSend = await ethTransferFunction.SendTransactionAndWaitForReceiptAsync(senderAddress, gas,
                    null, null, newAddress, amountToSend);
            }
            catch (Exception e)
            {
                onSuccessSendTransaction?.Invoke(false, e.Message, "");
            }
            TCGWallet.Instance.LoadCurrentWalletBalance();
        }
    }

    public async Task TransferEtherUsingWeb3inWebGlOrNative()
    {
        var web3 = await GetWeb3Async();
        string currencySymbol = TCGWallet.Instance.CurrencySymbol;
        string selectedAccount = GetSelectedAccount();
        AddressTo = InputAddressTo.text;
        Amount = Decimal.Parse(InputAmount.text);
        var service = web3.Eth.GetEtherTransferService();

        switch (currencySymbol.Substring(currencySymbol.Length-3))
        {
            case "BNB":
                web3.TransactionManager.UseLegacyAsDefault = true;
                print($"[MultiplatformTransferTaskWeb3]: BNB to {AddressTo} {Amount}");
                try
                {
                    await service.TransferEtherAndWaitForReceiptAsync(AddressTo, Amount);
                }catch(Exception e)
                {
                    onSuccessSendTransaction?.Invoke(false, e.Message, "");
                    return;
                }
                break;
            case "ETH":
                web3.TransactionManager.UseLegacyAsDefault = false;
                try
                {
                    var timePreferenceFeeSuggesionStrategy = web3.FeeSuggestion.GetTimePreferenceFeeSuggestionStrategy();
                    var fee = await timePreferenceFeeSuggesionStrategy.SuggestFeeAsync();
                    var nonce = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(selectedAccount);
                    print($"[MultiplatformTransferTaskWeb3]: ETH to {AddressTo} {Amount}");
                    await service.TransferEtherAndWaitForReceiptAsync(AddressTo, Amount, fee.MaxPriorityFeePerGas.Value, fee.MaxFeePerGas.Value,
                        null, nonce.Value);
                }catch(Exception e)
                {
                    onSuccessSendTransaction?.Invoke(false, e.Message, "");
                    return;
                }
                break;
        }

        onSuccessSendTransaction?.Invoke(true, AddressTo, Amount.ToString());
        TCGWallet.Instance.LoadCurrentWalletBalance();
        /*var balance = await web3.Eth.GetBalance.SendRequestAsync(AddressTo);
        BalanceAddressTo = UnitConversion.Convert.FromWei(balance.Value);
        print($"[MultiplatformTransferTaskWeb3]: balance of {AddressTo}: {BalanceAddressTo}");*/
    }

    private Task MetamaskHost_SelectedAccountChanged(string arg)
    {
        _selectedAccountAddress = arg;
        return Task.CompletedTask;
    }

    public void MetamaskConnect()
    {
#if UNITY_WEBGL
        if (IsWebGL())
        {
            if (MetamaskWebglInterop.IsMetamaskAvailable())
            {
                MetamaskWebglInterop.EnableEthereum(gameObject.name, nameof(EthereumEnabled), nameof(DisplayError));
            }
            else
            {
                DisplayError("Metamask is not available, please install it");
            }
        }
#endif

    }

    public void EthereumEnabled(string addressSelected)
    {
#if UNITY_WEBGL
        if (IsWebGL())
        {
            if (!_isMetamaskInitialised)
            {
                MetamaskWebglInterop.EthereumInit(gameObject.name, nameof(NewAccountSelected), nameof(ChainChanged));
                MetamaskWebglInterop.GetChainId(gameObject.name, nameof(ChainChanged), nameof(DisplayError));
                _isMetamaskInitialised = true;
            }
            NewAccountSelected(addressSelected);
        }
#endif
    }

    public void NewAccountSelected(string accountAddress)
    {
        _selectedAccountAddress = accountAddress;
    }

    private string GetSelectedAccount()
    {
        return _selectedAccountAddress;
    }

    private async Task<IWeb3> GetWeb3Async()
    {
#if UNITY_WEBGL
        await metamaskHost.EnableProviderAsync();
        _selectedAccountAddress = metamaskHost.SelectedAccount;
        return await metamaskHost.GetWeb3Async();
#else
        Url = TCGWallet.Instance.RPCUrl;
        PrivateKey = TCGWallet.Instance.GetPrivateKey();
        ChainId = TCGWallet.Instance.ChainID;
        var account = TCGWallet.Instance.GetCurrentAccount();
        _selectedAccountAddress = account.Address;
        return new Web3(account, Url);
#endif
    }

}
