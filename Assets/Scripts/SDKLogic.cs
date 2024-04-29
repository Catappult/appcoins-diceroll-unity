using UnityEngine;
using System.Collections;
using System;
using System.Diagnostics;

public class SDKLogic : MonoBehaviour
{
    public string unityClassName = "AptoBridge";
    public string publicKey = "YourPublicKey";
    public string sku = "YourProductSku";
    public string developerPayload = "YourDeveloperPayload";
    public Logic logic;

    private AndroidJavaClass aptoBridgeClass;

    private void Start()
    {
        aptoBridgeClass = new AndroidJavaClass("AptoBridge");
        // Initialize AptoBridge
        aptoBridgeClass.CallStatic("Initialize", "SDKLogic", publicKey , true);
    }

    // Method to start the billing flow from AptoBridge
    public void MakePurchase()
    {
        string appcoinsWalletPackage = "com.appcoins.wallet";
        bool appcoinsWalletInstalled = IsPackageInstalled(appcoinsWalletPackage);

        if (!appcoinsWalletInstalled)
        {
            // Prompt the user to install the Appcoins Wallet
            ShowDialogToInstallWallet();
        }
        else
        {
            aptoBridgeClass.CallStatic("ProductsStartPay", sku, developerPayload);
        }
    }

    // Method to receive purchase result from AptoBridge
    public void OnMsgFromPlugin(string purchaseJson)
    {
        PurchaseResult purchaseResult = JsonUtility.FromJson<PurchaseResult>(purchaseJson);

        bool succeed = purchaseResult.succeed;
        int responseCode = purchaseResult.responseCode;

        if(purchaseResult.succeed)
        {
            logic.UpdateAttempts(3);
            aptoBridgeClass.CallStatic("ProductsStartConsume", responseCode);
        }
    }

    private static bool IsPackageInstalled(string packageName)
    {
        try
        {
            AndroidJavaClass packageManagerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject packageManager = packageManagerClass.GetStatic<AndroidJavaObject>("currentActivity").Call<AndroidJavaObject>("getPackageManager");
            packageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, packageManager.GetStatic<int>("GET_ACTIVITIES"));
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    private static void ShowDialogToInstallWallet()
    {
        OpenAptoideStoreApp("com.appcoins.wallet");
    }

    private static void OpenAptoideStoreApp(string packageName)
    {
        try
        {
            // Open the Aptoide store app using its package name
            AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
            AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", "market://details?id=" + packageName);

            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            string action = intentClass.GetStatic<string>("ACTION_VIEW");

            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", action, uri);

            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            currentActivity.Call("startActivity", intent);
        }
        catch (Exception e)
        {
            print("Error opening Aptoide store app: " + e.Message);
        }
    }

    [System.Serializable]
    public class PurchaseResult
    {
        public string msg;
        public bool succeed;
        public int responseCode;
    }
}