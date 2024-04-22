using UnityEngine;

public class SDKLogic : MonoBehaviour
{
    public string unityClassName = "AptoBridge";
    public string publicKey = "YourPublicKey";
    public string sku = "YourProductSku";
    public string developerPayload = "YourDeveloperPayload";

    private void Start()
    {
        // Initialize AptoBridge via UnitySendMessage
        var initializeData = new AptoBridgeData(unityClassName, publicKey, true);
        string initializeJson = JsonUtility.ToJson(initializeData);
        CallAptoBridgeMethod("Initialize", initializeJson);
    }

    public void MakePurchase()
    {
        // Start the purchase process via UnitySendMessage
        var purchaseData = new PurchaseData(sku, developerPayload);
        string purchaseJson = JsonUtility.ToJson(purchaseData);
        CallAptoBridgeMethod("ProductsStartPay", purchaseJson);
    }

    private void OnMsgFromPlugin(string message)
    {
        // Handle messages received from the plugin
        Debug.Log("Message from plugin: " + message);
    }

    private void CallAptoBridgeMethod(string methodName, string jsonParams)
    {
        using (var aptoBridgeClass = new AndroidJavaClass(unityClassName))
        {
            if (aptoBridgeClass != null)
            {
                // Convert JSON string to JSONObject
                AndroidJavaObject jsonObject = new AndroidJavaObject("org.json.JSONObject", jsonParams);

                // Call the SendUnityMessage method with JSONObject parameter
                aptoBridgeClass.CallStatic("SendUnityMessage", jsonObject);
            }
            else
            {
                Debug.LogError("Failed to find AptoBridge class.");
            }
        }
    }

    [System.Serializable]
    private class AptoBridgeData
    {
        public string unityClassName;
        public string publicKey;
        public bool needLog;

        public AptoBridgeData(string _unityClassName, string _publicKey, bool _needLog)
        {
            unityClassName = _unityClassName;
            publicKey = _publicKey;
            needLog = _needLog;
        }
    }

    [System.Serializable]
    private class PurchaseData
    {
        public string sku;
        public string developerPayload;

        public PurchaseData(string _sku, string _developerPayload)
        {
            sku = _sku;
            developerPayload = _developerPayload;
        }
    }
}