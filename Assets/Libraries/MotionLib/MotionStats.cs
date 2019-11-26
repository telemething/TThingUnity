#if UNITY_EDITOR
#undef UNITY_WSA_10_0
#endif 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MessageLib;


#if UNITY_WSA_10_0
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
#endif

namespace MessageLib
{
    public interface MessageRecordI
    {
    };

    public delegate void MessageRecordReceivedDelegate(MessageRecordI messageFromHub);

    public struct MessageFromHub
    {
        public string MessageS;
    };

    public delegate void MessageFromHubReceivedDelegate(MessageFromHub messageFromHub);

    public class MessageLib
    {
        public MessageLib()
        {
        }
    }
}

namespace System.IO
{
    public class CircularBuffer<T> : ICollection<T>, IEnumerable<T>, ICollection, IEnumerable
    {
        private int capacity;
        private int size;
        private int head;
        private int tail;
        private T[] buffer;

        //[NonSerialized]
        private object syncRoot;

        public CircularBuffer(int capacity)
            : this(capacity, false)
        {
        }

        public CircularBuffer(int capacity, bool allowOverflow)
        {
            if (capacity < 0)
                throw new ArgumentException("capacity must be greater than or equal to zero.",
                    "capacity");

            this.capacity = capacity;
            size = 0;
            head = 0;
            tail = 0;
            buffer = new T[capacity];
            AllowOverflow = allowOverflow;
        }

        public bool AllowOverflow
        {
            get;
            set;
        }

#if UNITY_WSA_10_0
        public T Tail => buffer[tail];
#endif

        public int Capacity
        {
            get { return capacity; }
            set
            {
                if (value == capacity)
                    return;

                if (value < size)
                    throw new ArgumentOutOfRangeException("value",
                        "value must be greater than or equal to the buffer size.");

                var dst = new T[value];
                if (size > 0)
                    CopyTo(dst);
                buffer = dst;

                capacity = value;
            }
        }

        public int Size
        {
            get { return size; }
        }

        public bool Contains(T item)
        {
            int bufferIndex = head;
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < size; i++, bufferIndex++)
            {
                if (bufferIndex == capacity)
                    bufferIndex = 0;

                if (item == null && buffer[bufferIndex] == null)
                    return true;
                else if ((buffer[bufferIndex] != null) &&
                         comparer.Equals(buffer[bufferIndex], item))
                    return true;
            }

            return false;
        }

        public void Clear()
        {
            size = 0;
            head = 0;
            tail = 0;
        }

        public int Put(T[] src)
        {
            return Put(src, 0, src.Length);
        }

        public int Put(T[] src, int offset, int count)
        {
            int realCount = AllowOverflow ? count : Math.Min(count, capacity - size);
            int srcIndex = offset;
            for (int i = 0; i < realCount; i++, tail++, srcIndex++)
            {
                if (tail == capacity)
                    tail = 0;
                buffer[tail] = src[srcIndex];
            }
            size = Math.Min(size + realCount, capacity);
            return realCount;
        }

        public void Put(T item)
        {
            try
            {
                if (!AllowOverflow && size == capacity)
                    throw new Exception("Buffer is full.");

                if (++tail == capacity)
                    tail = 0;

                buffer[tail] = item;

                if (size < Capacity)
                    size++;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public void Puty(T item)
        {
            if (!AllowOverflow && size == capacity)
                throw new Exception("Buffer is full.");

            buffer[tail] = item;
            if (tail++ == capacity)
                tail = 0;
            size++;
        }

        public void Skip(int count)
        {
            head += count;
            if (head >= capacity)
                head -= capacity;
        }

        public T[] Get(int count)
        {
            var dst = new T[count];
            Get(dst);
            return dst;
        }

        public int Get(T[] dst)
        {
            return Get(dst, 0, dst.Length);
        }

        public int Get(T[] dst, int offset, int count)
        {
            int realCount = Math.Min(count, size);
            int dstIndex = offset;
            for (int i = 0; i < realCount; i++, head++, dstIndex++)
            {
                if (head == capacity)
                    head = 0;
                dst[dstIndex] = buffer[head];
            }
            size -= realCount;
            return realCount;
        }

        public T Get()
        {
            try
            {
                if (size == 0)
                    throw new InvalidOperationException("Buffer is empty.");

                var item = buffer[head];
                if (++head == capacity)
                    head = 0;
                size--;
                return item;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyTo(0, array, arrayIndex, size);
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (count > size)
                throw new ArgumentOutOfRangeException("count",
                    "count cannot be greater than the buffer size.");

            int bufferIndex = head;
            for (int i = 0; i < count; i++, bufferIndex++, arrayIndex++)
            {
                if (bufferIndex == capacity)
                    bufferIndex = 0;
                array[arrayIndex] = buffer[bufferIndex];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            int bufferIndex = head;
            for (int i = 0; i < size; i++, bufferIndex++)
            {
                if (bufferIndex == capacity)
                    bufferIndex = 0;

                yield return buffer[bufferIndex];
            }
        }

        public T[] GetBuffer()
        {
            return buffer;
        }

        public T[] ToArray()
        {
            var dst = new T[size];
            CopyTo(dst);
            return dst;
        }

        #region ICollection<T> Members

        int ICollection<T>.Count
        {
            get { return Size; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        void ICollection<T>.Add(T item)
        {
            Put(item);
        }

        bool ICollection<T>.Remove(T item)
        {
            if (size == 0)
                return false;

            Get();
            return true;
        }

        #endregion

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region ICollection Members

        int ICollection.Count
        {
            get { return Size; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (syncRoot == null)
                    Interlocked.CompareExchange(ref syncRoot, new object(), null);
                return syncRoot;
            }
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            CopyTo((T[])array, arrayIndex);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        #endregion
    }
}


// might need this for float point summation https://en.wikipedia.org/wiki/Kahan_summation_algorithm

namespace MotionLib
{
    public class MotionStatsConfig
    {
        public int PreTriggerSeconds { set; get; }
        public int PostTriggerSeconds { set; get; }
        public int MessageRatePerSecond { set; get; }
    }

    public class MotionStatRecord : MessageRecordI
    {
        public ulong _motionRecordCount;

        public Motion.RotationStruct<double> RotSum = new Motion.RotationStruct<double>();
        public Vector3<double> AccelSum = new Vector3<double>();
        public Vector3<ulong> MagSum = new Vector3<ulong>();

        public Motion.RotationStruct<double> RotSumD2 = new Motion.RotationStruct<double>();
        public Vector3<double> AccelSumD2 = new Vector3<double>();
        public Vector3<ulong> MagSumD2 = new Vector3<ulong>();

        public Motion.RotationStruct<double> RotMean = new Motion.RotationStruct<double>();
        public Vector3<double> AccelMean = new Vector3<double>();
        public Vector3<ulong> MagAvg = new Vector3<ulong>();

        public Motion.RotationStruct<double> RotVar = new Motion.RotationStruct<double>();
        public Vector3<double> AccelVar = new Vector3<double>();
        public Vector3<ulong> MagVar = new Vector3<ulong>();

        public Motion.RotationStruct<double> RotStddev = new Motion.RotationStruct<double>();
        public Vector3<double> AccelStddev = new Vector3<double>();
        public Vector3<ulong> MagStddev = new Vector3<ulong>();
    }

    public class MotionStats
    {
        private MessageLib.MessageRecordReceivedDelegate _StatsReadyCallback;
        private System.IO.CircularBuffer<Motion.MotionStruct> _MessageBufferCurrent;
        private System.IO.CircularBuffer<Motion.MotionStruct> _MessageBuffer1;
        private System.IO.CircularBuffer<Motion.MotionStruct> _MessageBuffer2;
        private int _MessageBufferCurrentCapacity;
        private MotionStatsConfig _Config;
        private MotionStatRecord _cmr;

        private bool _calcRotation = true;
        private bool _calcAcceleration = true;
        private bool _calcMagnetometer = true;

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// 
        //*********************************************************************

        public MotionStats(MotionStatsConfig config)
        {
            _Config = config;

            _calcRotation = true;
            _calcAcceleration = false;
            _calcMagnetometer = false;

            _MessageBufferCurrentCapacity =
                _Config.PreTriggerSeconds * _Config.MessageRatePerSecond;

            if (null == _MessageBufferCurrent)
            {
                _MessageBuffer1 =
                    new System.IO.CircularBuffer<Motion.MotionStruct>(_MessageBufferCurrentCapacity, true);
                _MessageBuffer2 =
                    new System.IO.CircularBuffer<Motion.MotionStruct>(_MessageBufferCurrentCapacity, true);

                _MessageBufferCurrent = _MessageBuffer1;
            }
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*********************************************************************

        /// <param name="motionRecord"></param>
        public void AddRecord(Motion.MotionStruct motionRecord)
        {
            //await Task.Run(() => _MessageBufferCurrent.Put(motionRecord));

            //if (_MessageBufferCurrent == _MessageBuffer1)
            _MessageBufferCurrent.Put(new Motion.MotionStruct(motionRecord));
        }

        //*********************************************************************
        ///
        /// <summary>
        /// This lets us ceate stats for a period of time after the trigger event
        /// </summary>
        /// 
        //*********************************************************************

#if UNITY_WSA_10_0
        private async Task CaptureEndMessageTimer()
        {
            await Task.Delay(_Config.PostTriggerSeconds * 1000);

            //*** TODO * analyze the post trigger event

            //_EndingTimeMessage = _CurrentMessage; //*** TODO *
        }
#else
        private void CaptureEndMessageTimer()
        {
        }
#endif

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="motionRecordList"></param>
        /// 
        //*********************************************************************

#if UNITY_WSA_10_0

        public async Task GetStatsFromBuffer()
        {
            //_MessageBufferCurrent = _MessageBufferCurrent == _MessageBuffer1 ? _MessageBuffer2 : _MessageBuffer1;

            if (_MessageBufferCurrent == _MessageBuffer1)
            {
                _MessageBufferCurrent = _MessageBuffer2;
                await GetStatsAsync(_MessageBuffer1);
                //GetStats(_MessageBuffer1.ToArray());
            }
            else
            {
                _MessageBufferCurrent = _MessageBuffer1;
                await GetStatsAsync(_MessageBuffer2);
                //GetStats(_MessageBuffer2.ToArray());
            }
        }
#else
        public void GetStatsFromBuffer()
        {
        }
#endif

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="motionRecordList"></param>
        /// 
        //*********************************************************************

        private void GetStats(IEnumerable<Motion.MotionStruct> motionRecordList)
        {
            try
            {
                _cmr = new MotionStatRecord();

                GetSums(motionRecordList);
                CalcAverages();
                GetSumD2(motionRecordList);
                CalcStdDev();

#if UNITY_WSA_10_0
                //*** Send the stat record to the caller ***
                Task.Run(() => _StatsReadyCallback(_cmr));
#else
                _StatsReadyCallback(_cmr);
#endif
            }
            catch (Exception e)
            {
                var tt = e.Message;
                //*** Do something
            }
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="motionRecordList"></param>
        /// 
        //*********************************************************************

        private void GetSums(IEnumerable<Motion.MotionStruct> motionRecordList)
        {
            try
            {
                foreach (var motionRecord in motionRecordList)
                {
                    _cmr._motionRecordCount++;

                    if (_calcRotation)
                    {
                        _cmr.RotSum.y += motionRecord.YPR.y;
                        _cmr.RotSum.p += motionRecord.YPR.p;
                        _cmr.RotSum.r += motionRecord.YPR.r;
                    }

                    if (_calcAcceleration)
                    {
                        _cmr.AccelSum.x += motionRecord.Accell.x;
                        _cmr.AccelSum.y += motionRecord.Accell.y;
                        _cmr.AccelSum.z += motionRecord.Accell.z;
                    }

                    if (_calcMagnetometer)
                    {
                        _cmr.MagSum.x += (ulong)motionRecord.Magnetometer.x;
                        _cmr.MagSum.y += (ulong)motionRecord.Magnetometer.y;
                        _cmr.MagSum.z += (ulong)motionRecord.Magnetometer.z;
                    }
                }
            }
            catch (Exception e)
            {
                var tt = e.Message;
                //*** Do something
            }
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="motionRecordList"></param>
        /// 
        //*********************************************************************

        private void GetStatsTry(IEnumerable<Motion.MotionStruct> motionRecordList)
        {
            //******************
            /*_calcAcceleration = false;
            _calcMagnetometer = false;

            var mrl = new Motion.MotionStruct[9];

            for (var i = 0; i < 9; i++)
            {
                mrl[i] = new Motion.MotionStruct();
                mrl[i].YPR = new Motion.RotationStruct<double>();
            }

            int j = 0;
            mrl[j++].YPR.y = 1;
            mrl[j++].YPR.y = 2;
            mrl[j++].YPR.y = 3;
            mrl[j++].YPR.y = 4;
            mrl[j++].YPR.y = 5;
            mrl[j++].YPR.y = 4;
            mrl[j++].YPR.y = 3;
            mrl[j++].YPR.y = 2;
            mrl[j++].YPR.y = 1;

            motionRecordList = mrl;*/

            //*****************

            try
            {
                double delta;
                _cmr = new MotionStatRecord();

                foreach (var motionRecord in motionRecordList)
                {
                    _cmr._motionRecordCount++;

                    if (_calcRotation)
                    {
                        delta = motionRecord.YPR.y - _cmr.RotMean.y;
                        _cmr.RotMean.y += (delta / _cmr._motionRecordCount);
                        _cmr.RotSum.y += (delta * (motionRecord.YPR.y - _cmr.RotMean.y));

                        delta = motionRecord.YPR.p - _cmr.RotMean.p;
                        _cmr.RotMean.p += (delta / _cmr._motionRecordCount);
                        _cmr.RotSum.p += (delta * (motionRecord.YPR.p - _cmr.RotMean.p));

                        delta = motionRecord.YPR.r - _cmr.RotMean.r;
                        _cmr.RotMean.r += (delta / _cmr._motionRecordCount);
                        _cmr.RotSum.r += (delta * (motionRecord.YPR.r - _cmr.RotMean.r));
                    }

                    if (_calcAcceleration)
                    {
                        _cmr.AccelSum.x += motionRecord.Accell.x;
                        _cmr.AccelSum.y += motionRecord.Accell.y;
                        _cmr.AccelSum.z += motionRecord.Accell.z;
                    }

                    if (_calcMagnetometer)
                    {
                        _cmr.MagSum.x += (ulong)motionRecord.Magnetometer.x;
                        _cmr.MagSum.y += (ulong)motionRecord.Magnetometer.y;
                        _cmr.MagSum.z += (ulong)motionRecord.Magnetometer.z;
                    }
                }

                CalcStdDev();
            }
            catch (Exception e)
            {
                var tt = e.Message;
                //*** Do something
            }
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*********************************************************************

        private void CalcStdDev()
        {
            try
            {
                if (_calcRotation)
                {
                    _cmr.RotVar.y = _cmr.RotSumD2.y / (_cmr._motionRecordCount);
                    _cmr.RotVar.p = _cmr.RotSumD2.p / (_cmr._motionRecordCount);
                    _cmr.RotVar.r = _cmr.RotSumD2.r / (_cmr._motionRecordCount);

                    _cmr.RotStddev.y = Math.Sqrt(_cmr.RotVar.y);
                    _cmr.RotStddev.p = Math.Sqrt(_cmr.RotVar.p);
                    _cmr.RotStddev.r = Math.Sqrt(_cmr.RotVar.r);
                }

                if (_calcAcceleration)
                {
                }

                if (_calcMagnetometer)
                {
                }
            }
            catch (Exception e)
            {
                var tt = e.Message;
                //*** Do something
            }
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="motionRecordList"></param>
        /// 
        //*********************************************************************

#if UNITY_WSA_10_0

        public async Task GetStatsAsync(IEnumerable<Motion.MotionStruct> motionRecordList)
        {
            await Task.Run(() => GetStats(motionRecordList));
        }

#else

        public void GetStatsAsync(IEnumerable<Motion.MotionStruct> motionRecordList)
        {
        }
#endif

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*********************************************************************

        private void CalcAverages()
        {
            try
            {
                if (_calcRotation)
                {
                    _cmr.RotMean.y =
                        _cmr.RotSum.y / _cmr._motionRecordCount;
                    _cmr.RotMean.p =
                        _cmr.RotSum.p / _cmr._motionRecordCount;
                    _cmr.RotMean.r =
                        _cmr.RotSum.r / _cmr._motionRecordCount;
                }

                if (_calcAcceleration)
                {
                    _cmr.AccelMean.x = _cmr.AccelSum.x / _cmr._motionRecordCount;
                    _cmr.AccelMean.y = _cmr.AccelSum.y / _cmr._motionRecordCount;
                    _cmr.AccelMean.z = _cmr.AccelSum.z / _cmr._motionRecordCount;
                }

                if (_calcMagnetometer)
                {
                    _cmr.MagAvg.x =
                        _cmr.MagSum.x / _cmr._motionRecordCount;
                    _cmr.MagAvg.y =
                        _cmr.MagSum.y / _cmr._motionRecordCount;
                    _cmr.MagAvg.z =
                        _cmr.MagSum.z / _cmr._motionRecordCount;
                }
            }
            catch (Exception e)
            {
                var tt = e.Message;
                //*** Do something
            }
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="motionRecordList"></param>
        /// 
        //*********************************************************************

        private void GetSumD2(IEnumerable<Motion.MotionStruct> motionRecordList)
        {
            try
            {
                foreach (var motionRecord in motionRecordList)
                {
                    if (_calcRotation)
                    {
                        var dd = ((motionRecord.YPR.y - _cmr.RotMean.y) * (motionRecord.YPR.y - _cmr.RotMean.y));

                        _cmr.RotSumD2.y += ((motionRecord.YPR.y - _cmr.RotMean.y) * (motionRecord.YPR.y - _cmr.RotMean.y));
                        _cmr.RotSumD2.p += ((motionRecord.YPR.p - _cmr.RotMean.p) * (motionRecord.YPR.p - _cmr.RotMean.p));
                        _cmr.RotSumD2.r += ((motionRecord.YPR.r - _cmr.RotMean.r) * (motionRecord.YPR.r - _cmr.RotMean.r));
                    }

                    if (_calcAcceleration)
                    {
                        _cmr.AccelSum.x += motionRecord.Accell.x;
                        _cmr.AccelSum.y += motionRecord.Accell.y;
                        _cmr.AccelSum.z += motionRecord.Accell.z;
                    }

                    if (_calcMagnetometer)
                    {
                        _cmr.MagSum.x += (ulong)motionRecord.Magnetometer.x;
                        _cmr.MagSum.y += (ulong)motionRecord.Magnetometer.y;
                        _cmr.MagSum.z += (ulong)motionRecord.Magnetometer.z;
                    }
                }
            }
            catch (Exception e)
            {
                var tt = e.Message;
                //*** Do something
            }
        }

        //*********************************************************************
        ///
        /// <summary>
        /// Online sample from online
        /// </summary>
        /// 
        //*********************************************************************

        public static double StdDevOnline(IEnumerable<double> values)
        {
            // ref: http://warrenseen.com/blog/2006/03/13/how-to-calculate-standard-deviation/
            double mean = 0.0;
            double sum = 0.0;
            double stdDev = 0.0;
            int n = 0;
            foreach (double val in values)
            {
                n++;
                double delta = val - mean;
                mean += delta / n;
                sum += delta * (val - mean);
            }
            if (1 < n)
                stdDev = Math.Sqrt(sum / (n - 1));

            return stdDev;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*********************************************************************

#if UNITY_WSA_10_0
        public async Task StartCollecting(MessageLib.MessageRecordReceivedDelegate statsReadyCallback)
        {
            _StatsReadyCallback = statsReadyCallback;
        }
#else
        /*public async Task StartCollecting(MessageLib.MessageRecordReceivedDelegate statsReadyCallback)
        {
            _StatsReadyCallback = statsReadyCallback;
        }*/
#endif

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*********************************************************************

        /* public async Task StopCollecting()
         {

         }*/

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventType"></param>
        /// <returns></returns>
        /// 
        //*********************************************************************

#if UNITY_WSA_10_0

        private async Task SetEvent(Motion.MotionStructEventTypeEnum eventType)
        {
            switch (eventType)
            {
                case Motion.MotionStructEventTypeEnum.Trigger:

                    //*** create a trimer that will start analysis of the short after-trigger time period
                    CaptureEndMessageTimer();

                    //*** send the contents of the message buffer to analysis
                    await GetStatsFromBuffer();
                    break;
            }
        }

#else

        /*private async Task SetEvent(Motion.MotionStructEventTypeEnum eventType)
        {
            switch (eventType)
            {
                case Motion.MotionStructEventTypeEnum.Trigger:

                    //*** create a trimer that will start analysis of the short after-trigger time period
                    CaptureEndMessageTimer();

                    //*** send the contents of the message buffer to analysis
                    await GetStatsFromBuffer();
                    break;
            }
        }*/

#endif

        //*********************************************************************
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventType"></param>
        /// <returns></returns>
        /// 
        //*********************************************************************

#if UNITY_WSA_10_0

        public async Task SetEventAsync(Motion.MotionStructEventTypeEnum eventType)
        {
            await Task.Run(() => SetEvent(eventType));
        }

#else

        /*public async Task SetEventAsync(Motion.MotionStructEventTypeEnum eventType)
        {
            await Task.Run(() => SetEvent(eventType));
        }*/

#endif

    }
}

