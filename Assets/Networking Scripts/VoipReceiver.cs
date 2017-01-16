using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Utilities;
using NAudio;
using System.IO;
using System.IO;

public class VoipReceiver : MonoBehaviour {
    public float rangeReduction;
    public float multiplier;
    AudioSource debugAudio;
    int FREQUENCY;
    public int transmitFreq;
    string DEVICE;
    AudioClip lastClip;
    float lastSampleTime = 0;
    int lastSamplePosition = 0;
    public bool isTransmitting = false;

    public short loudestSampleSoFar = 500;
    public float gain = 1;

    

    float statsBegin = 0;
    float statsEnd;
    
    int writtenSamples = 0;
    public struct ByteTuple {
        public byte byte1;
        public byte byte2;
    }

    void Start() {
        debugAudio = gameObject.GetComponent<AudioSource>();        
    }

    public void ReceiveAudio(byte[] data) {
        //Debug.Log("Voip recieved " + (data.Length - 3) + " samples of audio");
        byte[] trimmedData = new byte[data.Length - 3];

        Buffer.BlockCopy(data, 3, trimmedData, 0, trimmedData.Length); //move received data to trimmed array, excluding first three bytes

        //AudioClip incomingClip;


        List<float> decompressedData = new List<float>();
        for (int i = 0; i < trimmedData.Length; i++) {
            short s = NAudio.Codecs.MuLawDecoder.MuLawToLinearSample(trimmedData[i]);

            decompressedData.Add(((float)s) / (((float)short.MaxValue) * rangeReduction));
            //Debug.Log(s);
        }
        if (debugAudio.clip == null) {
            debugAudio.clip = AudioClip.Create("clip", 25000, 1, transmitFreq, false);
        }
        //decompressedData.InsertRange(decompressedData.Count, new float[25000 - decompressedData.Count]);
        //Debug.Log(writtenSamples);
        debugAudio.clip.SetData(decompressedData.ToArray(), writtenSamples % 25000);
        writtenSamples += decompressedData.Count;






        //AudioClip incomingClip = new AudioClip();
        //incomingClip.SetData(decompressedData, 0);
        if (debugAudio.isPlaying != true) {
            Debug.Log("starting audio");
            //debugAudio.PlayDelayed(2);
            debugAudio.Play();
            debugAudio.loop = true;
        }

        //Debug.Log(incomingClip.name);
        //debugAudio.Play(0);
        //Debug.Log(debugAudio.name)
    }

    private float[] DownsampleNaive(float[] inBuffer, int inputSampleRate, int outputSampleRate) {
        List<float> outBuffer = new List<float>();
        double ratio = (double)inputSampleRate / outputSampleRate;
        int sampleGroup = Mathf.RoundToInt((float)ratio);
        int outSample = 0;
        int inSample = 0;
        while (inSample < inBuffer.Length) {
            float[] range = new float[sampleGroup];
            float sum = 0;
            for (int i = 0; i < sampleGroup; i++) {
                sum += inBuffer[i + inSample];
            }
            outBuffer.Add(sum / sampleGroup);
            inSample += sampleGroup;
        }
        return outBuffer.ToArray();
    }
    /*
    float Decompress(byte b1, byte b2) {
        byte[] bytes = new byte[2];
        bytes[0] = b1;
        bytes[1] = b2;
        ushort s = BitConverter.ToUInt16(bytes, 0);
        float f = ((((float)s) / (((float)ushort.MaxValue)/rangeReduction)) * 2.0f) - 1.0f;
        return f;
    }
    
    byte Compress(float input) {
        byte compressed = (byte)(((input + 1) / 2) * 256);
        return compressed;
    }
    */
    byte[] Compress16(float input) {
        return BitConverter.GetBytes(halve(input));
    }

    short halve(float input) { //signed float input
        short i = (short)(input * (((float)short.MaxValue) * rangeReduction));
        return i;
    }

    



}
