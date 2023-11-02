using Aptos.Unity.Rest.Model;
using Aptos.Unity.Sample.UI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class UIManager : MonoBehaviour
{
    static public UIManager Instance { get; set; }

    [Header("General")]
    public List<PanelTab> panelTabs;
    [Space]
    [SerializeField] private TMP_Text mainPanelTitle;
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private GameObject notificationPrefab;
    [Space]
    [SerializeField] private PanelTab accountTab;
    [SerializeField] private PanelTab sendTransactionTab;
    [SerializeField] private PanelTab mintNFTTab;
    [SerializeField] private PanelTab nftLoaderTab;
    [SerializeField] private PanelTab addAccountTab;

    [Header("Infos")]
    [SerializeField] private TMP_Dropdown walletListDropDown;
    [SerializeField] private TMP_Dropdown networkDropDown;
    [SerializeField] private TMP_Text balanceText;

    [Header("Add Account")]
    [SerializeField] private TMP_InputField createdMnemonicInputField;
    [SerializeField] private TMP_InputField importMnemonicInputField;

    [Header("Send Transaction")]
    [SerializeField] private TMP_Text senderAddress;
    [SerializeField] private TMP_InputField receiverAddressInput;
    [SerializeField] private TMP_InputField sendAmountInput;
    [SerializeField] private TMP_Text Currency;

    [Header("Mint NFT")]
    [SerializeField] private TMP_InputField c_collectionNameInputField;
    [SerializeField] private TMP_InputField collectionDescriptionInputField;
    [SerializeField] private TMP_InputField collectionUriInputField;
    [Space]
    [SerializeField] private TMP_InputField n_collectionNameInputField;
    [SerializeField] private TMP_InputField tokenNameInputField;
    [SerializeField] private TMP_InputField tokenDescriptionInputField;
    [SerializeField] private TMP_InputField supplyInputField;
    [SerializeField] private TMP_InputField maxInputField;
    [SerializeField] private TMP_InputField tokenURIInputField;
    [SerializeField] private TMP_InputField royaltyPointsInputField;

    [Header("Notification")]
    [SerializeField] private Transform notificationPanel;

    [Header("Event")]
    [SerializeField] private UnityEvent nftLoad;

    public event Action<string> onNetworkChanged;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        InitStatusCheck();

        TCGWallet.Instance.onGetBalance += UpdateBalance;
        TCGWallet.Instance.onSuccessGetBalance += IsSuccessBalance;
        EtherTransferCoroutinesUnityWebRequest.onSuccessSendTransaction += IsSuccessTransaction;
        MultiplatformTransferTaskWeb3.onSuccessSendTransaction += IsSuccessTransaction;
        NFTLoader.onSuccessGetNFTs += IsSuccessNFT;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void InitStatusCheck()
    {
        networkDropDown.onValueChanged.AddListener(delegate
        {
            SetNetwork(networkDropDown);
        });

        SetNetwork(networkDropDown);

        if (PlayerPrefs.GetString(TCGWallet.Instance.mnemonicsKey) != string.Empty)
        {
            TCGWallet.Instance.InitWalletFromCache();
            AddWalletAddressListUI(TCGWallet.Instance.addressList);
            ToggleEmptyState(false);
            ToggleNotification(ResponseInfo.Status.Success, "Successfully Import the Wallet");
        }
        else
        {
            ToggleEmptyState(true);
        }

        walletListDropDown.onValueChanged.AddListener(delegate
        {
            OnWalletListDropdownValueChanged(walletListDropDown);
        });

        nftLoad?.Invoke();
    }

    void UpdateBalance(float _amount)
    {
        print($"[UpdateBalance]: {_amount}");
        balanceText.text = _amount.ToString("0.0000") + " " + TCGWallet.Instance.CurrencySymbol;
    }

    void IsSuccessBalance(bool isSuccess)
    {
        if (!isSuccess)
        {
            ToggleNotification(ResponseInfo.Status.Failed, "Getting balance is failed! Try Again!");
        }
    }

    void IsSuccessTransaction(bool isSuccess, string receAddr, string amount = "")
    {
        if (!isSuccess)
        {
            ToggleNotification(ResponseInfo.Status.Failed, receAddr);
        }
        else
        {
            ToggleNotification(ResponseInfo.Status.Success, $"Sent {amount} {TCGWallet.Instance.CurrencySymbol} to {receAddr}");
        }
    }

    void IsSuccessNFT(bool isSuccess, string msg)
    {
        if (!isSuccess)
        {
            ToggleNotification(ResponseInfo.Status.Failed, msg);
        }
    }

    public void SetNetwork(TMP_Dropdown _target)
    {
        onNetworkChanged?.Invoke(_target.options[_target.value].text);
        Currency.text = TCGWallet.Instance.CurrencySymbol;
        ToggleNotification(ResponseInfo.Status.Success, "Set Network to " + _target.options[_target.value].text);
        nftLoad?.Invoke();
    }

    void OnWalletListDropdownValueChanged(TMP_Dropdown _target)
    {
        PlayerPrefs.SetInt(TCGWallet.Instance.currentAddressIndexKey, _target.value);
        TCGWallet.Instance.LoadCurrentWalletBalance();
        senderAddress.text = TCGWallet.Instance.addressList[_target.value];
        nftLoad?.Invoke();
    }

    public void OnWalletCreateClicked()
    {
        if (TCGWallet.Instance.CreateNewWallet())
        {
            createdMnemonicInputField.text = PlayerPrefs.GetString(TCGWallet.Instance.mnemonicsKey);
            ToggleEmptyState(false);
            ToggleNotification(ResponseInfo.Status.Success, "Successfully Create the Wallet");
        }
        else
        {
            ToggleEmptyState(true);
            ToggleNotification(ResponseInfo.Status.Failed, "Fail to Create the Wallet");
        }

        AddWalletAddressListUI(TCGWallet.Instance.addressList);
    }

    public void ToggleEmptyState(bool _empty)
    {
        accountTab.DeActive(_empty);
        sendTransactionTab.DeActive(_empty);
        mintNFTTab.DeActive(_empty);
        nftLoaderTab.DeActive(_empty);

        if (_empty)
        {
            walletListDropDown.ClearOptions();
            List<string> options = new List<string>();
            options.Add("Please Create Wallet First");
            walletListDropDown.AddOptions(options);
            balanceText.text = "n/a " + TCGWallet.Instance.CurrencySymbol;
            createdMnemonicInputField.text = String.Empty;
            importMnemonicInputField.text = String.Empty;

            OpenTabPanel(addAccountTab);
        }
    }

    public void ToggleNotification(ResponseInfo.Status status, string _message)
    {
        NotificationPanel np = Instantiate(notificationPrefab, notificationPanel).GetComponent<NotificationPanel>();
        np.Toggle(status, _message);
    }

    public void OpenTabPanel(PanelTab _panelTab)
    {
        foreach (PanelTab _childPanelTab in panelTabs)
        {
            if (_childPanelTab.panelGroup == _panelTab.panelGroup)
            {
                _childPanelTab.UnSelected();
            }
        }

        _panelTab.Selected();

        if (_panelTab.panelGroup == PanelGroup.mainPanel)
        {
            mainPanelTitle.text = _panelTab.tabName;
        }
    }

    public void AddWalletAddressListUI(List<string> _addressList)
    {
        walletListDropDown.ClearOptions();
        walletListDropDown.value = 0;

        List<string> addressList = new List<string>();
        foreach (string _s in _addressList)
        {
            //addressList.Add(ShortenString(_s, 4));
            addressList.Add(_s);
        }

        walletListDropDown.AddOptions(addressList);
        if(PlayerPrefs.HasKey(TCGWallet.Instance.currentAddressIndexKey))
            walletListDropDown.value = PlayerPrefs.GetInt(TCGWallet.Instance.currentAddressIndexKey);

        senderAddress.text = TCGWallet.Instance.GetCurrentWalletAddress();
    }

    public void OnImportWalletClicked(TMP_InputField _input)
    {
        if (TCGWallet.Instance.RestoreWallet(_input.text))
        {
            AddWalletAddressListUI(TCGWallet.Instance.addressList);
            ToggleEmptyState(false);
            ToggleNotification(ResponseInfo.Status.Success, "Successfully Import the Wallet");
        }
        else
        {
            ToggleEmptyState(true);
            ToggleNotification(ResponseInfo.Status.Failed, "Fail to Import the Wallet");
        }
    }

    public void Logout()
    {
        PlayerPrefs.DeleteKey(TCGWallet.Instance.mnemonicsKey);

        ToggleEmptyState(true);
    }

    public void CopyMnemonicWords()
    {
        CopyToClipboard(PlayerPrefs.GetString(TCGWallet.Instance.mnemonicsKey));
        ToggleNotification(ResponseInfo.Status.Success, "Successfully Copied MnemonicWords.");
    }

    public void CopyPrivateKey()
    {
        CopyToClipboard(TCGWallet.Instance.GetPrivateKey());
        ToggleNotification(ResponseInfo.Status.Success, "Successfully Copied PrivateKey.");
    }

    public void CopyWalletAddress()
    {
        CopyToClipboard(TCGWallet.Instance.GetCurrentWalletAddress());
        ToggleNotification(ResponseInfo.Status.Success, "Successfully Copied Wallet address.");
    }

    void CopyToClipboard(string _input)
    {
        TextEditor te = new TextEditor();
        te.text = _input;
        te.SelectAll();
        te.Copy();
    }
}
