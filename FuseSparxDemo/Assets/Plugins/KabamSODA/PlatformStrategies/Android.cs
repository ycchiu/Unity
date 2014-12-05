#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
using System.Collections;
using Kabam.SimpleJSON;

namespace Kabam.Soda.PlatformStrategies {
    public class Android : IStrategy {
        private static volatile AndroidJavaObject _playerActivity;

        AndroidJavaObject PlayerActivity {
            get {
                if (_playerActivity == null) {
                    using (AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                        _playerActivity = playerClass.GetStatic<AndroidJavaObject>("currentActivity");
                    }
                }
                return _playerActivity;
            }
        }

        private static volatile AndroidJavaClass _apiClass;

        AndroidJavaClass ApiClass {
            get {
                if (_apiClass == null) {
                    _apiClass = new AndroidJavaClass("com.kabam.soda.unity.UnityKabamSession");
                }
                return _apiClass;
            }
        }

        #region IStrategy implementation
        private string version;
        public string Version {
            get {
                if (version == null) {
                    version = "Android " + ApiClass.CallStatic<string>("getVersion", new object[] {});
                }

                return version;
            }
        }

        public void Init(string gameObjectName) {
            ApiClass.CallStatic("init", new object[] {null, null});
            ApiClass.CallStatic("setReceiverObjectName", new object[] { gameObjectName });
        }

        public void ShowUI() {
            if (PlayerActivity != null) {
                using (AndroidJavaClass sodaActivityClass = new AndroidJavaClass("com.kabam.soda.SodaActivity")) {
                    using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", new object[] { PlayerActivity, sodaActivityClass })) {
                        PlayerActivity.Call("startActivity", intent);
                    }
                }
            }
        }

        public void Login(string playerId, string playerCertificate, string languageCode, string countryCode) {
            if (string.IsNullOrEmpty(languageCode) || string.IsNullOrEmpty(countryCode)) {
                using (AndroidJavaClass localeClass = new AndroidJavaClass("java.util.Locale")) {
                    using (AndroidJavaObject locale = localeClass.CallStatic<AndroidJavaObject>("getDefault", new object[] {})) {
                        languageCode = locale.Call<string>("getLanguage", new object[] {});
                        countryCode = locale.Call<string>("getCountry", new object[] {});
                        Debug.Log("Got locale from Android: " + languageCode + "_" + countryCode);
                    }
                }
            }

            if (ApiClass != null) {
                using (AndroidJavaObject locale = new AndroidJavaObject("java.util.Locale", new object[] {languageCode, countryCode})) {
                    ApiClass.CallStatic("login", new object[] {
                        playerId,
                        playerCertificate,
                        locale
                    });
                }
            }
        }

        public void LogRevenue(KabamTransactionData data) {
            JSONNode purchaseData = new JSONClass();
            purchaseData["packageName"] = data.PackageName;
            purchaseData["orderId"] = data.OrderId;
            purchaseData["productId"] = data.ProductId;
            purchaseData["developerPayload"] = data.DeveloperPayload;
            purchaseData["purchaseTime"] = data.PurchaseTime;
            purchaseData["purchaseState"] = data.PurchaseState;
            purchaseData["purchaseToken"] = data.PurchaseToken;
            ApiClass.CallStatic ("createRevenueEvent", new object[] {purchaseData.ToString()});
        }

        public void FulfillReward(string transactionId) {
            ApiClass.CallStatic("fulfillReward", new object[] {transactionId});
        }

        public void SetRewardPayload(string payload) {
            ApiClass.CallStatic("setRewardPayload", new object[] {payload});
        }

        public void Configure(string clientId, string mobileKey, string wskeUrl, string unityVersion) {
            ApiClass.CallStatic("config", new object[] {
                clientId,
                mobileKey,
                wskeUrl,
                unityVersion
            });
        }
        #endregion
    }
}
#endif