using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

//using Xamarin.Forms;

namespace MotionLib
{
    public class MotionLib //: ContentPage
    {
        /*public MotionLib()
        {
            var button = new Button
            {
                Text = "Click Me!",
                VerticalOptions = LayoutOptions.CenterAndExpand,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
            };

            int clicked = 0;
            button.Clicked += (s, e) => button.Text = "Clicked: " + clicked++;

            Content = button;
        }*/
    }

    public class Motion
    {
        Byte[] _dataPacket = new Byte[22];  // InvenSense Teapot packet
        int _serialCount = 0;                 // current packet byte position
        int _synced = 0;
        int _interval = 0;
        private bool _stop = false;
        private bool _settled = false;
        private Quaternion _settledQuatOffset = null;

        float[] q = new float[4];

        Vector3<ulong> _accel = new Vector3<ulong>();
        VectorDouble _accelLin = new VectorDouble();
        VectorDouble _accelWorld = new VectorDouble();

        double[] _gravity = new double[3];
        double[] _euler = new double[3];
        double[] _ypr = new double[3];

        private int _max = 0;
        //private SerialPort _port = null;
        //private DataStorage _dataStorage = null;
        //private Thread _thread = null;

        public class RotationStruct<T>
        {
            public T y;
            public T p;
            public T r;
        }

        public enum MotionStructEventTypeEnum
        {
            motion,
            startSeries,
            endSeries,
            Trigger
        };

        public class MotionStruct
        {
            public RotationStruct<double> YPR;
            public Vector3<float> Accell;
            public Vector3<Int16> Magnetometer;
            public Int32 ElapsedTimeMs;
            public Quaternion Quat;

            public MotionStruct(bool preAlloc)
            {
                if (preAlloc)
                {
                    YPR = new RotationStruct<double>();
                    Accell = new Vector3<float>();
                    Magnetometer = new Vector3<short>();
                    Quat = new Quaternion();
                }
            }
            public MotionStruct(MotionStruct copyFrom)
            {
                this.YPR = new RotationStruct<double>();
                this.Accell = new Vector3<float>();
                this.Magnetometer = new Vector3<short>();
                this.Quat = new Quaternion();

                this.YPR.y = copyFrom.YPR.y;
                this.YPR.p = copyFrom.YPR.p;
                this.YPR.r = copyFrom.YPR.r;

                this.Accell.x = copyFrom.Accell.x;
                this.Accell.y = copyFrom.Accell.y;
                this.Accell.z = copyFrom.Accell.z;

                this.Magnetometer.x = copyFrom.Magnetometer.x;
                this.Magnetometer.y = copyFrom.Magnetometer.y;
                this.Magnetometer.z = copyFrom.Magnetometer.z;

                this.Quat.x = copyFrom.Quat.x;
                this.Quat.y = copyFrom.Quat.y;
                this.Quat.z = copyFrom.Quat.z;
                this.Quat.w = copyFrom.Quat.w;

                ElapsedTimeMs = copyFrom.ElapsedTimeMs;
                this.EventType = copyFrom.EventType;
            }

            public MotionStructEventTypeEnum EventType;

            /// <summary>
            /// MotionStruct
            /// </summary>
            public MotionStruct()
            {
            }
        }

        //*********************************************************************
        /// 
        ///  <summary>
        ///  
        ///  </summary>
        /// <param name="quat"></param>
        ///  
        //*********************************************************************
        public void SetSettledQuat(Quaternion quat)
        {
            _settled = true;
            _settledQuatOffset = quat.getCopy();
            _settledQuatOffset.w -= 1;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="dataStorage"></param>
        /// 
        //*********************************************************************

        /*public void ReadDataAsynch(SerialPort port, DataStorage dataStorage)
        {
            this._port = port;
            this._dataStorage = dataStorage;

            _thread = new Thread(ReadData);
            _thread.Start();
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="dataStorage"></param>
        /// 
        //*********************************************************************

        public void ReadData(SerialPort port, DataStorage dataStorage)
        {
            try
            {
                this._port = port;
                this._dataStorage = dataStorage;

                ReadData();

            }
            catch (Exception ex)
            {
                throw;
            }
        }*/

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataPkt"></param>
        /// <returns></returns>
        /// 
        //*********************************************************************

        public UInt32 GetMilliSeconds(Byte[] dataPkt)
        {
            var mSec = (dataPkt[19] << 24) | (dataPkt[18] << 16) | (dataPkt[17] << 8) | dataPkt[16];
            return Convert.ToUInt32(mSec);
        }

        //*********************************************************************
        /// 
        ///  <summary>
        ///  
        ///  </summary>
        /// <param name="dataPkt"></param>
        /// <param name="quat"></param>
        /// <returns></returns>
        ///  
        //*********************************************************************
        public Quaternion GetQuaternion(Byte[] dataPkt, Quaternion quat)
        {
            if (null == quat)
                quat = new Quaternion();

            quat.w = ((dataPkt[2] << 8) | dataPkt[3]) / 16384.0f;
            quat.x = ((dataPkt[4] << 8) | dataPkt[5]) / 16384.0f;
            quat.y = ((dataPkt[6] << 8) | dataPkt[7]) / 16384.0f;
            quat.z = ((dataPkt[8] << 8) | dataPkt[9]) / 16384.0f;

            if (quat.w >= 2)
                quat.w = -4 + quat.w;
            if (quat.x >= 2)
                quat.x = -4 + quat.x;
            if (quat.y >= 2)
                quat.y = -4 + quat.y;
            if (quat.z >= 2)
                quat.z = -4 + quat.z;

            if (_settled)
            {
                quat.w -= _settledQuatOffset.w;
                quat.x -= _settledQuatOffset.x;
                quat.y -= _settledQuatOffset.y;
                quat.z -= _settledQuatOffset.z;
            }

            //Console.WriteLine(string.Format("q:{0},{1},{2},{3}", q[0], q[1], q[2], q[3]));

            return quat;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataPkt"></param>
        /// <param name="rawAccel"></param>
        /// <returns></returns>
        /// 
        //*********************************************************************

        public Vector3<ulong> GetRawAccel_old(Byte[] dataPkt, Vector3<ulong> rawAccel)
        {
            if (null == rawAccel)
                rawAccel = new Vector3<ulong>();

            rawAccel.x = (ulong)((dataPkt[12] << 8) + dataPkt[13]);
            rawAccel.y = (ulong)((dataPkt[14] << 8) + dataPkt[15]);
            rawAccel.z = (ulong)((dataPkt[16] << 8) + dataPkt[17]);

            return rawAccel;
        }

        /*public Vector3<ulong> GetRawAccel(Byte[] dataPkt, Vector3<ulong> rawAccel)
        {
            if (null == rawAccel)
                rawAccel = new Vector3<ulong>();

            rawAccel.x = (ulong)((dataPkt[10] << 8) + dataPkt[11]);
            rawAccel.y = (ulong)((dataPkt[12] << 8) + dataPkt[13]);
            rawAccel.z = (ulong)((dataPkt[14] << 8) + dataPkt[15]);

            return rawAccel;
        }*/

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataPkt"></param>
        /// <param name="rawAccel"></param>
        /// <returns></returns>
        /// 
        //*********************************************************************

        public Vector3<Int16> GetRawAccel(Byte[] dataPkt, Vector3<Int16> rawAccel)
        {
            if (null == rawAccel)
                rawAccel = new Vector3<Int16>();

            rawAccel.x = (Int16)((dataPkt[10] << 8) + dataPkt[11]);
            rawAccel.y = (Int16)((dataPkt[12] << 8) + dataPkt[13]);
            rawAccel.z = (Int16)((dataPkt[14] << 8) + dataPkt[15]);

            return rawAccel;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataPkt"></param>
        /// <param name="rawMag"></param>
        /// <returns></returns>
        /// 
        //*********************************************************************

        public Vector3<Int16> GetRawMagnetometer(Byte[] dataPkt, Vector3<Int16> rawMag)
        {
            if (null == rawMag)
                rawMag = new Vector3<Int16>();

            rawMag.x = (Int16)((dataPkt[16] << 8) + dataPkt[17]);
            rawMag.y = (Int16)((dataPkt[18] << 8) + dataPkt[19]);
            rawMag.z = (Int16)((dataPkt[20] << 8) + dataPkt[21]);

            return rawMag;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataPkt"></param>
        /// <returns></returns>
        /// 
        //*********************************************************************

        public Int32 GetElapsedTime(Byte[] dataPkt)
        {
            //*** MPU 6050 ***
            //Int32 elapsedTime = (Int32)(dataPkt[16] + (dataPkt[17] << 8) + (dataPkt[18] << 16) + (dataPkt[19] << 24));

            //*** MPU 9250 ***
            //Int32 elapsedTime = (Int32)(dataPkt[22] + (dataPkt[23] << 8) + (dataPkt[24] << 16) + (dataPkt[25] << 24));
            Int32 elapsedTime = (Int32)(dataPkt[22] << 24) + (dataPkt[23] << 16) + (dataPkt[24] << 8) + (dataPkt[25]);

            return elapsedTime;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="quat"></param>
        /// <param name="gravity"></param>
        /// <returns></returns>
        /// 
        //*********************************************************************

        public Vector3<double> GetGravity(Quaternion quat, Vector3<double> gravity)
        {
            if (null == gravity)
                gravity = new Vector3<double>();

            gravity.x = 2 * (quat.x * quat.z - quat.w * quat.y);
            gravity.y = 2 * (quat.w * quat.x + quat.y * quat.z);
            gravity.z = quat.w * quat.w - quat.x * quat.x - quat.y * quat.y + quat.z * quat.z;

            return gravity;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="quat"></param>
        /// <param name="gravity"></param>
        /// <param name="ypr"></param>
        /// <returns></returns>
        /// 
        //*********************************************************************

        public Vector3<double> GetYPR(Quaternion quat, Vector3<double> gravity, Vector3<double> ypr)
        {
            if (null == ypr)
                ypr = new Vector3<double>();

            ypr.x = Math.Atan2(2 * quat.x * quat.y - 2 * quat.w * quat.z, 2 * quat.w * quat.w + 2 * quat.x * quat.x - 1);
            ypr.y = Math.Atan(gravity.x / Math.Sqrt(gravity.y * gravity.y + gravity.z * gravity.z));
            ypr.z = Math.Atan(gravity.y / Math.Sqrt(gravity.x * gravity.x + gravity.z * gravity.z));

            return ypr;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="quat"></param>
        /// <param name="euler"></param>
        /// <returns></returns>
        /// 
        //*********************************************************************

        public Vector3<double> GetEuler(Quaternion quat, Vector3<double> euler)
        {
            if (null == euler)
                euler = new Vector3<double>();

            euler.x = Math.Atan2(2 * quat.x * quat.y - 2 * quat.w * quat.z, 2 * quat.w * quat.w + 2 * quat.x * quat.x - 1);
            euler.y = -Math.Asin(2 * quat.x * quat.z + 2 * quat.w * quat.y);
            euler.z = Math.Atan2(2 * quat.y * quat.z - 2 * quat.w * quat.x, 2 * quat.w * quat.w + 2 * quat.z * quat.z - 1);

            return euler;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawAccel"></param>
        /// <param name="gravity"></param>
        /// <returns></returns>
        /// 
        //*********************************************************************

        public Vector3<double> GetLinAccel(Vector3<Int16> rawAccel,
            Vector3<double> gravity, Vector3<double> linAccel)
        {
            if (null == linAccel)
                linAccel = new Vector3<double>();

            linAccel.x = rawAccel.x - gravity.x * 8192;
            linAccel.y = rawAccel.y - gravity.y * 8192;
            linAccel.z = rawAccel.z - gravity.z * 8192;

            return linAccel;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accelLin"></param>
        /// <param name="quat"></param>
        /// <param name="accelWorld"></param>
        /// <returns></returns>
        /// 
        //*********************************************************************

        public VectorDouble GetWorldAccel(Vector3<double> accelLin,
            Quaternion quat, VectorDouble accelWorld)
        {
            if (null == accelWorld)
                accelWorld = new VectorDouble();

            accelWorld.x = accelLin.x;
            accelWorld.y = accelLin.y;
            accelWorld.z = accelLin.z;

            accelWorld.rotate(quat);

            return accelWorld;
        }

        //*********************************************************************
        /// 
        ///  <summary>
        ///  
        ///  </summary>
        ///  <param name="dataPkt"></param>
        /// <param name="loopCounter"></param>
        /// 
        ///  [0] = '-';                        // record type ID
        ///  [1 + offset] = motionRecordsRead; // records read count
        ///
        ///  [2 + offset] = fifoBuffer[0];     // Quat W
        ///  [3 + offset] = fifoBuffer[1];
        ///  [4 + offset] = fifoBuffer[4];     // Quat X
        ///  [5 + offset] = fifoBuffer[5];
        ///  [6 + offset] = fifoBuffer[8];     // Quat Y
        ///  [7 + offset] = fifoBuffer[9];
        ///  [8 + offset] = fifoBuffer[12];    // Quat Z
        ///  [9 + offset] = fifoBuffer[13];
        ///  
        ///  [10 + offset] = fifoBuffer[28];   // Acc X
        ///  [11 + offset] = fifoBuffer[29];
        ///  [12 + offset] = fifoBuffer[32];   // Acc Y
        ///  [13 + offset] = fifoBuffer[33];
        ///  [14 + offset] = fifoBuffer[36];   // Acc Z
        ///  [15 + offset] = fifoBuffer[37];
        ///  
        ///  [16 + offset] = t_now2.v[0];      // Mills 0
        ///  [17 + offset] = t_now2.v[1];      // Mills 1
        ///  [18 + offset] = t_now2.v[2];      // Mills 2
        ///  [19 + offset] = t_now2.v[3];      // Mills 3
        ///  
        ///  [20 + offset] = '/r';             // terminator
        ///  [21 + offset] = '/n';             // terminator
        /// 
        //*********************************************************************
        public MotionStruct GetMotion(Byte[] dataPkt, int loopCounter, MotionStruct ms)
        {
            var quat = new Quaternion();
            var rawAccel = new Vector3<Int16>();
            var gravity = new Vector3<double>();
            var ypr = new Vector3<double>();
            var euler = new Vector3<double>();
            var accelLin = new Vector3<double>();
            var magnetometer = new Vector3<Int16>();

            quat = GetQuaternion(dataPkt, quat);

            // $	14631	-917	-884	-7262	94637
            //Console.WriteLine(string.Format("{0},{1},{2},{3}", quat.w, quat.x, quat.y, quat.z));

            rawAccel = GetRawAccel(dataPkt, rawAccel);
            gravity = GetGravity(quat, gravity);
            magnetometer = this.GetRawMagnetometer(dataPkt, magnetometer);
            euler = GetEuler(quat, ypr);
            ypr = GetYPR(quat, gravity, ypr);
            accelLin = GetLinAccel(rawAccel, gravity, accelLin);
            _accelWorld = GetWorldAccel(accelLin, quat, _accelWorld);

            if (0 == loopCounter++ % 10)
            {
                //Console.WriteLine(string.Format("ypr:\t{0}\t{1}\t{2}\t{3}",
                //    round(quat.w * 100.0f) / 100.0f, round(quat.x * 100.0f) / 100.0f,
                //    round(quat.y * 100.0f) / 100.0f, round(quat.z * 100.0f) / 100.0f));

                Debug.WriteLine(string.Format("ypr:\t{0}\t{1}\t{2}",
                    ypr.x * 180.0f / Math.PI, ypr.y * 180.0f / Math.PI, ypr.z * 180.0f / Math.PI));

                Debug.WriteLine(string.Format("euler:\t{0}\t{1}\t{2}",
                   euler.x * 180.0f / Math.PI, euler.y * 180.0f / Math.PI, euler.z * 180.0f / Math.PI));

                Debug.WriteLine(string.Format("accelRaw:\t{0}\t{1}\t{2}",
                    _accel.x, _accel.y, _accel.z));

                //Console.WriteLine(string.Format("accelReal:\t{0}\t{1}\t{2}",
                //    accelLin.x, accelLin.y, accelLin.z));

                //Console.WriteLine(string.Format("accelWorld:\t{0}\t{1}\t{2}",
                //    accelWorld.x, accelWorld.y, accelWorld.z));
            }

            if (null == ms)
                ms = new MotionStruct(true);

            ms.YPR.y = ypr.x * 180.0f / Math.PI;
            ms.YPR.p = ypr.y * 180.0f / Math.PI;
            ms.YPR.r = ypr.z * 180.0f / Math.PI;

            ms.Accell.x = (float)_accelWorld.x;
            ms.Accell.y = (float)_accelWorld.y;
            ms.Accell.z = (float)_accelWorld.z;

            ms.Magnetometer.x = magnetometer.x;
            ms.Magnetometer.y = magnetometer.y;
            ms.Magnetometer.z = magnetometer.z;

            ms.Quat = quat;

            ms.ElapsedTimeMs = GetElapsedTime(dataPkt);

            return ms;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*********************************************************************

        /*private void ReadDataOrig()
        {
            //interval = millis();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            int loopCounter = 0;

            while (true)
            {
                if (_stop)
                {
                    if (null != _dataStorage)
                        _dataStorage.Stop();

                    return;
                }

                if (1 > _port.BytesToRead)
                    continue;

                int ch = _port.ReadByte();

                //Console.WriteLine(ch.ToString());
                //continue;

                if (_synced == 0 && ch != '$')
                    continue;   // initial synchronization - also used to resync/realign if needed

                _synced = 1;

                //print((char)ch);
                //println(CharCount++);

                //if ((serialCount == 1 && ch != 2)
                //    || (serialCount == 12 && ch != '\r')
                //    || (serialCount == 13 && ch != '\n'))

                if ((_serialCount == 1 && ch != 2)
                    || (_serialCount == 20 && ch != '\r')
                    || (_serialCount == 21 && ch != '\n'))
                {
                    _serialCount = 0;
                    _synced = 0;
                    continue;
                }

                if (_serialCount > 0 || ch == '$')
                {
                    _dataPacket[_serialCount++] = (byte)ch;
                    //dataPacket[serialCount++] = (ulong)ch;

                    //if (serialCount == 14)
                    if (_serialCount == 22)
                    {
                        //if (max++ > 10)
                        //    return;

                        _dataStorage.Store(_dataPacket, stopwatch.ElapsedMilliseconds);

                        _serialCount = 0; // restart packet byte position

                        // get quaternion from data packet
                        //var q1 = ((teapotPacket[2] << 8) | teapotPacket[3]);
                        //ulong q2 = ((teapotPacket[4] << 8) | teapotPacket[5]);
                        //ulong q3 = ((teapotPacket[6] << 8) | teapotPacket[7]);
                        //ulong q4 = ((teapotPacket[8] << 8) | teapotPacket[9]);

                        // $	14631	-917	-884	-7262	94637
                        //Console.WriteLine(string.Format("{0},{1},{2},{3}", q1, q2, q3, q4));

                        // get quaternion from data packet
                        q[0] = ((_dataPacket[2] << 8) | _dataPacket[3]) / 16384.0f;
                        q[1] = ((_dataPacket[4] << 8) | _dataPacket[5]) / 16384.0f;
                        q[2] = ((_dataPacket[6] << 8) | _dataPacket[7]) / 16384.0f;
                        q[3] = ((_dataPacket[8] << 8) | _dataPacket[9]) / 16384.0f;

                        for (int i = 0; i < 4; i++)
                            if (q[i] >= 2)
                                q[i] = -4 + q[i];

                        //Console.WriteLine(string.Format("q:{0},{1},{2},{3}", q[0], q[1], q[2], q[3]));

                        // set our toxilibs quaternion to new data
                        //quat.set(q[0], q[1], q[2], q[3]);

                        var quat = new Quaternion(q[0], q[1], q[2], q[3]);

                        //Console.WriteLine(string.Format("{0},{1},{2},{3}", q[0], q[1], q[2], q[3]));

                        //*************************************************

                        _accel.x = (ulong)((_dataPacket[12] << 8) + _dataPacket[13]);
                        _accel.y = (ulong)((_dataPacket[14] << 8) + _dataPacket[15]);
                        _accel.z = (ulong)((_dataPacket[16] << 8) + _dataPacket[17]);

                        //*************************************************

                        // below calculations unnecessary for orientation only using toxilibs

                        // calculate gravity vector
                        _gravity[0] = 2 * (q[1] * q[3] - q[0] * q[2]);
                        _gravity[1] = 2 * (q[0] * q[1] + q[2] * q[3]);
                        _gravity[2] = q[0] * q[0] - q[1] * q[1] - q[2] * q[2] + q[3] * q[3];

                        // calculate Euler angles
                        _euler[0] = Math.Atan2(2 * q[1] * q[2] - 2 * q[0] * q[3], 2 * q[0] * q[0] + 2 * q[1] * q[1] - 1);
                        _euler[1] = -Math.Asin(2 * q[1] * q[3] + 2 * q[0] * q[2]);
                        _euler[2] = Math.Atan2(2 * q[2] * q[3] - 2 * q[0] * q[1], 2 * q[0] * q[0] + 2 * q[3] * q[3] - 1);

                        // calculate yaw/pitch/roll angles
                        _ypr[0] = Math.Atan2(2 * q[1] * q[2] - 2 * q[0] * q[3], 2 * q[0] * q[0] + 2 * q[1] * q[1] - 1);
                        _ypr[1] = Math.Atan(_gravity[0] / Math.Sqrt(_gravity[1] * _gravity[1] + _gravity[2] * _gravity[2]));
                        _ypr[2] = Math.Atan(_gravity[1] / Math.Sqrt(_gravity[0] * _gravity[0] + _gravity[2] * _gravity[2]));

                        // calculate linear accel

                        _accelLin.x = _accel.x - _gravity[0] * 8192;
                        _accelLin.y = _accel.y - _gravity[1] * 8192;
                        _accelLin.z = _accel.z - _gravity[2] * 8192;

                        // calculate world accel

                        _accelWorld.x = _accelLin.x;
                        _accelWorld.y = _accelLin.y;
                        _accelWorld.z = _accelLin.z;

                        _accelWorld.rotate(quat);

                        // output various components for debugging
                        //println("q:\t" + round(q[0]*100.0f)/100.0f + "\t" + round(q[1]*100.0f)/100.0f + "\t" + round(q[2]*100.0f)/100.0f + "\t" + round(q[3]*100.0f)/100.0f);
                        //println("euler:\t" + euler[0]*180.0f/PI + "\t" + euler[1]*180.0f/PI + "\t" + euler[2]*180.0f/PI);
                        //println("ypr:\t" + ypr[0]*180.0f/PI + "\t" + ypr[1]*180.0f/PI + "\t" + ypr[2]*180.0f/PI);


                        if (0 == loopCounter++ % 10)
                        {
                            Debug.WriteLine(string.Format("ypr:\t{0}\t{1}\t{2}",
                                _ypr[0] * 180.0f / Math.PI, _ypr[1] * 180.0f / Math.PI, _ypr[2] * 180.0f / Math.PI));

                            //Console.WriteLine(string.Format("accelWorld:\t{0}\t{1}\t{2}",
                            //    accelWorld.x, accelWorld.y, accelWorld.z));

                            //Console.WriteLine(string.Format("accelReal:\t{0}\t{1}\t{2}",
                            //    accelLin.x, accelLin.y, accelLin.z));

                            Debug.WriteLine(string.Format("accelRaw:\t{0}\t{1}\t{2}",
                                _accel.x, _accel.y, _accel.z));
                        }
                    }
                }
            }
        }

        private void ReadData_25()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var loopCounter = 0;
            var dataPkt = new Byte[25];
            int ch;

            while (true)
            {
                if (_stop)
                {
                    if (null != _dataStorage)
                        _dataStorage.Stop();

                    return;
                }

                if (1 > _port.BytesToRead)
                    continue;

                ch = _port.ReadByte();

                if (_synced == 0 && ch != '$')
                    continue;   // initial synchronization - also used to resync/realign if needed

                _synced = 1;

                if ((_serialCount == 1 && ch != 2)
                    || (_serialCount == 23 && ch != '\r')
                    || (_serialCount == 24 && ch != '\n'))
                {
                    _serialCount = 0;
                    _synced = 0;
                    continue;
                }

                if (_serialCount > 0 || ch == '$')
                {
                    dataPkt[_serialCount++] = (byte)ch;

                    if (_serialCount == 25)
                    {
                        _serialCount = 0; // restart packet byte position
                        _dataStorage.Store(_dataPacket, stopwatch.ElapsedMilliseconds);
                        GetMotion(dataPkt, loopCounter++);
                    }
                }
            }
        }
        private void ReadData()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var loopCounter = 0;
            var dataPkt = new Byte[22];
            int ch;

            while (true)
            {
                if (_stop)
                {
                    if (null != _dataStorage)
                        _dataStorage.Stop();

                    return;
                }

                if (1 > _port.BytesToRead)
                    continue;

                ch = _port.ReadByte();

                if (_synced == 0 && ch != '$')
                    continue;   // initial synchronization - also used to resync/realign if needed

                _synced = 1;

                if ((_serialCount == 1 && ch != 2)
                    || (_serialCount == 20 && ch != '\r')
                    || (_serialCount == 21 && ch != '\n'))
                {
                    _serialCount = 0;
                    _synced = 0;
                    continue;
                }

                if (_serialCount > 0 || ch == '$')
                {
                    dataPkt[_serialCount++] = (byte)ch;

                    if (_serialCount == 22)
                    {
                        _serialCount = 0; // restart packet byte position
                        //_dataStorage.Store(_dataPacket, stopwatch.ElapsedMilliseconds);
                        _dataStorage.Store(dataPkt, stopwatch.ElapsedMilliseconds);
                        GetMotion(dataPkt, loopCounter++);
                    }
                }
            }
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// 
        //*********************************************************************

        public void ReadBytes(SerialPort port)
        {
            int intReturnASCII = 0;
            char charReturnValue = (Char)intReturnASCII;
            int totalBytesReceived = 0;
            int loopBytesReceived = 0;
            int bytesReceived = 0;
            char[] readBuffer = new char[1024];
            int bytesToRead;

            int count = 0;
            string returnMessage = "";

            Byte B;

            while (true)
            {
                count = port.BytesToRead;
                bytesToRead = count > 1024 ? 1024 : count;

                port.ReadByte();

                while (count > 0)
                {
                    bytesReceived = port.Read(readBuffer, 0, bytesToRead);
                    loopBytesReceived += bytesReceived;
                    totalBytesReceived += bytesReceived;

                    //intReturnASCII = _port.ReadByte();
                    //returnMessage += Convert.ToString(intReturnASCII);

                    //totalBytesReceived++;

                    if (loopBytesReceived > 500)
                    {
                        Console.WriteLine(totalBytesReceived.ToString());
                        loopBytesReceived = 0;
                        //Console.WriteLine(returnMessage);
                        //returnMessage = "";
                    }

                    count--;
                }
            }
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*********************************************************************

        public void StopReadingDevice()
        {
            _stop = true;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// 
        //*********************************************************************

        public void ReadDevice(string portName, int baudRate, string fileName, int readBufferSize)
        {
            var serPort = new SerialPort(portName, baudRate);
            serPort.Open();
            serPort.WriteLine("d");

            //var ds = new DataStorage(@"C:\\data\ttdata.txt", 10000, 
            //    DataStorage.StorageFormatEnum.Binary);

            var ds = new DataStorage(fileName, readBufferSize,
                DataStorage.StorageFormatEnum.Dcb, DataStorage.StorageModeEnum.Write);

            ReadDataAsynch(serPort, ds);
        }*/
    }

}





