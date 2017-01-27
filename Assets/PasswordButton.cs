using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PasswordButton : MonoBehaviour {
    public UnityEngine.UI.InputField pwd;
    public UIController controller;

	public void SendPassword() {
        controller.PasswordCallback(pwd.text);
    }
}
