using System.Collections;

namespace Kabam {
    public class KabamTransactionData {

#if UNITY_EDITOR
#elif UNITY_IOS && !SODA_SKIP_IOS
        public string Receipt { get; set; }
        public string IP { get; set; }
        public long Price { get; set; }
        public string Currency { get; set; }
        public string Country { get; set; }
        public string TransactionType { get; set; }
        public string Provider { get; set; }
        public string Metadata { get; set; }
        public string TransactionId { get; set; }
#elif UNITY_ANDROID
        public string PackageName { get; set; }
        public string OrderId { get; set; }
        public string ProductId { get; set; }
        public string DeveloperPayload { get; set; }
        public string PurchaseTime { get; set; }
        public string PurchaseState { get; set; }
        public string PurchaseToken { get; set; }

#endif
    }

}
