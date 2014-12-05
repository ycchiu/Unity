using UnityEngine;
using System.Collections;

namespace Kabam.Soda.PlatformStrategies {
    public class Editor : IStrategy {
        #region IStrategy implementation
        public string Version { get { return null; } }

        public void Init(string gameObjectName) {
            Debug.Log(string.Format("SODA Init: {0}", gameObjectName));
        }

        public void ShowUI() {
            Debug.Log("SODA ShowUI");
        }

        public void Configure(string clientId, string mobileKey, string wskeUrl, string unityVersion) {
            Debug.Log(string.Format(
                "SODA Configure:\n\tclientId: {0}\n\tmobileKey: {1}\n\twskeUrl: {2}\n\tunityVersion: {3}",
                clientId,
                mobileKey,
                wskeUrl,
                unityVersion));
        }

        public void Login(string playerId, string playerCertificate, string languageCode, string countryCode) {
            Debug.Log(string.Format(
                "SODA Login:\n\tplayerId: {0}\n\tplayerCertificate: {1}\n\tlanguageCode: {2}\n\tcountryCode: {3}",
                playerId,
                playerCertificate,
                languageCode,
                countryCode));
        }

        public void LogRevenue(KabamTransactionData data) {
            Debug.Log(string.Format("SODA LogRevenue: {0}", data));
        }

        public void FulfillReward(string transactionId) {
            Debug.Log(string.Format("SODA FulfillReward: {0}", transactionId));
        }

        public void SetRewardPayload(string payload) {
            Debug.Log(string.Format("SODA SetRewardPayload: {0}", payload));
        }
        #endregion
    }
}