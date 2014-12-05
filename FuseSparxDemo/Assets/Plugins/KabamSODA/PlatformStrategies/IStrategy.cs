using UnityEngine;
using System.Collections;

namespace Kabam.Soda.PlatformStrategies {
    public interface IStrategy {
        void Init(string gameObjectName);
        void ShowUI();
        void Configure(string clientId, string mobileKey, string wskeUrl, string unityVersion);
        void Login(string playerId, string playerCertificate, string languageCode, string countryCode);
        void LogRevenue(Kabam.KabamTransactionData data);
        void FulfillReward(string transactionId);
        void SetRewardPayload(string payload);
        string Version { get; }
    }
}
