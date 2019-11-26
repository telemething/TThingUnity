/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataStream : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace MotionLib
{
    public class DataStreamPacket
    {
        public delegate void GotDataPacketDelegate(DataStreamPacket dsp);
        public delegate void ConnectedDelegate(string clientAddr);

        public static int MaxLength = 220;

        public byte[] Bytes = new byte[MaxLength];
        public long MilliSeconds;
        public int Length;

        public DataStreamPacket()
        {
        }

        //*********************************************************************
        /// 
        ///  <summary>
        ///  
        ///  </summary>
        /// <param name="bytes"></param>
        ///  
        //*********************************************************************
        public DataStreamPacket(Byte[] bytes, long milliSeconds)
        {
            this.MilliSeconds = milliSeconds;
            Bytes = bytes;
        }

        public DataStreamPacket(Byte[] bytes, int length, bool makeCopy)
        {
            if (makeCopy)
                Buffer.BlockCopy(bytes, 0, Bytes, 0, length);
            else
                Bytes = bytes;
        }
    }

    //*************************************************************************
    //*************************************************************************
    //*************************************************************************

    public interface IDataStream : IDisposable
    {
        //*************************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*************************************************************************

        void Stop();

        //*************************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataPacket"></param>
        /// <param name="milliSeconds"></param>
        /// 
        //*************************************************************************

        void Store(Byte[] dataPacket, long milliSeconds);

        //*************************************************************************
        /// 
        ///  <summary>
        ///  
        ///  </summary>
        ///  <param name="dsp"></param>
        ///  <returns></returns>
        ///  
        /// 
        ///  <summary>
        ///  
        ///  </summary>
        ///  <param name="dsp"></param>
        ///  <returns></returns>
        ///  
        ///  
        ///   <summary>
        ///   
        ///   </summary>
        ///   <param name="callback"></param>
        /// <param name="connectedDelegate"></param>
        /// <param name="skipCount"></param>
        ///   
        //*************************************************************************
        void StartStreamingAsync(DataStreamPacket.GotDataPacketDelegate callback,
            DataStreamPacket.ConnectedDelegate connectedDelegate, int skipCount);
    }
}




