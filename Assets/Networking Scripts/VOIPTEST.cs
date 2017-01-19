using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Utilities;
using NAudio;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System.IO;
using UnityEditor.Audio;

public class VOIPTEST : MonoBehaviour {
    public VoipReceiver VoipReceive;


    public float volume;

    //Too high or too low WILL cause issues. ACM stream buffer can easily overflow, only performing one ACM pass.
    float interval = 0.05f;

    //Doesn't seem to slow things down considerably.
    float lowPassQuality = 1;

    //Should we cutoff audio when not talking? This may cause "popping" on the receiving end.
    public bool useSilenceCutoff = true;
    public float silenceCutoff = 0.01f;
    float runningAvgCutoff = 0;
    
    
    //These 
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
    

    //Destination clip length. 
    int receivingBufferLength = 20000;

    //On-object audio source
    AudioSource audioSource;
    int writtenSamples = 0;
    
    
    void Start () {
        //byte[] test = (Mathf.FloatToHalf(0.5f);
        //Debug.Log("Output length " + test.Length);
        //Debug.Log(test[0] + " " + test[1]);
        //Debug.Log(Convert.ToString(test[0], 2));
        //Debug.Log(Convert.ToString(test[1], 2));
        VoipReceive = gameObject.GetComponent<VoipReceiver>();
        audioSource = gameObject.GetComponent<AudioSource>();
        
        //downsampler = new NAudio.Wave.Compression.AcmStream(new NAudio.Wave.WaveFormat(), new NAudio.Wave.WaveFormat(8000, 16, 1));
        //StartTransmitting();
    }

    // Update is called once per frame
    void Update() {
        //PacketLossCompensate();
        
        if (Time.time - lastSampleTime > interval && isTransmitting) {
            
            statsBegin = Time.time;
            lastSampleTime = Time.time;
            int newPos = Microphone.GetPosition(DEVICE);

            if(newPos == 0 && lastSamplePosition == 0) {
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
            for(int i = 0; i < data.Length; i++) {
                filterOutput[i] = lowPassFilter.Transform(data[i]);
                avg += Mathf.Abs(filterOutput[i]);
            }
            avg /= filterOutput.Length;
            if(avg > runningAvgCutoff) {
                runningAvgCutoff += (avg - runningAvgCutoff) * 0.9f;
            }
            else {
                runningAvgCutoff += (avg - runningAvgCutoff) * 0.05f;
            }
            

            //Saves data when not talking. silenceCutoff is user-configurable, as well as useSilenceCutoff
            
            if(runningAvgCutoff < silenceCutoff && useSilenceCutoff) {
                lastSamplePosition = newPos;
                return;
            }
            

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
            for (int i = 0; i < converted.Length; i+=2) {
                byte[] bytes = new byte[2];
                bytes[0] = converted[i];
                bytes[1] = converted[i+1];
                short s = BitConverter.ToInt16(bytes, 0);
                compressedData.Add(NAudio.Codecs.MuLawEncoder.LinearToMuLawSample(s));
            }

            //Constructs simulated packet if performing testing

            byte[] simupacket = new byte[compressedData.Count + 3];
            compressedData.CopyTo(simupacket, 3);

            lastSamplePosition = newPos;

            Debug.Log("Transmit took " + (Time.time - statsBegin) / 1000.0f + "ms");
            if (Input.GetKey(KeyCode.C)) {
                return;
            }
            
            VoipReceive.ReceiveAudio(simupacket);
        }
    }

    public void StartTransmitting() { //trigger this with keystroke, VR input, UI button, etc
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

        SOURCE_FREQUENCY = minFreq;
        //SOURCE_FREQUENCY = 8000;
        DEVICE = deviceName;
        lastClip = Microphone.Start(deviceName, true, 60, SOURCE_FREQUENCY);
        
        
        downsampler = new NAudio.Wave.Compression.AcmStream(new NAudio.Wave.WaveFormat(SOURCE_FREQUENCY, 16, 1), new NAudio.Wave.WaveFormat(TRANSMIT_FREQUENCY, 16, 1));
        bitreduce = new NAudio.Wave.Compression.AcmStream(new NAudio.Wave.WaveFormat(TRANSMIT_FREQUENCY, 16, 1), new NAudio.Wave.WaveFormat(TRANSMIT_FREQUENCY, 8, 1));
        lowPassFilter = NAudio.Dsp.BiQuadFilter.LowPassFilter(SOURCE_FREQUENCY, TRANSMIT_FREQUENCY*0.5f, lowPassQuality);

        

        Debug.Log("Channels: " + lastClip.channels);
        Debug.Log("Bitconvert is little endian? " + BitConverter.IsLittleEndian);
        isTransmitting = true;
        
    }

    
    public void StopTransmitting() {
        isTransmitting = false;
        Microphone.End(DEVICE);
        downsampler.Dispose();
        bitreduce.Dispose();      
        lastClip = null;
    }
    /*
    public void StartReceiving() {
        writtenSamples = 0;
        //audioSource.timeSamples = 0;
        audioSource.clip = null;
    }
    */
    public void ToggleTransmitting() {
        if (isTransmitting) {
            StopTransmitting();
        }
        else {
            StartTransmitting();
        }
    }

    //Making sure the playhead is a suitable distance away from the incoming packets in bufferspace
    //This is useful not only when there is packet loss, but also when there is fluctuating network speed
    public void PacketLossCompensate() { 
        if (audioSource.timeSamples > (writtenSamples % receivingBufferLength) - 100 && Mathf.Abs(audioSource.timeSamples - (writtenSamples % receivingBufferLength)) < receivingBufferLength * 0.8f) {
            
            audioSource.Pause();
        }
        else {
            if (audioSource.isPlaying == false) {
                audioSource.Play();
                audioSource.loop = true;
            }
                
        }
    }

    public void ReceiveAudio(byte[] data) { //called, containing incoming packet
        
        Debug.Log("Voip recieved " + (data.Length - 3) + " samples of audio");
        byte[] trimmedData = new byte[data.Length - 3];

        Buffer.BlockCopy(data, 3, trimmedData, 0, trimmedData.Length); //move received data to trimmed array, excluding first three bytes

        //AudioClip incomingClip;

        
        List<float> decompressedData = new List<float>();
        for (int i = 0; i < trimmedData.Length; i++) {
            short s = NAudio.Codecs.MuLawDecoder.MuLawToLinearSample(trimmedData[i]);

            decompressedData.Add((s / ((float)short.MaxValue)) * volume);
            
            //decompressedData.Add(((float)(trimmedData[i] / byte.MaxValue) * 2.0f) - 1.0f);
            //Debug.Log(s);
        }
        if(audioSource.clip == null) {
            audioSource.clip = AudioClip.Create("clip", receivingBufferLength, 1, TRANSMIT_FREQUENCY, false);
        }
        
        //Debug.Log("incoming data: " + trimmedData.Length + " mulaw data: " + decompressedData.Count);

        //decompressedData.InsertRange(decompressedData.Count, new float[25000 - decompressedData.Count]);
        //Debug.Log(writtenSamples);
        float[] floatData = decompressedData.ToArray();




        

        

        audioSource.clip.SetData(floatData, writtenSamples%receivingBufferLength);


        //Debug.Log("Writing audio at " + writtenSamples % receivingBufferLength + " of " + receivingBufferLength + " samples");
        
        writtenSamples += decompressedData.Count;
        if (writtenSamples > receivingBufferLength * 4) {
            writtenSamples -= receivingBufferLength * 3;
        }
        
    }
    byte[] Compress16(float input) {
        //return BitConverter.GetBytes(input);
        return BitConverter.GetBytes(halve(input));
    }

    short halve(float input) { //signed float input
        short i = (short)(input * (((float)short.MaxValue)));
        return i; //signed short output
    }

    public void OnApplicationExit() {
        Microphone.End(DEVICE);
        downsampler.Dispose();
        bitreduce.Dispose();
    }

    

}