using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Utilities;

public class VoipReceiver : MonoBehaviour, IReceivesPacket<MeshPacket>, INetworked<MeshNetworkIdentity> {

    /*
        VoipReceiver.cs
        Copyright 2017 Finn Sinclair

        Uses NAudio.Codecs.MuLawDecoder

        VoipReceiver is a standard networked behavior in the networked object model
        that can receive MeshPackets with the type PacketType.VOIP. It will decode
        the compressed data using Mu-Law decompression, and output it to the correct
        in-game audio emitter.

        It can compensate for packet loss (or interruptions in streaming data), 
        intelligently resuming audio playback when new packets become available.

        TRANSMIT_FREQUENCY must match the sender's TRANSMIT_FREQUENCY. Currently, this is
        not user-modfiable. However, in the future, the VOIP packet may contain transmission
        frequency information, allowing for on-the-fly and user-modifiable frequency settings.

        This is a member of the standardized distributed network object model,
        fully implementing IReceivesPacket<MeshPacket>. Thus, it must be associated
        with a suitable MeshNetworkIdentity in order to be addressable on the mesh
        network.


    */

    public MeshNetworkIdentity thisObjectIdentity;

    public float volume;

    int TRANSMIT_FREQUENCY = 12000;

    float bufferSeparation = 100;

    //Destination clip length. 
    int receivingBufferLength = 30000;

    //On-object audio source
    AudioSource audioSource;
    int writtenSamples = 0;

    public void SetIdentity(MeshNetworkIdentity i) {
        thisObjectIdentity = i;
    }
    public MeshNetworkIdentity GetIdentity() {
        return thisObjectIdentity;
    }

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
        //Dynamically adjust the padding between the playhead and the new data
        //Reduces popping and and jumping when the datastream is resumed
        bufferSeparation +=(100 - bufferSeparation) * 0.1f;

        //If the playhead is too close to the advancing front of audio data
        if (audioSource.timeSamples > (writtenSamples % receivingBufferLength) - bufferSeparation && Mathf.Abs(audioSource.timeSamples - (writtenSamples % receivingBufferLength)) < receivingBufferLength * 0.8f) {
            //Increase the separation margin to guard against popping.
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
        Buffer.BlockCopy(p.GetContents(), 0, data, 0, data.Length);
        
        //Use NAudio MuLawDecoder decompression codec
        List<float> decompressedData = new List<float>();
        for (int i = 0; i < data.Length; i++) {
            short s = NAudio.Codecs.MuLawDecoder.MuLawToLinearSample(data[i]);
            
            decompressedData.Add((s / ((float)short.MaxValue)) * volume);
        }
        //Make sure we have an audio destination
        if (audioSource.clip == null) {
            Debug.Log("Creating Audio Clip");
            audioSource.clip = AudioClip.Create("clip", receivingBufferLength, 1, TRANSMIT_FREQUENCY, false);
        }
        float[] floatData = decompressedData.ToArray();
        //Modulus allows for audio streaming wraparound
        audioSource.clip.SetData(floatData, writtenSamples % receivingBufferLength);
        
        //Prevent writtenSamples from overflowing int.MAXVALUE after a long time.
        writtenSamples += decompressedData.Count;
        if (writtenSamples > receivingBufferLength * 4) {
            writtenSamples -= receivingBufferLength * 3;
        }
    }
    
    byte[] Compress16(float input) {
        return BitConverter.GetBytes(halve(input));
    }

    short halve(float input) { //signed float input
        short i = (short)(input * (((float)short.MaxValue)));
        return i; //signed short output
    }
    



}