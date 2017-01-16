using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Utilities;
using NAudio;
using System.IO;

public class VOIPTEST : MonoBehaviour {
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

    NAudio.Wave.Compression.AcmStream downsampler;
    NAudio.Wave.Compression.AcmStream bitreduce;
    // Use this for initialization

    int writtenSamples = 0;
    
    
    void Start () {
        //byte[] test = (Mathf.FloatToHalf(0.5f);
        //Debug.Log("Output length " + test.Length);
        //Debug.Log(test[0] + " " + test[1]);
        //Debug.Log(Convert.ToString(test[0], 2));
        //Debug.Log(Convert.ToString(test[1], 2));

        debugAudio = gameObject.GetComponent<AudioSource>();
        //downsampler = new NAudio.Wave.Compression.AcmStream(new NAudio.Wave.WaveFormat(), new NAudio.Wave.WaveFormat(8000, 16, 1));
        //StartTransmitting();
    }

    // Update is called once per frame
    void Update() {
        loudestSampleSoFar += (short)(((500 - Mathf.Abs(loudestSampleSoFar)) * 0.01f) * Mathf.Sign(loudestSampleSoFar));
        if (Time.time - lastSampleTime >= 0.1f && isTransmitting) {
            statsBegin = Time.time;
            
            lastSampleTime = Time.time;
            int newPos = Microphone.GetPosition(DEVICE);

            //Debug.Log("NewPos " + newPos);
            float[] data = new float[Mathf.Abs(newPos - lastSamplePosition)];

            if (newPos <= lastSamplePosition) {
                
                float[] end = new float[lastClip.samples - lastSamplePosition];
                lastClip.GetData(end, lastSamplePosition);
                float[] wraparound = new float[newPos];
                lastClip.GetData(wraparound, 0);
                data = new float[end.Length + wraparound.Length];
                Buffer.BlockCopy(end, 0, data, 0, end.Length);
                Buffer.BlockCopy(wraparound, 0, data, end.Length, wraparound.Length);
            }
            else {
                lastClip.GetData(data, lastSamplePosition);
            }



            //Debug.Log("Polling sample " + (lastSamplePosition + 1) + " of " + lastClip.samples + ", adding " + data.Length + " samples.");

            


            List<byte> expanded = new List<byte>();
            for (int i = 0; i < data.Length; i++) {
                
                expanded.AddRange(Compress16(data[i]));

            }
            byte[] expandedArray = expanded.ToArray();
            
            //Debug.Log(preview);
            //byte[] byteStream;
            
            Buffer.BlockCopy(expandedArray, 0, downsampler.SourceBuffer, 0, expandedArray.Length);
            int srcBytesConverted = 0;
            int convertedByteCount = downsampler.Convert(expandedArray.Length, out srcBytesConverted);
            byte[] converted = new byte[convertedByteCount];
            Buffer.BlockCopy(downsampler.DestBuffer, 0, converted, 0, convertedByteCount);
            /*
            Buffer.BlockCopy(converted, 0, bitreduce.SourceBuffer, 0, converted.Length);
            int srcBytesCrunched = 0;
            int crunchedByteCount = downsampler.Convert(converted.Length, out srcBytesCrunched);
            byte[] crunched = new byte[crunchedByteCount];
            Buffer.BlockCopy(bitreduce.DestBuffer, 0, crunched, 0, crunchedByteCount);
            */
            

            List<byte> compressedData = new List<byte>(); //temporary
            
            
            for (int i = 0; i < converted.Length; i+=2) {
                byte[] bytes = new byte[2];
                bytes[0] = converted[i];
                bytes[1] = converted[i+1];
                short s = BitConverter.ToInt16(bytes, 0);
                
                if (Mathf.Abs(s) > Mathf.Abs(loudestSampleSoFar)) {
                    loudestSampleSoFar = s;

                }
                gain = (float)short.MaxValue / (float)Mathf.Abs(loudestSampleSoFar);
                s *= (short)(gain * multiplier);
                
                compressedData.Add(NAudio.Codecs.MuLawEncoder.LinearToMuLawSample(s));
                //Debug.Log("MuLaw " + NAudio.Codecs.MuLawEncoder.LinearToMuLawSample(s));
            }
            
            lastSamplePosition = newPos;

            byte[] simupacket = new byte[compressedData.Count + 3];
            compressedData.CopyTo(simupacket, 3);
            //Debug.Log("Transmit took " + (Time.time - statsBegin) / 1000 + "ms");
            ReceiveAudio(simupacket);
        }
    }

    public void StartTransmitting() { //trigger this with keystroke, VR input, UI button, etc
        Debug.Log("Voip Test Transmit");
        Debug.Log("Beginning VOIP transmit");
        lastSamplePosition = 0;
        lastSampleTime = 0;
        loudestSampleSoFar = 500;

        int maxFreq = 0;
        int minFreq = 0;
        string deviceName = Microphone.devices[0];
        Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);

        FREQUENCY = minFreq;
        //FREQUENCY = 8000;
        DEVICE = deviceName;
        lastClip = Microphone.Start(deviceName, true, 20, FREQUENCY);
        
        downsampler = new NAudio.Wave.Compression.AcmStream(new NAudio.Wave.WaveFormat(FREQUENCY, 16, 1), new NAudio.Wave.WaveFormat(transmitFreq, 16, 1));
        bitreduce = new NAudio.Wave.Compression.AcmStream(new NAudio.Wave.WaveFormat(transmitFreq, 16, 1), new NAudio.Wave.WaveFormat(transmitFreq, 8, 1));
        Debug.Log("Channels: " + lastClip.channels);
        Debug.Log("Bitconvert is little endian? " + BitConverter.IsLittleEndian);
        isTransmitting = true;
    }

    public void StopTransmitting() {
        isTransmitting = false;
        Microphone.End(DEVICE);
        downsampler.Dispose();
        lastClip = null;
    }

    public void ReceiveAudio(byte[] data) {
        
        //Debug.Log("Voip recieved " + (data.Length - 3) + " samples of audio");
        byte[] trimmedData = new byte[data.Length - 3];

        Buffer.BlockCopy(data, 3, trimmedData, 0, trimmedData.Length); //move received data to trimmed array, excluding first three bytes

        //AudioClip incomingClip;

        
        List<float> decompressedData = new List<float>();
        for (int i = 0; i < trimmedData.Length; i++) {
            short s = NAudio.Codecs.MuLawDecoder.MuLawToLinearSample(trimmedData[i]);
            
            decompressedData.Add(((float)s) / (((float)short.MaxValue)*rangeReduction));
            //Debug.Log(s);
        }
        if(debugAudio.clip == null) {
            debugAudio.clip = AudioClip.Create("clip", 25000, 1, transmitFreq, false);
        }
        //decompressedData.InsertRange(decompressedData.Count, new float[25000 - decompressedData.Count]);
        //Debug.Log(writtenSamples);
        debugAudio.clip.SetData(decompressedData.ToArray(), writtenSamples%25000);
        writtenSamples += decompressedData.Count;






        //AudioClip incomingClip = new AudioClip();
        //incomingClip.SetData(decompressedData, 0);
        if(debugAudio.isPlaying != true) {
            Debug.Log("starting audio");

            debugAudio.Play();
            debugAudio.loop = true;
        }
            
        //Debug.Log(incomingClip.name);
        //debugAudio.Play(0);
        //Debug.Log(debugAudio.name)
    }
    /*
    private float[] DownsampleNaive(float[] inBuffer, int inputSampleRate, int outputSampleRate) {
        List<float> outBuffer = new List<float>();
        double ratio = (double)inputSampleRate / outputSampleRate;
        int sampleGroup = Mathf.RoundToInt((float)ratio);
        int outSample = 0;
        int inSample = 0;
        while (inSample < inBuffer.Length) {
            float[] range = new float[sampleGroup];
            float sum = 0;
            for(int i = 0; i < sampleGroup; i++) {
                sum += inBuffer[i + inSample];
            }
            outBuffer.Add(sum / sampleGroup);
            inSample += sampleGroup;
        }
        return outBuffer.ToArray();
    }
    */
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

    public void OnApplicationExit() {
        downsampler.Dispose();
        bitreduce.Dispose();
    }

    

}
