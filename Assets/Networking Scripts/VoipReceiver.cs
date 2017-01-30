using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Utilities;

public class VoipReceiver : MonoBehaviour, IReceivesPacket<MeshPacket> {

    public float volume;

    int TRANSMIT_FREQUENCY = 12000;

    float bufferSeparation = 100;

    //Destination clip length. 
    int receivingBufferLength = 30000;

    //On-object audio source
    AudioSource audioSource;
    int writtenSamples = 0;


    void Start() {
        audioSource = gameObject.GetComponent<AudioSource>();
        StartReceiving();
    }

    // Update is called once per frame
    void Update() {

        PacketLossCompensate();
    }
    
    public void StartReceiving() {
        writtenSamples = 0;
        //audioSource.timeSamples = 0;
        audioSource.clip = null;
    }

    

    //Making sure the playhead is a suitable distance away from the incoming packets in bufferspace
    //This is useful not only when there is packet loss, but also when there is fluctuating network speed
    public void PacketLossCompensate() {
        bufferSeparation +=(100 - bufferSeparation) * 0.1f;
        if (audioSource.timeSamples > (writtenSamples % receivingBufferLength) - bufferSeparation && Mathf.Abs(audioSource.timeSamples - (writtenSamples % receivingBufferLength)) < receivingBufferLength * 0.8f) {
            bufferSeparation += 200;
            //Debug.Log("Pausing");
            audioSource.Pause();
        }
        else {
            if (audioSource.isPlaying == false) {
                //Debug.Log("Restarting");
                audioSource.Play();
                audioSource.loop = true;
            }

        }
    }

    public void ReceivePacket(MeshPacket p) { //called, containing incoming packet

        if(p.GetPacketType() != PacketType.VOIP) {
            Debug.LogError("PACKET TYPE MISMATCH");
            return;
        }
        byte[] data = new byte[p.GetContents().Length];


        Debug.Log("Voip recieved " + data.Length + " samples of audio");
      


        List<float> decompressedData = new List<float>();
        for (int i = 0; i < data.Length; i++) {
            short s = NAudio.Codecs.MuLawDecoder.MuLawToLinearSample(data[i]);
            decompressedData.Add((s / ((float)short.MaxValue)) * volume);
        }
        //initialize input
        if (audioSource.clip == null) {
            Debug.Log("Creating Audio Clip");
            audioSource.clip = AudioClip.Create("clip", receivingBufferLength, 1, TRANSMIT_FREQUENCY, false);
        }
        float[] floatData = decompressedData.ToArray();
        audioSource.clip.SetData(floatData, writtenSamples % receivingBufferLength);
        
        //Prevent writtenSamples from overflowing int.MAXVALUE after a long time.
        
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
    



}