/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataStore : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}*/

/*using UnityEngine;
using System.Collections;

public class DataStore : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}*/

/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MotionLib
{
    public class DataStorage : IDataStream, IDisposable
    {
        private DataStreamPacket.GotDataPacketDelegate _gotDataPacketCallback = null;

        private System.IO.FileStream _fileStream = null;
        private System.IO.StreamWriter _streamWriter = null;
        private System.IO.StreamReader _streamReader = null;

        private Queue<DataStreamPacket> _dataQueue = null;
        private Task _task = null;
        private bool _stop = false;
        private Thread _thread = null;
        private int _skipCount = 0;
        private int _recordCount = 0;

        public enum StorageFormatEnum
        {
            Binary,
            Dcb
        };

        public enum StorageModeEnum
        {
            Read,
            Write
        };

        private StorageFormatEnum _storageFormat;

        //*************************************************************************
        /// 
        ///  <summary>
        ///  
        ///  </summary>
        ///  <param name="file"></param>
        ///  <param name="size"></param>
        ///  <param name="storageFormat"></param>
        /// <param name="storageMode"></param>
        ///  
        //*************************************************************************
        public DataStorage(string file, int size,
            StorageFormatEnum storageFormat, StorageModeEnum storageMode)
        {
            _storageFormat = storageFormat;

            try
            {
                switch (_storageFormat)
                {
                    case StorageFormatEnum.Binary:

                        if (storageMode == StorageModeEnum.Write)
                            _fileStream = new System.IO.FileStream(file,
                                System.IO.FileMode.Create, System.IO.FileAccess.Write);
                        else
                            _fileStream = new System.IO.FileStream(file,
                                System.IO.FileMode.Open, System.IO.FileAccess.Read);

                        break;
                    case StorageFormatEnum.Dcb:

                        if (storageMode == StorageModeEnum.Write)
                            _streamWriter = new System.IO.StreamWriter(file);
                        else
                            _streamReader = new System.IO.StreamReader(file);

                        break;
                }

                //fileStream = new System.IO.FileStream(file, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                //fileStream.Write(new byte[] {2}, 0, 1);
                //fileStream.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to open file : " + ex.Message);
            }

            _dataQueue = new Queue<DataStreamPacket>(size);

            _thread = new Thread(QueueToFile);
            _thread.Start();
        }

        //*************************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*************************************************************************

        private void Cleanup()
        {
            if (null != _fileStream)
                try
                {
                    _fileStream.Close();
                    _fileStream = null;
                }
                catch (Exception)
                {
                }

            if (null != _streamWriter)
                try
                {
                    _streamWriter.Close();
                    _streamWriter = null;
                }
                catch (Exception)
                {
                }

            if (null != _streamReader)
                try
                {
                    _streamReader.Close();
                    _streamReader = null;
                }
                catch (Exception)
                {
                }
        }

        //*************************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*************************************************************************

        ~DataStorage()
        {
            Cleanup();
        }

        //*************************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*************************************************************************

        public void Dispose()
        {
            _stop = true;
            Cleanup();
        }

        //*************************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*************************************************************************

        public void Stop()
        {
            _stop = true;
        }

        //*************************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataPacket"></param>
        /// <param name="milliSeconds"></param>
        /// 
        //*************************************************************************

        public void Store(Byte[] dataPacket, long milliSeconds)
        {
            _dataQueue.Enqueue(new DataStreamPacket(dataPacket, milliSeconds));
        }

        string _ReadLine;
        string[] _ReadLineParts;
        private int _LinesReadCount = 0;

        //*************************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dsp"></param>
        /// <returns></returns>
        /// 
        //*************************************************************************

        public DataStreamPacket ReadLine_old(DataStreamPacket dsp)
        {
            if (null == dsp)
                dsp = new DataStreamPacket();

            _ReadLine = _streamReader.ReadLine();

            if (null == _ReadLine)
                return null;

            _LinesReadCount++;

            _ReadLineParts = _ReadLine.Split('-');

            dsp.MilliSeconds = Convert.ToInt32(_ReadLineParts[0]);
            dsp.Bytes = StringToByteArray(_ReadLineParts[1]);

            return dsp;
        }

        //*************************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dsp"></param>
        /// <returns></returns>
        /// 
        //*************************************************************************

        public DataStreamPacket ReadLine(DataStreamPacket dsp)
        {
            if (null == dsp)
                dsp = new DataStreamPacket();

            _ReadLine = _streamReader.ReadLine();

            if (null == _ReadLine)
                return null;

            _LinesReadCount++;

            dsp.MilliSeconds = 0;
            dsp.Bytes = StringToByteArray(_ReadLine);

            return dsp;
        }

        //*************************************************************************
        /// 
        ///  <summary>
        ///  
        ///  </summary>
        ///  <param name="callback"></param>
        /// <param name="connectedDelegate"></param>
        /// <param name="skipCount"></param>
        ///  
        //*************************************************************************
        public void StartStreamingAsync(DataStreamPacket.GotDataPacketDelegate callback,
            DataStreamPacket.ConnectedDelegate connectedDelegate, int skipCount)
        {
            _skipCount = skipCount;
            _gotDataPacketCallback = callback;
            _thread = new Thread(FileToCallback);
            _thread.Start();
        }

        //*************************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ba"></param>
        /// <returns></returns>
        /// 
        //*************************************************************************

        string ByteArrayToString(Byte[] ba)
        {
            var hex = BitConverter.ToString(ba);
            return hex.Replace("-", "");
        }

        //*************************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        /// 
        //*************************************************************************

        public static byte[] StringToByteArray(String hex)
        {
            var numberChars = hex.Length;
            var bytes = new byte[numberChars / 2];

            for (var i = 0; i + 1 < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return bytes;
        }

        //*************************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*************************************************************************

        void QueueToFile()
        {
            while (true)
            {
                //var dqa = dataQueue.Any();
                var dqa = (_dataQueue.Count > 0);

                if (dqa)
                {
                    try
                    {
                        var pkt = _dataQueue.Dequeue();

                        switch (_storageFormat)
                        {
                            case StorageFormatEnum.Binary:
                                _fileStream.Write(pkt.Bytes, 0, pkt.Bytes.Length);
                                break;
                            case StorageFormatEnum.Dcb:
                                _streamWriter.WriteLine(string.Format("{0}-{1}",
                                    pkt.MilliSeconds, ByteArrayToString(pkt.Bytes)));
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error while saving data to file : " + ex.Message);
                    }
                }
                else if (_stop)
                {
                    if (null != _fileStream)
                        try
                        {
                            _fileStream.Close();
                            _fileStream = null;
                        }
                        catch (Exception)
                        {
                        }
                    return;
                }
            }
        }

        //*************************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*************************************************************************

        void FileToCallback()
        {
            var dsp = new DataStreamPacket();
            _recordCount = 0;
            int _skipIndex = 0;

            while (true)
            {
                if (_stop)
                {
                    Cleanup();
                    return;
                }

                dsp = ReadLine(dsp);

                if (null == dsp)
                {
                    Cleanup();
                    return;
                }

                _recordCount++;

                if (_skipIndex++ > _skipCount)
                {
                    _gotDataPacketCallback(dsp);
                    _skipIndex = 0;
                }
            }
        }

    }
}*/



