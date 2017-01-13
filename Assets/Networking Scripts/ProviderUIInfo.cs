using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProviderUIInfo : MonoBehaviour {
    public InputField servername;
    public InputField password;
    public NetworkCoordinator nc;

	public void Publish() {
        nc.PublishGameInfo(servername.text, password.text);
    }
}
