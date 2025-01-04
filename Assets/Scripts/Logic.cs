using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Logic : MonoBehaviour
{
    public const string ATTEMPTS_KEY = "Attempts";

    [SerializeField]
    private int _startingAttempts = 3;
    [SerializeField]
    private UIDice _dice;
    [SerializeField]
    private Button _btnRoll;
    [SerializeField]
    private Button _btnBuySDK;
    [SerializeField]
    private Button _btnSubsSDK;
    [SerializeField]
    private TMP_Text _txtAttempts;
    [SerializeField]
    private TMP_InputField _numberInput;
    [SerializeField]
    private TMP_Text _txtResult;
    [SerializeField]
    private AptoPurchaseManager _aptoPurchaseManager;


    private int _currentAttempts = 0;
    private int _answer;
    bool hasLoggedInitialization;
    
    bool  isGoldenDice = false;
    bool hasWallet = false;

    private const string WalletPackageName = "com.appcoins.wallet";

    

    void Awake()
    {   


        UpdateAttempts(_startingAttempts);

        hasLoggedInitialization = false;

        TMP_Text textComponentSubs = _btnSubsSDK.GetComponentInChildren<TMP_Text>();
        if (_btnSubsSDK != null)
        {
            _btnSubsSDK.interactable = false;
            ColorBlock colors = _btnSubsSDK.colors;
            colors.normalColor = new Color(colors.normalColor.r, colors.normalColor.g, colors.normalColor.b, 0.5f); // Set alpha to 50%
            _btnSubsSDK.colors = colors;
            
            if (textComponentSubs != null)
            {
                textComponentSubs.color = Color.white;
            }
        }


        TMP_Text textComponentBuy = _btnBuySDK.GetComponentInChildren<TMP_Text>();
        if (_btnBuySDK != null)
        {
            _btnBuySDK.interactable = false;
            ColorBlock colors = _btnSubsSDK.colors;
            colors.normalColor = new Color(colors.normalColor.r, colors.normalColor.g, colors.normalColor.b, 0.5f); // Set alpha to 50%
            _btnBuySDK.colors = colors;
            
            if (textComponentBuy != null)
            {
                textComponentBuy.color = Color.white;
            }
        }

        if (PlayerPrefs.HasKey(ATTEMPTS_KEY))
            _currentAttempts = PlayerPrefs.GetInt(ATTEMPTS_KEY, 0);
        else
        {
            _currentAttempts = _startingAttempts;
        }

        UpdateAttempts(_currentAttempts);


    }

    void Update()
    {
        

        if(isGoldenDice){
            setGoldenDice();
        }

        
        if(_aptoPurchaseManager.ValidateLastPurchase())
        {
            UpdateAttempts(_startingAttempts);
            Debug.Log("Bought attempts.");
        }

        if(_aptoPurchaseManager.ValidateLastSubsPurchase())
        {
            UpdateAttempts(_startingAttempts);
            // Convert hex color #a87d05 to Color
            setGoldenDice();
            isGoldenDice = true;
        }

        

        //Debug.Log("Checking if billing is initialized...");
        bool isCabInitialized = _aptoPurchaseManager.isCabInitialized();
        //Debug.Log("isCabInitialized: " + isCabInitialized.ToString());

        bool hasWallet = _aptoPurchaseManager.hasWallet();
        if(!hasWallet){
            _btnSubsSDK.gameObject.SetActive(false);
        }
        

        if (isCabInitialized && !hasLoggedInitialization)
        {
        
            hasLoggedInitialization = true;
            _btnSubsSDK.interactable = true;
            _btnBuySDK.interactable = true;
            ColorBlock colors = _btnSubsSDK.colors;
            colors.normalColor = new Color(1f, 0.388f, 0.506f, 1f); // Set color to #FF6381
            _btnSubsSDK.colors = colors;
            _btnBuySDK.colors = colors;

            TMP_Text textComponentSubs = _btnSubsSDK.GetComponentInChildren<TMP_Text>();
            TMP_Text textComponentBuy = _btnBuySDK.GetComponentInChildren<TMP_Text>();

            if (textComponentSubs != null)
            {
                textComponentSubs.color = Color.black;
            }

            if (textComponentBuy != null)
            {
                textComponentBuy.color = Color.black;
            }

        }
        
        
    }

    public void OnRollDicePressed()
    {

        if (_currentAttempts <= 0)
        {
            Debug.LogError("Trying to roll without attempts, bailing...");
            return;
        }

        //Sanity keeping
        _txtResult.gameObject.SetActive(false);

        UpdateAttempts(_currentAttempts - 1);

        int diceValue = Random.Range(1, 7); //Max exclusive
        _dice.SetValue(diceValue);

        VerifyAnswerForDiceValue(diceValue);
    }

    private void setGoldenDice()
    {

        Color diceColor = new Color(168f / 255f, 125f / 255f, 5f / 255f);
        _dice.GetComponent<Image>().color = diceColor;
        Debug.Log("Bought attempts and Got Golden Dice.");


        // Assuming _dice is the parent GameObject
        UIDice parentDice = _dice;

        int i = 1;

        while(i<6){
            // Find the child GameObject by name
            Transform childTransform = parentDice.transform.Find(i.ToString());
            if (childTransform != null)
            { 
                GameObject childGameObject = childTransform.gameObject;
                Debug.Log("IDENTIFIED CHILD GAME OBJECT + " + childGameObject.name);

                

                // Get the Transform component of childGameObject
                Transform childTransformm = childGameObject.transform;

                // Loop through each child
                foreach (Transform child in childTransformm)
                {
                    // Do something with each child
                    Debug.Log("HERE : " + child.name);
                    Color diceColor_ = new Color(168f / 255f, 125f / 255f, 5f / 255f);
                    child.GetComponent<Image>().color = diceColor_;
                }                    
            }
            else
            {
                Debug.LogError("Child GameObject not found.");
            }

            i++;
        }
    

    }

    private void VerifyAnswerForDiceValue(int diceValue)
    {
        StartCoroutine(DisplayResult(_answer == diceValue ? "Correct" : "Incorrect"));
        if(_answer == diceValue){
            UpdateAttempts(_startingAttempts);
        }
    }

    IEnumerator DisplayResult(string result)
    {
        _txtResult.text = result;
        _txtResult.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        _txtResult.gameObject.SetActive(false);
    }

    public void OnTextChanged(string text)
    {
        _answer = int.Parse(_numberInput.text);

        _btnRoll.enabled = _currentAttempts > 0 && _answer > 0;
    }

    public void OnOSPBuyAttempts()
    {
        
    }

    public void OnSDKBuyAttempts()
    {
        
    }

    private void OnBuyAttemptsReturned()
    {
        UpdateAttempts(_currentAttempts + _startingAttempts);
    }

    public void UpdateAttempts(int val)
    {
        _currentAttempts = val;
        PlayerPrefs.SetInt(ATTEMPTS_KEY, _currentAttempts);
        _txtAttempts.text = _currentAttempts.ToString();
    }
}
