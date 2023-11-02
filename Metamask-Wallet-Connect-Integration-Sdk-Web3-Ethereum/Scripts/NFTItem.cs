using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;

public class NFTItem : MonoBehaviour
{
    [SerializeField] private RawImage _NFTTexture;
    [SerializeField] private RawImage _NFTTypeTexutre;
    [SerializeField] private TextMeshProUGUI _txtNFTName;
    [SerializeField] private TextMeshProUGUI _txtNFTType;
    [SerializeField] private Texture[] _NFTTypeTextures;

    bool isValueChanged;
    string _NFTUrl, _NFTType, _NFTName;

    // Start is called before the first frame update
    void Start()
    {
        isValueChanged = false;
        _NFTUrl = "";
        _NFTType = "";
        _NFTName = "";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        if (isValueChanged)
        {
            if (_NFTUrl == "" || _NFTType == "" || _NFTName == "") return;
            StartCoroutine(setNFTAsync(_NFTUrl, _NFTType, _NFTName));
            isValueChanged = false;
        }
    }

    public void setNFT(string _url, string _type, string _name)
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(setNFTAsync(_url, _type, _name));
        else
        {
            _NFTUrl = _url;
            _NFTType = _type;
            _NFTName = _name;
            isValueChanged = true;
        }
    }

    IEnumerator setNFTAsync(string _url, string _type, string _name)
    {
        _txtNFTName.text = _name;
        _txtNFTType.text = _type;
        switch (_type.ToLower())
        {
            case "silver":
                _NFTTypeTexutre.texture = _NFTTypeTextures[1];
                break;
            case "gold":
                _NFTTypeTexutre.texture = _NFTTypeTextures[0];
                break;
            default:
                break;

        }
        Texture NFTImage;
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(_url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                // Get downloaded asset bundle
                NFTImage = DownloadHandlerTexture.GetContent(uwr);
                _NFTTexture.texture = NFTImage;
            }
        }
    }
}
