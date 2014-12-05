using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Kabam.SimpleJSON;
using Kabam.Soda.PlatformStrategies;

using System.IO;

namespace Kabam {
    public class KabamSODA : MonoBehaviour {

        private IStrategy _strategy;
        private IStrategy Strategy {
            get {
                if (_strategy == null) {
#if UNITY_EDITOR
                    _strategy = new Editor();
#elif UNITY_ANDROID
                    _strategy = new Android();
#elif UNITY_IOS && !SODA_SKIP_IOS
                    _strategy = new Ios();
#else
                    _strategy = new Null();
#endif
                }
                return _strategy;
            }
        }

        public string Version {
            get {
                return this.Strategy.Version ?? "Unknown";
            }
        }

        protected void SODAConfig(string clientId, string mobileKey, string wskeUrl) {
            string unityVersion = Application.unityVersion;
            this.Strategy.Configure(clientId, mobileKey, wskeUrl, unityVersion);
        }

        protected void SODAInit() {
            Debug.Log("Initializing Kabam SODA");
            this.Strategy.Init(this.gameObject.name);
        }

        public void SODAStartGUI() {
            Debug.Log("Calling KabamSODA GUI");
            this.Strategy.ShowUI();
        }

        public void SODALogin(string playerId, string playerCertificate, string languageCode, string countryCode) {
            Debug.Log("Logging Player In To Kabam SODA.  Player Id: " + playerId + ", Player Certificate: " + playerCertificate 
                + ", Locale: " + languageCode + "_" + countryCode);

            this.Strategy.Login(playerId, playerCertificate, languageCode, countryCode);
        }
    
        public void SODALogRevenue(KabamTransactionData data) {
            this.Strategy.LogRevenue(data);
        }

        public void SODAFulfillReward(string transactionId) {
            this.Strategy.FulfillReward(transactionId);
        }

        public void SODASetRewardPayload(string payload) {
            // Sets the "developerPayload" string that is received as part of the SODAOnReward call.
            this.Strategy.SetRewardPayload(payload);
        }

        virtual protected void SODAOnReward(string message) {
            // Callback from SODA when a reward is redeemed
            Debug.Log("Kabam SODA Reward Redeemed: " + message);
        }

        virtual protected void SODAOnVisibilityChange(string message) {
            // Callback from SODA when the icon's visibility changes
            Debug.Log("Kabam SODA Visibility Changed: " + message);
            // The full namespace is used here to remove any possible compiler confusion.
            JSONNode node = Kabam.SimpleJSON.JSON.Parse(message);
            bool visible = node["visible"].AsBool;
            KabamSODABomb[] bombs = GetComponentsInChildren<KabamSODABomb>();
            foreach (KabamSODABomb bomb in bombs) {
                bomb.SODAOnVisibilityChange(visible);
            }
        }

        virtual protected void SODAOnCertificateExpired(string message) {
            // Callback from SODA when the player certificate has expired
            Debug.Log("Kabam SODA Player Certificate Expired: " + message);
        }

    }

}
