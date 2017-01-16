using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.Networking;
using Utilities;
public class VOIP : MonoBehaviour {

    public NetworkCoordinator nc;
    AudioSource debugAudio;
    public int FREQUENCY;
    string DEVICE;
    AudioClip lastClip;
    float lastSampleTime = 0;
    int lastSamplePosition = 0;
    public bool isTransmitting = false;
    // Use this for initialization

    List<float[]> queue = new List<float[]>();

    struct byteTuple {
        public byte b1;
        public byte b2;
    }
    
	void Start () {
        debugAudio = gameObject.GetComponent<AudioSource>();
        nc = gameObject.GetComponent<NetworkCoordinator>();
    }
	
	// Update is called once per frame
	void Update () {
		if(Time.time - lastSampleTime > 0.1f && isTransmitting) {
            lastSampleTime = Time.time;
            int newPos = Microphone.GetPosition(DEVICE);
            
            float[] data = new float[Mathf.Abs(newPos - lastSamplePosition)];
            data = ResampleNaive(data, FREQUENCY, 8000);
            byte[] compressedData = new byte[data.Length];
            
            lastClip.GetData(data, lastSamplePosition);
            for(int i = 0; i < data.Length; i++) {
                compressedData[i] = Compress(data[i]);
            }
            lastSamplePosition = newPos;
            Debug.Log("VOIP sending " + compressedData.Length + " samples");
            nc.RoutePacketToServers(new AudioPacket(compressedData)); //VOIP type header is added here (20)
            
        }
	}

    private float[] ResampleNaive(float[] inBuffer, int inputSampleRate, int outputSampleRate) {
        List<float> outBuffer = new List<float>();
        double ratio = (double)inputSampleRate / outputSampleRate;
        int outSample = 0;
        while (true) {
            int inBufferIndex = (int)(outSample++ * ratio);
            if (inBufferIndex < inBuffer.Length)
                outBuffer.Add(inBuffer[inBufferIndex]);
            else
                break;
        }
        return outBuffer.ToArray();
    }

    public void StartTransmitting() { //trigger this with keystroke, VR input, UI button, etc
        Debug.Log("Beginning VOIP transmit");
        isTransmitting = true;
        int maxFreq = 0;
        int minFreq = 0;
        string deviceName = Microphone.devices[0];
        Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);
        
        FREQUENCY = minFreq;
        //FREQUENCY = 8000;
        DEVICE = deviceName;
        lastClip = Microphone.Start(deviceName, false, 100, FREQUENCY);
        Debug.Log("Channels: " + lastClip.channels);
    }

    public void ReceiveAudio(byte[] data) {
        Debug.Log("Voip recieved " + (data.Length -3) + " samples of audio");
        byte[] trimmedData = new byte[data.Length - 3];
        
        Buffer.BlockCopy(data, 3, trimmedData, 0, trimmedData.Length); //move received data to trimmed array, excluding first three bytes

        AudioClip incomingClip;
        
        float[] decompressedData = new float[trimmedData.Length];
        for(int i = 0; i < decompressedData.Length; i++) {
            decompressedData[i] = Decompress(trimmedData[i]);
        }
        //incomingClip = AudioClip.Create("clip", decompressedData.Length, 1, 8000, false);
        incomingClip = new AudioClip();
        incomingClip.SetData(decompressedData, 0);
        debugAudio.PlayOneShot(incomingClip);
        //debugAudio.Play(0);
    }

    float Decompress(byte input) {
        float s = ((((float)input) / 256.0f) * 2) - 1f;
        return s;
    }

    byte Compress(float input) {
        byte compressed = (byte)(((input + 1) / 2) * 256);
        return compressed;
    }
}



