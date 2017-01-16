using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Utilities;
using NAudio;
using System.IO;


public class VoipTransmitter : MonoBehaviour {
    public float rangeReduction;
    public float multiplier;
    public NetworkCoordinator nc;
    AudioSource debugAudio;
    VOIPTEST test;
    VoipReceiver testReceiver;
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
    
    int writtenSamples = 0;
    
    public void Start() {
        test = gameObject.GetComponent<VOIPTEST>();
        testReceiver = gameObject.GetComponent<VoipReceiver>();
        debugAudio = gameObject.GetComponent<AudioSource>();
    }
    // Update is called once per frame
    void Update() {
        loudestSampleSoFar += (short)(((500 - Mathf.Abs(loudestSampleSoFar)) * 0.01f) * Mathf.Sign(loudestSampleSoFar));
        if (Time.time - lastSampleTime >= 0.1f && isTransmitting) {
            statsBegin = Time.time;
            
            lastSampleTime = Time.time;
            int newPos = Microphone.GetPosition(DEVICE);

            Debug.Log("LastSamplePos: " + lastSamplePosition + "NewPos: " + newPos + ", clip samples: " + lastClip.samples);
            Debug.Log("Calculated datalength: " + (newPos - lastSamplePosition));
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

            Debug.Log("Data.Length = " + data.Length);

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


            for (int i = 0; i < converted.Length; i += 2) {
                byte[] bytes = new byte[2];
                bytes[0] = converted[i];
                bytes[1] = converted[i + 1];
                short s = BitConverter.ToInt16(bytes, 0);

                if (Mathf.Abs(s) > Mathf.Abs(loudestSampleSoFar)) {
                    loudestSampleSoFar = s;

                }
                gain = (float)short.MaxValue / (float)Mathf.Abs(loudestSampleSoFar);
                s *= (short)(gain * multiplier);

                compressedData.Add(NAudio.Codecs.MuLawEncoder.LinearToMuLawSample(s));
                //Debug.Log("MuLaw " + NAudio.Codecs.MuLawEncoder.LinearToMuLawSample(s));
            }

            Debug.Log("Setting lastSamplePos to " + newPos);
            lastSamplePosition = newPos;


            //Debug.Log("Transmit took " + (Time.time - statsBegin) / 1000 + "ms");
            if(compressedData.Count > 1900) {
                //return;
            }
            Debug.Log("Sending " + compressedData.Count + " samples");
            byte[] simupacket = new byte[compressedData.Count + 3];
            compressedData.CopyTo(simupacket, 3);
            //Debug.Log("Transmit took " + (Time.time - statsBegin) / 1000 + "ms");
            test.ReceiveAudio(simupacket);
            //nc.RoutePacketToServers(new AudioPacket(compressedData.ToArray()));
            //testReceiver.ReceiveAudio(simupacket);
        }
    }

    public void StartTransmitting() { //trigger this with keystroke, VR input, UI button, etc
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
        StartCoroutine(EnableMic());
    }

    IEnumerator EnableMic() {
        yield return new WaitForSeconds(2);
        isTransmitting = true;
    }

    public void StopTransmitting() {
        isTransmitting = false;
        Microphone.End(DEVICE);
        downsampler.Dispose();
        
        lastClip = null;

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

    public void OnApplicationExit() {
        downsampler.Dispose();
        bitreduce.Dispose();
    }



}
