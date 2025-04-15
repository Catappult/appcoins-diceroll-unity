using System.Collections.Generic;


    public interface ISkuDetailsResponseListener
    {
        void OnSkuDetailsResponse(int responseCode, List<SkuDetails> skuDetailsList);
         // Add this method
    }
