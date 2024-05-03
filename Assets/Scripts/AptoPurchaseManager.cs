using UnityEngine;
using System.Threading;
using System.Collections;

// Define classes to deserialize JSON data received from the plugin
[System.Serializable]
public class PurchaseData
{
    public string msg; // Message indicating the type of response
    public bool succeed; // Indicates whether the operation succeeded
    public int responseCode; // Response code from the plugin
    public string purchaseToken; // Token of the purchase (if any)
    public Purchase[] purchases; // Array of purchases (if any)
}

[System.Serializable]
public class Purchase
{
    // Purchase details
    public string developerPayload;
    public bool isAutoRenewing;
    public string itemType;
    public string orderId;
    public string originalJson;
    public string packageName;
    public int purchaseState;
    public long purchaseTime;
    public string sku;
    public string token;
}

public class AptoPurchaseManager : MonoBehaviour
{
    public string publicKey = "YOUR_PUBLIC_KEY"; // Public key for AppCoins billing
    public string sku = "YOUR_SKU_ID"; // SKU ID for the item to be purchased
    public string developerPayload = "YOUR_DEVELOPER_PAYLOAD"; // Developer payload for verification (optional)
    public bool lastPurchaseCheck = false; // Check for purchase

    private AndroidJavaClass aptoBridgeClass; // Reference to the AptoBridge AndroidJavaClass

    void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            // Instantiate the AptoBridge AndroidJavaClass when the script starts
            aptoBridgeClass = new AndroidJavaClass("AptoBridge");

            // Initialize the AptoBridge plugin
            InitializeAptoBridge();
        }
    }

    // Method to query the purchase made upon the closure of the wallet
    private void OnApplicationFocus(bool focusStatus)
    {
        if(focusStatus == true)
        {
            aptoBridgeClass.CallStatic("QueryPurchases");
        }
    }

    // Method to initialize the AptoBridge plugin
    private void InitializeAptoBridge()
    {
        aptoBridgeClass.CallStatic("Initialize", this.gameObject.name, publicKey, true);
    }

    // Method to start the purchase process
    public void StartPurchase()
    {
        aptoBridgeClass.CallStatic("ProductsStartPay", sku, developerPayload);
    }

    // Example variable for other classes to call in order to validate the last purchase.
    public bool ValidateLastPurchase()
    {
        if (lastPurchaseCheck)
        {
            lastPurchaseCheck = false;
            return true;
        }
        else
        {
            return false;
        }
    }

    // Method to handle messages received from the plugin
    public void OnMsgFromPlugin(string message)
    {
        // Deserialize the JSON data into PurchaseData object
        PurchaseData purchaseData = JsonUtility.FromJson<PurchaseData>(message);

        // Switch based on the message type received
        switch (purchaseData.msg)
        {
            case "InitialResult":
                // Handle initialization result
                if (!purchaseData.succeed)
                {
                    Debug.LogError("Failed to initialize billing service.");
                }
                break;

            case "LaunchBillingResult":
                // Handle launch billing flow result
                if (!purchaseData.succeed)
                {
                    Debug.LogError("Failed to launch billing flow.");
                }
                else
                {
                    Debug.LogError("Launched the billing flow.");
                }
                break;

            case "ProductsPayResult":
                // Handle product purchase result
                if (!purchaseData.succeed)
                {
                    Debug.LogError("Failed to make the purchase.");
                }
                else
                {
                    Debug.LogError("Made the purchase sucesfully.");
                }
                break;

            case "ProductsConsumeResult":
                if (!purchaseData.succeed)
                {
                    Debug.LogError("Failed to consume the purchase.");
                }
                else
                {
                    Debug.LogError("Consumed the purchase sucesfully.");
                }
                break;

            case "QueryPurchasesResult":
                // Handle query purchases result
                bool itemPurchased = false;
                // Check if the item has already been purchased
                foreach (Purchase purchase in purchaseData.purchases)
                {
                    if (purchase.sku == sku)
                    {
                        itemPurchased = true;
                        aptoBridgeClass.CallStatic("ProductsStartConsume", purchase.token);
                        lastPurchaseCheck = true;
                        Debug.Log("Item already purchased.");
                        break;
                    }
                }

                if (!itemPurchased)
                {
                    
                }
                break;
        }
    }
}