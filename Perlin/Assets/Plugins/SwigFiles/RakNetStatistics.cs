/* ----------------------------------------------------------------------------
 * This file was automatically generated by SWIG (http://www.swig.org).
 * Version 2.0.2
 *
 * Do not make changes to this file unless you know what you are doing--modify
 * the SWIG interface file instead.
 * ----------------------------------------------------------------------------- */

namespace RakNet {

using System;
using System.Runtime.InteropServices;

public class RakNetStatistics : IDisposable {
  private HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal RakNetStatistics(IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new HandleRef(this, cPtr);
  }

  internal static HandleRef getCPtr(RakNetStatistics obj) {
    if (obj != null)
    {
	if (obj.bytesInSendBufferIsCached)
        {
  	     	obj.SetBytesInSendBuffer(obj.bytesInSendBuffer, obj.bytesInSendBuffer.Length);
 	}
  	if (obj.messageInSendBufferIsCached)
        {
		obj.SetMessageInSendBuffer(obj.messageInSendBuffer, obj.messageInSendBuffer.Length);
	}
   	if (obj.runningTotalIsCached)
        {
		obj.SetRunningTotal(obj.runningTotal, obj.runningTotal.Length);
	}
  	if (obj.valueOverLastSecondIsCached)
        {
		obj.SetValueOverLastSecond(obj.valueOverLastSecond, obj.valueOverLastSecond.Length);
	}
	obj.bytesInSendBufferIsCached=false;
	obj.messageInSendBufferIsCached=false;
	obj.runningTotalIsCached=false;
	obj.valueOverLastSecondIsCached=false;
    }
    
    return (obj == null) ? new HandleRef(null, IntPtr.Zero) : obj.swigCPtr;
  }


  ~RakNetStatistics() {
    Dispose();
  }

  public virtual void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          RakNetPINVOKE.delete_RakNetStatistics(swigCPtr);
        }
        swigCPtr = new HandleRef(null, IntPtr.Zero);
      }
      GC.SuppressFinalize(this);
    }
  }


	private bool bytesInSendBufferIsCached  = false;
	private bool messageInSendBufferIsCached  = false;
	private bool runningTotalIsCached  = false;
	private bool valueOverLastSecondIsCached  = false;
	private double[] bytesInSendBufferCache;
	private uint[] messageInSendBufferCache;
	private ulong[] runningTotalCache;
	private ulong[] valueOverLastSecondCache;

  public ulong[] valueOverLastSecond {
	set 
	{
	    	valueOverLastSecondCache=value;
		valueOverLastSecondIsCached = true;
		SetValueOverLastSecond (value, value.Length);    
	}

        get
        {
            ulong[] returnArray;
            if (!valueOverLastSecondIsCached)
            {
                IntPtr cPtr = RakNetPINVOKE.RakNetStatistics_valueOverLastSecond_get (swigCPtr);
                int len = (int) RNSPerSecondMetrics.RNS_PER_SECOND_METRICS_COUNT;
		if (len<=0)
		{
			return null;
		}
                returnArray = new ulong[len];
                long[] marshalArray = new long[len];
                Marshal.Copy(cPtr, marshalArray, 0, len);
                for (int i=0;i<len;i++)
                {
                    returnArray[i]= (ulong) ( marshalArray[i] );
                }
                valueOverLastSecondCache = returnArray;
                valueOverLastSecondIsCached = true;
            }
            else
            {
                returnArray = valueOverLastSecondCache;
            }
            return returnArray;
        }
 
  }

  public ulong[] runningTotal {
	set 
	{
	    	runningTotalCache=value;
		runningTotalIsCached = true;
		SetRunningTotal (value, value.Length);    
	}

        get
        {
            ulong[] returnArray;
            if (!runningTotalIsCached)
            {
                IntPtr cPtr = RakNetPINVOKE.RakNetStatistics_runningTotal_get (swigCPtr);
                int len = (int) RNSPerSecondMetrics.RNS_PER_SECOND_METRICS_COUNT;
		if (len<=0)
		{
			return null;
		}
                returnArray = new ulong[len];
                long[] marshalArray = new long[len];
                Marshal.Copy(cPtr, marshalArray, 0, len);
                for (int i=0;i<len;i++)
                {
                    returnArray[i]= (ulong) ( marshalArray[i] );
                }
                runningTotalCache = returnArray;
                runningTotalIsCached = true;
            }
            else
            {
                returnArray = runningTotalCache;
            }
            return returnArray;
        }
 
  }

  public ulong connectionStartTime {
    set {
      RakNetPINVOKE.RakNetStatistics_connectionStartTime_set(swigCPtr, value);
    } 
    get {
      ulong ret = RakNetPINVOKE.RakNetStatistics_connectionStartTime_get(swigCPtr);
      return ret;
    } 
  }

  public bool isLimitedByCongestionControl {
    set {
      RakNetPINVOKE.RakNetStatistics_isLimitedByCongestionControl_set(swigCPtr, value);
    } 
    get {
      bool ret = RakNetPINVOKE.RakNetStatistics_isLimitedByCongestionControl_get(swigCPtr);
      return ret;
    } 
  }

  public ulong BPSLimitByCongestionControl {
    set {
      RakNetPINVOKE.RakNetStatistics_BPSLimitByCongestionControl_set(swigCPtr, value);
    } 
    get {
      ulong ret = RakNetPINVOKE.RakNetStatistics_BPSLimitByCongestionControl_get(swigCPtr);
      return ret;
    } 
  }

  public bool isLimitedByOutgoingBandwidthLimit {
    set {
      RakNetPINVOKE.RakNetStatistics_isLimitedByOutgoingBandwidthLimit_set(swigCPtr, value);
    } 
    get {
      bool ret = RakNetPINVOKE.RakNetStatistics_isLimitedByOutgoingBandwidthLimit_get(swigCPtr);
      return ret;
    } 
  }

  public ulong BPSLimitByOutgoingBandwidthLimit {
    set {
      RakNetPINVOKE.RakNetStatistics_BPSLimitByOutgoingBandwidthLimit_set(swigCPtr, value);
    } 
    get {
      ulong ret = RakNetPINVOKE.RakNetStatistics_BPSLimitByOutgoingBandwidthLimit_get(swigCPtr);
      return ret;
    } 
  }

  public uint[] messageInSendBuffer {
	set 
	{
	    	messageInSendBufferCache=value;
		messageInSendBufferIsCached = true;
		SetMessageInSendBuffer (value, value.Length);    
	}

        get
        {
            uint[] returnArray;
            if (!messageInSendBufferIsCached)
            {
                IntPtr cPtr = RakNetPINVOKE.RakNetStatistics_messageInSendBuffer_get (swigCPtr);
                int len = (int) PacketPriority.NUMBER_OF_PRIORITIES;
		if (len<=0)
		{
			return null;
		}
                returnArray = new uint[len];
                int[] marshalArray = new int[len];
                Marshal.Copy(cPtr, marshalArray, 0, len);
                for (int i=0;i<len;i++)
                {
                    returnArray[i]= (uint) ( marshalArray[i] );
                }
                messageInSendBufferCache = returnArray;
                messageInSendBufferIsCached = true;
            }
            else
            {
                returnArray = messageInSendBufferCache;
            }
            return returnArray;
        }
 
  }

  public double[] bytesInSendBuffer {
	set 
	{
	    	bytesInSendBufferCache=value;
		bytesInSendBufferIsCached = true;
		SetBytesInSendBuffer (value, value.Length);    
	}

        get
        {
            double[] returnArray;
            if (!bytesInSendBufferIsCached)
            {
                IntPtr cPtr = RakNetPINVOKE.RakNetStatistics_bytesInSendBuffer_get (swigCPtr);
                int len = (int) PacketPriority.NUMBER_OF_PRIORITIES;
		if (len<=0)
		{
			return null;
		}
                returnArray = new double[len];
                double[] marshalArray = new double[len];
                Marshal.Copy(cPtr, marshalArray, 0, len);
                for (int i=0;i<len;i++)
                {
                    returnArray[i]= (double) ( marshalArray[i] );
                }
                bytesInSendBufferCache = returnArray;
                bytesInSendBufferIsCached = true;
            }
            else
            {
                returnArray = bytesInSendBufferCache;
            }
            return returnArray;
        }
 
  }

  public uint messagesInResendBuffer {
    set {
      RakNetPINVOKE.RakNetStatistics_messagesInResendBuffer_set(swigCPtr, value);
    } 
    get {
      uint ret = RakNetPINVOKE.RakNetStatistics_messagesInResendBuffer_get(swigCPtr);
      return ret;
    } 
  }

  public ulong bytesInResendBuffer {
    set {
      RakNetPINVOKE.RakNetStatistics_bytesInResendBuffer_set(swigCPtr, value);
    } 
    get {
      ulong ret = RakNetPINVOKE.RakNetStatistics_bytesInResendBuffer_get(swigCPtr);
      return ret;
    } 
  }

  public float packetlossLastSecond {
    set {
      RakNetPINVOKE.RakNetStatistics_packetlossLastSecond_set(swigCPtr, value);
    } 
    get {
      float ret = RakNetPINVOKE.RakNetStatistics_packetlossLastSecond_get(swigCPtr);
      return ret;
    } 
  }

  public float packetlossTotal {
    set {
      RakNetPINVOKE.RakNetStatistics_packetlossTotal_set(swigCPtr, value);
    } 
    get {
      float ret = RakNetPINVOKE.RakNetStatistics_packetlossTotal_get(swigCPtr);
      return ret;
    } 
  }

  public void SetBytesInSendBuffer(double[] inDoubleArray, int numDoubles) {
    RakNetPINVOKE.RakNetStatistics_SetBytesInSendBuffer(swigCPtr, inDoubleArray, numDoubles);
  }

  public void SetMessageInSendBuffer(uint[] inUnsignedIntArray, int numInts) {
    RakNetPINVOKE.RakNetStatistics_SetMessageInSendBuffer(swigCPtr, inUnsignedIntArray, numInts);
  }

  public void SetRunningTotal(ulong[] inUint64Array, int numUint64) {
    RakNetPINVOKE.RakNetStatistics_SetRunningTotal(swigCPtr, inUint64Array, numUint64);
  }

  public void SetValueOverLastSecond(ulong[] inUint64Array, int numUint64) {
    RakNetPINVOKE.RakNetStatistics_SetValueOverLastSecond(swigCPtr, inUint64Array, numUint64);
  }

  public RakNetStatistics() : this(RakNetPINVOKE.new_RakNetStatistics(), true) {
  }

}

}
