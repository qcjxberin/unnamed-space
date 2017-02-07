using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PasswordButton : MonoBehaviour {
    public UnityEngine.UI.InputField pwd;
    public UnityEngine.UI.Text label;
    public UIController controller;

	public void SendPassword() {
        controller.PasswordCallback(pwd.text);
        pwd.text = "";
        label.text = "Server is password protected.";
    }
}
