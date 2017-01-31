using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PublishButton : MonoBehaviour {
    public InputField gameName;
    public InputField password;
    public UIController controller;

    public void PublishGame() {
        controller.GameSetupCallback(gameName.text, password.text);
    }
}
