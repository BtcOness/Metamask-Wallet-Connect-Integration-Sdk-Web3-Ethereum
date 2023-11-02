using NBitcoin;
using System.Collections.Generic;
using UnityEngine;
using Nethereum.HdWallet;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Unity.Rpc;
using System.Collections;
using System;
using Nethereum.Web3.Accounts;
using static GlobalData;

public class TCGWallet : MonoBehaviour
{
    static public TCGWallet Instance;

    [HideInInspector]
    public string mnemonicsKey = "MnemonicsKey";
    [HideInInspector]
    public string privateKey = "PrivateKey";
    [HideInInspector]
    public string currentAddressIndexKey = "CurrentAddressIndexKey";

    [SerializeField] private int accountNumLimit = 10;
    public List<string> addressList;

    public event Action<float> onGetBalance;
    public event Action<bool> onSuccessGetBalance;

    public string RPCUrl = "https://ethereum.publicnode.com";
    public string Ethereum_MAINNET_BASE_URL = "https://ethereum.publicnode.com";
    public string Ethereum_TESTNET_BASE_URL = "https://eth-goerli.public.blastapi.io";
    public string Binanace_MAINNET_BASE_URL = "https://bsc-dataseed3.binance.org";
    public string Binanace_TESTNET_BASE_URL = "https://endpoints.omniatech.io/v1/bsc/testnet/public";
    public string CurrencySymbol = "ETH";
    public int ChainID = 1;
    public WalletNetwork walletNetwork;

    Wallet wallet;
    decimal BalanceAddressTo;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        UIManager.Instance.onNetworkChanged += SetNetwork;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool CreateNewWallet()
    {
        Mnemonic mnemo = new Mnemonic(Wordlist.English, WordCount.Twelve);
        wallet = new Wallet(mnemo.WordList, WordCount.Twelve);

        PlayerPrefs.SetString(mnemonicsKey, mnemo.ToString());
        PlayerPrefs.SetInt(currentAddressIndexKey, 0);

        GetWalletAddress();
        LoadCurrentWalletBalance();

        if (mnemo.ToString() != string.Empty)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public string GetCurrentWalletAddress()
    {
        if(addressList.Count > PlayerPrefs.GetInt(currentAddressIndexKey))
            return addressList[PlayerPrefs.GetInt(currentAddressIndexKey)];
        return "";
    }

    public List<string> GetWalletAddress()
    {
        addressList = new List<string>();

        for (int i = 0; i < accountNumLimit; i++)
        {
            var account = wallet.GetAccount(i);
            var addr = account.Address;

            addressList.Add(addr);
        }

        return addressList;
    }

    public void LoadCurrentWalletBalance()
    {
        if (addressList.Count > PlayerPrefs.GetInt(currentAddressIndexKey))
            StartCoroutine(GetWalletBalance(addressList[PlayerPrefs.GetInt(currentAddressIndexKey)]));
        else
            onSuccessGetBalance?.Invoke(false);
    }

    IEnumerator GetWalletBalance(string address)
    {
        var balanceRequest = new EthGetBalanceUnityRequest(RPCUrl);
        yield return balanceRequest.SendRequest(address, BlockParameter.CreateLatest());

        if (balanceRequest.Result != null)
        {
            BalanceAddressTo = UnitConversion.Convert.FromWei(balanceRequest.Result.Value);
            onGetBalance?.Invoke(float.Parse(BalanceAddressTo.ToString()));
            print($"balance of {address} is {BalanceAddressTo}");
        }
        else
        {
            onSuccessGetBalance?.Invoke(false);
        }
    }

    public void InitWalletFromCache()
    {
        wallet = new Wallet(PlayerPrefs.GetString(mnemonicsKey), null);
        GetWalletAddress();
        LoadCurrentWalletBalance();
    }

    public float EtherTokenToFloat(float _token)
    {
        return _token / 100000000f;
    }

    public long EtherFloatToToken(float _amount)
    {
        return Convert.ToInt64(_amount * 100000000);
    }

    public bool RestoreWallet(string _mnemo)
    {
        try
        {
            wallet = new Wallet(_mnemo, null);
            PlayerPrefs.SetString(mnemonicsKey, _mnemo);
            PlayerPrefs.SetInt(currentAddressIndexKey, 0);

            GetWalletAddress();
            LoadCurrentWalletBalance();

            return true;
        }
        catch
        {

        }

        return false;
    }

    public string GetPrivateKey()
    {
        return wallet.GetAccount(PlayerPrefs.GetInt(currentAddressIndexKey)).PrivateKey;
    }

    public Account GetCurrentAccount()
    {
        return wallet.GetAccount(PlayerPrefs.GetInt(currentAddressIndexKey));
    }

    private void SetNetwork(string networkName)
    {
        print($"network changed {networkName}");
        switch (networkName)
        {
            case "Ethereum Mainnet":
                RPCUrl = Ethereum_MAINNET_BASE_URL;
                LoadCurrentWalletBalance();
                CurrencySymbol = "ETH";
                ChainID = 1;
                walletNetwork = WalletNetwork.Ethereum;
                break;
            case "Ethereum Testnet":
                RPCUrl = Ethereum_TESTNET_BASE_URL;
                LoadCurrentWalletBalance();
                CurrencySymbol = "GoerliETH";
                ChainID = 5;
                walletNetwork = WalletNetwork.EthereumTestnet;
                break;
            case "Binance Mainnet":
                RPCUrl = Binanace_MAINNET_BASE_URL;
                LoadCurrentWalletBalance();
                CurrencySymbol = "BNB";
                ChainID = 56;
                walletNetwork = WalletNetwork.Binance;
                break;
            case "Binanace Testnet":
                RPCUrl = Binanace_TESTNET_BASE_URL;
                LoadCurrentWalletBalance();
                CurrencySymbol = "TBNB";
                ChainID = 97;
                walletNetwork = WalletNetwork.BinanceTestnet;
                break;
            default:
                RPCUrl = Ethereum_MAINNET_BASE_URL;
                LoadCurrentWalletBalance();
                CurrencySymbol = "ETH";
                ChainID = 1;
                walletNetwork = WalletNetwork.Ethereum;
                break;
        }
    }
}
