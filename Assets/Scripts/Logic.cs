using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;

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

    private static string[] inappSkus = new string[] { "attempts" };
    private static string[] subsSkus = new string[] { "golden_dice" };

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey(ATTEMPTS_KEY))
        {
            _currentAttempts = PlayerPrefs.GetInt(ATTEMPTS_KEY, 0);
        }
        else
        {
            _currentAttempts = _startingAttempts;
        }

        UpdateAttemptsUI();

        _btnRoll.onClick.AddListener(OnRollDicePressed);
        _btnBuySDK.onClick.AddListener(OnBuySDKPressed);
        _btnSubsSDK.onClick.AddListener(OnSubsSDKPressed);

        AptoideBillingSDKManager.InitializePlugin(
            this,
            this,
            this,
            this,
            "INSERT HERE API KEY",
            this.gameObject.name);
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
        AptoideBillingSDKManager.LaunchBillingFlow("golden_dice", "subs", "developerPayload", "123456789", true);
    }

    private void UpdateAttemptsUI()
    {
        PlayerPrefs.SetInt(ATTEMPTS_KEY, _currentAttempts);
        _txtAttempts.text = _currentAttempts.ToString();
    }

    private IEnumerator ValidatePurchase(Purchase purchase, bool isDebugVersion = false)
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
                if (isDebugVersion || responseText == "true")
                {
                    Debug.Log($"Purchase validated successfully for {purchase.sku}. Consuming the purchase...");
                    _currentAttempts = _startingAttempts;
                    AptoideBillingSDKManager.ConsumeAsync(purchase.token);

                    if (purchase.itemType == "subs")
                    {
                        Debug.Log("Subscription purchased.");
                        setGoldenDice();
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

    private void setGoldenDice()
    {
        Color diceColor = new Color(168f / 255f, 125f / 255f, 5f / 255f);
        _dice.GetComponent<Image>().color = diceColor;

        // Assuming _dice is the parent GameObject
        UIDice parentDice = _dice;

        int i = 1;
        while (i < 6)
        {
            // Find the child GameObject by name
            Transform childTransform = parentDice.transform.Find(i.ToString());
            if (childTransform != null)
            {
                GameObject childGameObject = childTransform.gameObject;

                // Get the Transform component of childGameObject
                Transform childTransformm = childGameObject.transform;

                // Loop through each child
                foreach (Transform child in childTransformm)
                {
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
            // Check if subscriptions are supported
            if (AptoideBillingSDKManager.IsFeatureSupported("SUBSCRIPTIONS") == 0)
            {
                Debug.Log("Subscriptions are supported.");
                AptoideBillingSDKManager.QuerySkuDetailsAsync(subsSkus, "subs");
            }
            else
            {
                Debug.LogWarning("Subscriptions are not supported on this device.");
            }

            // Query purchases for both in-app and subscription products
            PurchasesResult inAppPurchasesResult = AptoideBillingSDKManager.QueryPurchases("inapp");
            HandlePurchasesResult(inAppPurchasesResult);

            PurchasesResult subsPurchasesResult = AptoideBillingSDKManager.QueryPurchases("subs");
            HandlePurchasesResult(subsPurchasesResult);

            AptoideBillingSDKManager.QuerySkuDetailsAsync(inappSkus, "inapp");
            AptoideBillingSDKManager.QuerySkuDetailsAsync(subsSkus, "subs");
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

    public void OnPurchasesUpdated(int responseCode, Purchase[] purchases)
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
            ShowToast("Failed to update purchases.");
        }
    }

    public void OnSkuDetailsResponse(int responseCode, SkuDetails[] skuDetailsList)
    {
        if (responseCode == 0) // Assuming 0 indicates success
        {
            foreach (var skuDetails in skuDetailsList)
            {
                Debug.Log($"SKU Details received: {skuDetails.sku}");
                if (skuDetails.sku == "attempts")
                {
                    Debug.Log($"Price for attempts: {skuDetails.price}");
                    // Update the UI or perform any action with the SKU details
                    _btnBuySDK.GetComponentInChildren<TMP_Text>().text = "Buy Attempts: " + skuDetails.price;
                }
                else if (skuDetails.sku == "golden_dice")
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
        // Disable the buttons if billing setup fails
        _btnBuySDK.interactable = false;
        _btnSubsSDK.interactable = false;
    }

    // Add this method to handle the PurchasesResult
    private void HandlePurchasesResult(PurchasesResult purchasesResult)
    {
        if (purchasesResult.responseCode == 0) // Assuming 0 indicates success
        {
            foreach (var purchase in purchasesResult.purchases)
            {
                Debug.Log($"Purchase found: {purchase.sku}");
                StartCoroutine(ValidatePurchase(purchase));
            }
        }
        else
        {
            Debug.LogError($"Failed to query purchases. Response code: {purchasesResult.responseCode}");
        }
    }

}
