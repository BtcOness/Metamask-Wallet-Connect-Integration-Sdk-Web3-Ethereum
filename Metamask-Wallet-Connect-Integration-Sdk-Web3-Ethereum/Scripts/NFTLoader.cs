using Leguar.TotalJSON;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using static GlobalData;

public class NFTLoader : MonoBehaviour
{
    [SerializeField] private Transform NFTParent;
    [SerializeField] private GameObject NFTPrefab;

    public static event Action<bool, string> onSuccessGetNFTs;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GetNftListByAddress()
    {
        foreach(Transform childTransform in NFTParent)
        {
            Destroy(childTransform.gameObject);
        }

        WalletNetwork networkChain;
        switch (TCGWallet.Instance.CurrencySymbol)
        {
            case "ETH":
                networkChain = WalletNetwork.Ethereum;
                break;
            case "GoerliETH":
                networkChain = WalletNetwork.Ethereum;
                break;
            case "BNB":
                networkChain = WalletNetwork.Binance;
                break;
            case "TBNB":
                networkChain = WalletNetwork.Binance;
                break;
            default:
                networkChain = WalletNetwork.Binance;
                break;
        }
        string userWalletAddress = TCGWallet.Instance.GetCurrentWalletAddress();
        if(userWalletAddress == "") return;
        StartCoroutine(GetNftListByAddressAsync(userWalletAddress, networkChain));
    }

    IEnumerator GetNftListByAddressAsync(string walletAddress, WalletNetwork network)
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
            default:
                chain = "eth";
                break;
        }
        JSON requestObject = new JSON();
        string url = $"https://deep-index.moralis.io/api/v2/{walletAddress}/nft?chain={chain}&format=decimal&media_items=false";
        print($"url: {url}");

        UnityWebRequest uwr = UnityWebRequest.Get(url);
        uwr.SetRequestHeader("accept", "application/json");
        uwr.SetRequestHeader("X-API-Key", moralisAPIKey);
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
            onSuccessGetNFTs?.Invoke(false, "Network Error!");
        }
        else
        {
            Debug.Log("Received: " + uwr.downloadHandler.text);
            string result = uwr.downloadHandler.text;
            JSON json = JSON.ParseString(result);
            JArray jsonArray = json.GetJArray("result");
            foreach(JValue jValue in jsonArray.Values)
            {
                JSON resultJson = JSON.ParseString(jValue.CreateString());
                JSON metadataJson = JSON.ParseString(resultJson.GetString("metadata"));
                string NFTImageLink = metadataJson.GetString("image");
                string NFTName = metadataJson.GetString("name");
                if (NFTName.Length > 10)
                    NFTName = NFTName.Substring(NFTName.IndexOf("]")+2, NFTName.IndexOf(":")-NFTName.IndexOf("]")- 3);
                string NFTType="", NFTSize="";
                JArray attributesArray = metadataJson.GetJArray("attributes");
                foreach(JValue attributeItem in attributesArray.Values)
                {
                    JSON attributeItemJson = JSON.ParseString(attributeItem.CreateString());
                    string attributeType = attributeItemJson.GetString("trait_type");
                    string attributeValue;
                    switch (attributeType)
                    {
                        case "Type":
                            attributeValue = attributeItemJson.GetString("value");
                            NFTType = attributeValue;
                            break;
                        case "Size":
                            attributeValue = attributeItemJson.GetString("value");
                            NFTSize = attributeValue;
                            break;
                    }
                }
                print($"{NFTImageLink}-{NFTName}-{NFTType}-{NFTSize}");
                GameObject NFTObject = Instantiate(NFTPrefab, NFTParent);
                NFTItem _nftItem = NFTObject.GetComponent<NFTItem>();
                _nftItem.setNFT(NFTImageLink, NFTType, NFTName);
            }
        }
    }
}
