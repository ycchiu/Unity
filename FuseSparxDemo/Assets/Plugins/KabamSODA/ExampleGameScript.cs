using UnityEngine;
using System.Collections;
using Kabam;

public class GameScript : KabamSODA {

    // Use this for initialization
    IEnumerator Start() {
        SODAInit();

        Debug.Log("Contacting Game Server");
        string server = "http://glass-test-app.herokuapp.com";
        string playerId = "abc";
        WWW www = new WWW(server + "/certificates/" + playerId);
        yield return www;
        string playerCertificate = www.text;
        Debug.Log("Result From Game Server: " + playerCertificate);

        // passing language code and country code as null will have SODA use the system's locale
        string languageCode = null;
        string countryCode = null;
        SODALogin(playerId, playerCertificate, languageCode, countryCode);
    }


}
