using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProviderUIInfo : MonoBehaviour {
    public InputField servername;
    public InputField password;
    public NetworkCoordinator nc;

	public void Publish() {
        string parsedName = servername.text;
        parsedName = parsedName.Replace(":", "$COLON");
        nc.PublishGameInfo(parsedName, password.text);
    }
}
