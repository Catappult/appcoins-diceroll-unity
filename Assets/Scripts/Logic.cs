using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityAppCoinsSDK;

public class Logic : MonoBehaviour, 
                    IAppCoinsBillingStateListener, 
                    IConsumeResponseListener, 
                    IPurchasesUpdatedListener, 
                    ISkuDetailsResponseListener
{
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

    public const string ATTEMPTS_KEY = "Attempts";
    private int _currentAttempts = 0;


    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey(ATTEMPTS_KEY))
            _currentAttempts = PlayerPrefs.GetInt(ATTEMPTS_KEY, 0);
        else
            _currentAttempts = _startingAttempts;

        UpdateAttemptsUI();

        _btnRoll.onClick.AddListener(OnRollDicePressed);
        _btnBuySDK.onClick.AddListener(OnBuySDKPressed);
        _btnSubsSDK.onClick.AddListener(OnSubsSDKPressed);

        AptoideBillingSDKManager.InitializePlugin(this, this, this, this, "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAzIR0OxCJDzaF2PvcymkPvG9PQTCVkGPxG5eLt5ZcIBftWKl6nFmgItAyYm2ixOrpNUOHjtuTOXuaMMABV91Y6CitQujsr0O76PsHduY0jG2j32wJAIluzspkzKS6sBp4MZvfG/ctUaqjDibYuvRZtE3Wv7kY7zH/lwKmD+BnGScFc8YTJUOlcRdqXtIPbX9Je2h5PtLUNmiLzcnjKxJ7dwsSc/QEuVXSY7k/jFkjIsv62EaLEcMtJrbuL+jvLg6/MpK2REuinLrkG9xK2JjgK9xhW6D7pEvQb/Dj3YFk0RbaP7EITsnrQaqZ1pL9aAEDzeG3qcsJSU2cn/wfGgZodwIDAQAB", this.gameObject.name);
        AptoideBillingSDKManager.QuerySkuDetailsAsync(new string[] { "attempts" }, "inapp");
        AptoideBillingSDKManager.QuerySkuDetailsAsync(new string[] { "golden_dice" }, "subs");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnRollDicePressed()
    {
        if (_currentAttempts > 0)
        {
            _currentAttempts--;
            UpdateAttemptsUI();

            int diceValue = Random.Range(1, 7); // Generate a random number between 1 and 6
            _dice.SetValue(diceValue); // Assuming _dice.SetValue updates the dice face

            // Check if the input number matches the dice value
            if (int.TryParse(_numberInput.text, out int inputNumber) && inputNumber == diceValue)
            {
                _currentAttempts = _startingAttempts; // Reset attempts
                UpdateAttemptsUI();
                ShowToast("Correct");
            }
            else if (_currentAttempts == 0)
            {
                ShowToast("No more attempts, purchase now.");
            }
        }
        else
        {
            ShowToast("No more attempts, purchase now.");
        }
    }

    private void OnBuySDKPressed()
    {
        ShowToast("Buy inapp purchase.");
        AptoideBillingSDKManager.LaunchBillingFlow("attempts", "inapp", "developerPayload");
    }

    private void OnSubsSDKPressed()
    {
        ShowToast("Subscribe SDK button pressed.");
        AptoideBillingSDKManager.LaunchBillingFlow("golden_dice", "subs", "developerPayload");
    }

    private void UpdateAttemptsUI()
    {
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
                    _currentAttempts = _startingAttempts; 
                    AptoideBillingSDKManager.ConsumeAsync(purchase.token);

                    if (purchase.itemType == "subs")
                    {
                        Debug.Log("Subscription purchased.");
                        
                    }
                    else
                    {
                        Debug.Log("Item purchased.");
                        UpdateAttemptsUI();
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








    private void ShowToast(string message)
    {
    #if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            if (currentActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toast = toastClass.CallStatic<AndroidJavaObject>(
                        "makeText", 
                        currentActivity, 
                        message, 
                        toastClass.GetStatic<int>("LENGTH_SHORT")
                    );
                    toast.Call("show");
                }));
            }
        }
    #else
        Debug.Log($"Toast: {message}");
    #endif
    }


    //Implement Listeners

    public void OnBillingSetupFinished(int responseCode)
    {
        if (responseCode == 0) // Assuming 0 indicates success
        {
            Debug.Log("Billing setup finished successfully.");
        }
        else
        {
            Debug.LogError($"Billing setup failed with response code: {responseCode}");
        }
    }

    public void OnConsumeResponse(int responseCode, string purchaseToken)
    {
        if (responseCode == 0) // Assuming 0 indicates success
        {
            Debug.Log($"Purchase with token {purchaseToken} consumed successfully.");
        }
        else
        {
            Debug.LogError($"Failed to consume purchase with token {purchaseToken}. Response code: {responseCode}");
        }
    }

    public void OnPurchasesUpdated(int responseCode, List<Purchase> purchases)
    {
        if (responseCode == 0) // Assuming 0 indicates success
        {
            foreach (var purchase in purchases)
            {
                Debug.Log($"Purchase updated: {purchase.sku}");
                StartCoroutine(ValidatePurchase(purchase));
            }
        }
        else
        {
            Debug.LogError($"Failed to update purchases. Response code: {responseCode}");
        }
    }

    public void OnSkuDetailsResponse(int responseCode, List<SkuDetails> skuDetailsList)
    {
        Debug.Log($"LOGIC SKU Details Response: {responseCode}");
        if (responseCode == 0) // Assuming 0 indicates success
        {
            foreach (var skuDetails in skuDetailsList)
            {
                Debug.Log($"SKU Details received: {skuDetails.sku}");
                if(skuDetails.sku == "attempts")
                {
                    Debug.Log($"Price for attempts: {skuDetails.price}");
                    // Update the UI or perform any action with the SKU details
                    _btnBuySDK.GetComponentInChildren<TMP_Text>().text = "Buy Attempts: " + skuDetails.price; 
                }
                else if(skuDetails.sku == "golden_dice")
                {
                    Debug.Log($"Price for golden dice subscription: {skuDetails.price}");
                    // Update the UI or perform any action with the SKU details
                    _btnSubsSDK.GetComponentInChildren<TMP_Text>().text = "Buy Subs: " + skuDetails.price;
                }
            }
        }
        else
        {
            Debug.LogError($"Failed to receive SKU details. Response code: {responseCode}");
        }
    }

        
    public void OnBillingServiceDisconnected()
    {
        Debug.LogError("Billing service disconnected.");
    }


}
