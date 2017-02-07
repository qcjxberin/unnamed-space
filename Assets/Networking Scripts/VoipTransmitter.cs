using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Utilities;
using NAudio;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System.IO;
public class VoipTransmitter : MonoBehaviour {

    /*
        VoipTransmitter.cs
        Copyright 2017 Finn Sinclair

        VoipTransmitter is one endpoint of the VOIP system. It collects
        audio data from the microphone, uses NAudio to filter, downsample,
        and compress the sound information, and then sends an AudioPacket
        to the ServerManager for broadcast. It is a discrete packet-based
        recording system, collecting segments of audio data on a regular
        time interval. The audio is collected in 32-bit float, and converted
        to 8-bit PCM data. VoipTransmitter is not network-aware, meaning
        that if all the right variables are set, it WILL send packets to
        the ServerManager, whether or not the ServerManager is actually
        connected to anyone or is ready to transmit.

        Credit to NAudio and Mark Heath
        for filtering, downsampling,
        and compression codecs. 
    */




    //Too high or too low WILL cause issues.
    //ACM stream buffer can easily overflow, only performing one ACM pass.
    //Too high of an interval will create packets that are too big for
    //QosType.UnreliableSequenced. Too short will reduce in unreliable packet
    //timing. Anywhere from 0.05 to 0.2 is pretty good. Adjust the transmit
    //frequency if the packets are too big.
    float interval = 0.05f;

    //Doesn't seem to slow things down considerably.
    float lowPassQuality = 1;

    //Should we cutoff audio when not talking? This may cause "popping" on the receiving end.
    public bool useSilenceCutoff = true;
    public float silenceCutoff = 0.01f;
    float runningAvgCutoff = 0;


    //Stick a VoipReceiver here if you are testing locally.
    //VoipTransmitter will create a simulated packet and give
    //it directly to debugReceiver.
    public VoipReceiver debugReceiver;

    int SOURCE_FREQUENCY;
    int TRANSMIT_FREQUENCY = 12000;
    string DEVICE;

    AudioClip lastClip;
    float lastSampleTime = 0;
    int lastSamplePosition = 0;
    public bool isTransmitting = false;




    //DSP pre-downsample filters
    NAudio.Dsp.BiQuadFilter lowPassFilter;
    NAudio.Dsp.BiQuadFilter lowShelfFilter;

    float statsBegin = 0;
    float statsEnd;


    //ACM stream converters
    NAudio.Wave.Compression.AcmStream downsampler;
    NAudio.Wave.Compression.AcmStream bitreduce;

    NAudio.Codecs.G722Codec g722;


    void Start() {
        
        

        //byte[] test = (Mathf.FloatToHalf(0.5f);
        //Debug.Log("Output length " + test.Length);
        //Debug.Log(test[0] + " " + test[1]);
        //Debug.Log(Convert.ToString(test[0], 2));
        //Debug.Log(Convert.ToString(test[1], 2));


        //downsampler = new NAudio.Wave.Compression.AcmStream(new NAudio.Wave.WaveFormat(), new NAudio.Wave.WaveFormat(8000, 16, 1));
        //StartTransmitting();
    }

    // Update is called once per frame
    void Update() {

        if (Time.time - lastSampleTime > interval && isTransmitting) {

            statsBegin = Time.time;
            lastSampleTime = Time.time;
            int newPos = Microphone.GetPosition(DEVICE);

            if (newPos == 0 && lastSamplePosition == 0) {
                Debug.Log("Skipping");
                return;
            }
            if (newPos == lastSamplePosition) {
                Debug.Log("Skipping");
                return;
            }

            float[] data = new float[Mathf.Abs(newPos - lastSamplePosition)]; //holding array for 32-bit float data
            
            //Retrieve appropriate microphone data into float array

            if (newPos <= lastSamplePosition) {
                int wrappedSampleLength = (lastClip.samples - lastSamplePosition) + newPos;
                data = new float[wrappedSampleLength];
                lastClip.GetData(data, lastSamplePosition);
            }
            else {
                lastClip.GetData(data, lastSamplePosition);
            }

            //Low pass filter, excluding frequencies unsuitable for lower transmission sample rate

            float[] filterOutput = new float[data.Length];
            float avg = 0;
            for (int i = 0; i < data.Length; i++) {
                filterOutput[i] = lowPassFilter.Transform(data[i]);
                //filterOutput[i] = data[i];
                avg += Mathf.Abs(filterOutput[i]);
            }
            avg /= filterOutput.Length;
            if (avg > runningAvgCutoff) {
                runningAvgCutoff += (avg - runningAvgCutoff) * 0.9f;
            }
            else {
                runningAvgCutoff += (avg - runningAvgCutoff) * 0.05f;
            }
            

            //Saves data when not talking. silenceCutoff is user-configurable, as well as useSilenceCutoff

            if (runningAvgCutoff < silenceCutoff && useSilenceCutoff) {
                lastSamplePosition = newPos;
                return;
            }
            //Debug.Log(filterOutput[0]);

            //Converts float data into 16 bit PCM

            List<byte> expanded = new List<byte>();
            for (int i = 0; i < filterOutput.Length; i++) {
                expanded.AddRange(Compress16(filterOutput[i]));
            }
            byte[] expandedArray = expanded.ToArray();



            //Downsample using ACM bytestream encoder

            Buffer.BlockCopy(expandedArray, 0, downsampler.SourceBuffer, 0, expandedArray.Length);
            int srcBytesConverted = 0;
            int convertedByteCount = downsampler.Convert(expandedArray.Length, out srcBytesConverted);
            byte[] converted = new byte[convertedByteCount];
            Buffer.BlockCopy(downsampler.DestBuffer, 0, converted, 0, convertedByteCount);


            //Apply mu-law voice codec compression, converts from 16-bit (two byte) to 8-bit (one byte) audio

            List<byte> compressedData = new List<byte>();
            for (int i = 0; i < converted.Length; i += 2) {
                byte[] bytes = new byte[2];
                bytes[0] = converted[i];
                bytes[1] = converted[i + 1];
                short s = BitConverter.ToInt16(bytes, 0);
                compressedData.Add(NAudio.Codecs.MuLawEncoder.LinearToMuLawSample(s));
            }

            lastSamplePosition = newPos;

            //Debug.Log("Transmit took " + (Time.time - statsBegin) / 1000.0f + "ms");
            if (Input.GetKey(KeyCode.C)) {
                return;
            }

            if(debugReceiver != null) {
                Debug.Log("Sending " + compressedData.Count + " bytes");
                debugReceiver.ReceivePacket(new MeshPacket(compressedData.ToArray(), PacketType.VOIP, 0, 0, 0, 0));
            }
            
        }
    }

    // Trigger this with keystroke, VR input, UI button, etc.
    // Don't do this every time the person starts/stops sending data, instead,
    // use this method when the game starts up, or when you want to make the mic
    // available. Audio won't actually go through unless the code in Update()
    // can successfully give the networking system an AudioPacket. Interrupt
    // it before it delivers the packet, and no data will be sent. VoipReceivers
    // on the other side will intelligently interpret this as packet loss and
    // wait for new data.
    public void StartTransmitting() {
        if (isTransmitting)
            return;
        Debug.Log("Voip Test Transmit");
        Debug.Log("Beginning VOIP transmit");
        lastSamplePosition = 0;
        lastSampleTime = 0;


        int maxFreq = 0;
        int minFreq = 0;
        string deviceName = Microphone.devices[0];
        Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);

        //Record at lowest frequency possible.
        //This makes the downsampling process a little less painful.
        SOURCE_FREQUENCY = minFreq;
        DEVICE = deviceName;
        lastClip = Microphone.Start(deviceName, true, 60, SOURCE_FREQUENCY);
        Debug.Log("Mic name: " + deviceName + ", freq: " + SOURCE_FREQUENCY);

        downsampler = new NAudio.Wave.Compression.AcmStream(new NAudio.Wave.WaveFormat(SOURCE_FREQUENCY, 16, 1), new NAudio.Wave.WaveFormat(TRANSMIT_FREQUENCY, 16, 1));
        bitreduce = new NAudio.Wave.Compression.AcmStream(new NAudio.Wave.WaveFormat(TRANSMIT_FREQUENCY, 16, 1), new NAudio.Wave.WaveFormat(TRANSMIT_FREQUENCY, 8, 1));
        lowPassFilter = NAudio.Dsp.BiQuadFilter.LowPassFilter(SOURCE_FREQUENCY, TRANSMIT_FREQUENCY * 0.5f, lowPassQuality);
        

        Debug.Log("Channels: " + lastClip.channels);
        Debug.Log("Bitconvert is little endian? " + BitConverter.IsLittleEndian);
        isTransmitting = true;

    }

    //Disposes NAudio assets, ends recording, etc
    public void StopTransmitting() {
        isTransmitting = false;
        Microphone.End(DEVICE);
        downsampler.Dispose();
        bitreduce.Dispose();
        lastClip = null;
    }

    public void ToggleTransmitting() {
        if (isTransmitting) {
            StopTransmitting();
        }
        else {
            StartTransmitting();
        }
    }
    //Compress float into 16 bits, in two bytes
    //TODO implement dithering to reduce quantization errors
    byte[] Compress16(float input) {
        return BitConverter.GetBytes(halve(input));
    }

    short halve(float input) { //signed float input
        short i = (short)(input * (((float)short.MaxValue)));
        return i; //signed short output
    }

    public void OnApplicationExit() {
        StopTransmitting();
    }
}


 