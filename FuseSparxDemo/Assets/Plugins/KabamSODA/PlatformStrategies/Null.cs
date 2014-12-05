namespace Kabam.Soda.PlatformStrategies {
    public class Null : IStrategy {
        #region IStrategy implementation
        public string Version { get { return null; } }

        public void Init(string gameObjectName) {
        }

        public void ShowUI() {
        }

        public void Configure(string clientId, string mobileKey, string wskeUrl, string unityVersion) {
        }

        public void Login(string playerId, string playerCertificate, string languageCode, string countryCode) {
        }

        public void LogRevenue(KabamTransactionData data) {
        }

        public void FulfillReward(string transactionId) {
        }

        public void SetRewardPayload(string payload) {
        }

        #endregion
    }
}