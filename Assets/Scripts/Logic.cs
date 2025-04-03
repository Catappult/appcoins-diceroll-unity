using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityAppCoinsSDK;
using UnityEngine.Networking;

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


        if (PlayerPrefs.HasKey(ATTEMPTS_KEY))
            _currentAttempts = PlayerPrefs.GetInt(ATTEMPTS_KEY, 0);
        else
        {
            _currentAttempts = _startingAttempts;
        }

        if(_currentAttempts == _startingAttempts)
        {
            
            _btnBuySDK.interactable = false;
            TMP_Text textComponentBuy = _btnBuySDK.GetComponentInChildren<TMP_Text>();
            textComponentBuy.color = Color.white;
            _btnSubsSDK.interactable = false;
            TMP_Text textComponentSub = _btnSubsSDK.GetComponentInChildren<TMP_Text>();
            textComponentSub.color = Color.white;
            

        }


        UpdateAttempts(_currentAttempts);


    }

    private string ExtractNumbers(string input)
    {
        // Define a regex pattern to match numbers (including decimal points)
        string pattern = @"\d+(\.\d+)?";
        Match match = Regex.Match(input, pattern);
        return match.Value;
    }

    void Update()
    {
        bool isCabInitialized = _aptoPurchaseManager.isCabInitialized();



        if (_aptoPurchaseManager.serverSideCheck)
        {
            // Get the purchases data
            var purchases = _aptoPurchaseManager.PurchasesAll; 
            if (purchases != null)
            {
                foreach (var purchase in purchases)
                {
                    Debug.Log($"Purchase: {purchase}");

                    // Start the validation coroutine for each purchase
                    StartCoroutine(ValidatePurchase(purchase));   
                }
            }
            else
            {
                Debug.LogWarning("No purchases found.");
            }

            // Validate server-side check
            // Calls consume purchase if OK
            //_aptoPurchaseManager.consumePurchase();
        }

        Debug.LogError("3Golden Dice Active " + _aptoPurchaseManager.IsGoldenDiceSubsActive());
            
        if(_aptoPurchaseManager.IsGoldenDiceSubsActive()){
            isGoldenDice = true;
            setGoldenDice();
        }
        

        
        /**if(_aptoPurchaseManager.ValidateLastPurchase())
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
        }**/


        if(_currentAttempts < _startingAttempts)
        {
            string priceAtt = _aptoPurchaseManager.getAttemptPrice();
            string priceSub = _aptoPurchaseManager.getSubsPrice();
            
            if(priceAtt != null){
                Debug.Log("Teste Call inapp" + priceAtt );
                _btnBuySDK.interactable = true;
                TMP_Text textComponentBuy = _btnBuySDK.GetComponentInChildren<TMP_Text>();
                textComponentBuy.text = "Buy Attempts: " + priceAtt;
            }



            if(priceSub != null){
                if(!isGoldenDice){
                    Debug.Log("Teste Call subs" + priceSub );
                    _btnSubsSDK.interactable = true;
                    TMP_Text textComponentSubs = _btnSubsSDK.GetComponentInChildren<TMP_Text>();
                    textComponentSubs.text = "Buy Subs: " + priceSub;
                }
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

    private IEnumerator ValidatePurchase(Purchase purchase)
    {
        string url = $"https://sdk.diceroll.catappult.io/validate/{purchase.packageName}/{purchase.sku}/{purchase.token}";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Send the request and wait for a response
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Validation failed for purchase: {purchase.sku}. Error: {webRequest.error}");
            }
            else
            {
                // Parse the response
                string responseText = webRequest.downloadHandler.text.Trim();
                Debug.Log($"Validation response for {purchase.sku}: {responseText}");

                // Check if the response is "true" or "false"
                if (responseText == "true")
                {
                    Debug.Log($"Purchase validated successfully for {purchase.sku}. Consuming the purchase...");
                    _aptoPurchaseManager.consumePurchase(purchase.token);

                    if (purchase.itemType == "subs")
                    {
                        Debug.Log("Subscription purchased.");
                        isGoldenDice = true;
                        setGoldenDice();
                    }
                    else
                    {
                        Debug.Log("Item purchased.");
                        UpdateAttempts(_startingAttempts);
                    }
                }
                else if (responseText == "false")
                {
                    Debug.LogError($"Purchase validation failed for {purchase.sku}.");
                }
                else
                {
                    Debug.LogError($"Unexpected response for purchase validation: {responseText}");
                }
            }
        }
    }


}
