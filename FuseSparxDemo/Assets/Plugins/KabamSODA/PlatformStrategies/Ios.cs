#if UNITY_IOS && !SODA_SKIP_IOS && !UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using Kabam.SimpleJSON;

namespace Kabam.Soda.PlatformStrategies {
    public class Ios : IStrategy {
        [DllImport ("__Internal")]
        private static extern void _KabamSODAConfig(string clientId, string mobileKey, string wskeUrl, string unityVersion);

        [DllImport ("__Internal")]
        private static extern void _KabamSODAInit(string receiverObjectName);

        [DllImport ("__Internal")]
        private static extern void _KabamSODALogin(string playerId, string playerCertificate);

        [DllImport ("__Internal")]
        private static extern void _KabamSODALogRevenueEvent(string json);

        [DllImport ("__Internal")]
        private static extern string _KabamSODAVersion();


        #region IStrategy implementation
        private string version;
        public string Version {
            get {
                if (version == null) {
                    version = "iOS " + _KabamSODAVersion();
                }

                return version;
            }
        }

        public void Init(string gameObjectName) {
            _KabamSODAInit(gameObjectName);
        }

        public void ShowUI() {
            // Not currently implemented for iOS
        }

        public void Login(string playerId, string playerCertificate, string languageCode, string countryCode) {
            _KabamSODALogin(playerId, playerCertificate);
        }

        public void LogRevenue(KabamTransactionData data) {
            JSONNode purchaseData = new JSONClass();
            purchaseData ["receipt"] = data.Receipt;
            purchaseData ["ip"] = data.IP;
            purchaseData ["price"] = new JSONData(data.Price);
            purchaseData ["currency"] = data.Currency;
            purchaseData ["country"] = data.Country;
            purchaseData ["transactionType"] = data.TransactionType;
            purchaseData ["provider"] = data.Provider;
            purchaseData ["metadata"] = data.Metadata;
            purchaseData ["transactionId"] = data.TransactionId;
            _KabamSODALogRevenueEvent(purchaseData.ToString ());
        }

        public void FulfillReward(string transactionId) {
            // Not currently implemented for iOS
        }

        public void SetRewardPayload(string payload) {
            // Not currently implemented for iOS
        }

        public void Configure(string clientId, string mobileKey, string wskeUrl, string unityVersion) {
            _KabamSODAConfig(clientId, mobileKey, wskeUrl, unityVersion);
        }
        #endregion


    }
}
#endif