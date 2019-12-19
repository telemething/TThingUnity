//#if UNITY_EDITOR
#undef UNITY_WSA_10_0
//#endif 

// Preproc values
// http://docs.unity3d.com/Manual/PlatformDependentCompilation.html 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using GeoLib;
using Newtonsoft.Json;
//using System.Threading.Tasks;
//using UnityEditor.VersionControl;
//using Windows.Storage.Streams;
using Debug = UnityEngine.Debug;
using Formatting = System.Xml.Formatting;

//using UnityEngine.JsonUtility;


#if UNITY_WSA_10_0
using Windows.Networking.Sockets;
using System.Threading.Tasks;
#endif

//*************************************************************************
///
/// <summary>
/// 
/// </summary>
/// <returns></returns>
/// 
//*************************************************************************

public class ThingMotion
{

#if !UNITY_WSA_10_0
    Thread _receiveThread;
    UdpClient _client;
#else
    
    public DatagramSocket socket = null;
    byte[] _udpInBuffer = new byte[1000];
    
#endif

    int _totalCount = 0;
    private Stopwatch _stopwatch = null;
    private long _startTimeMs = 0;
    private MotionLib.Motion _motion;

    private static ThingMotion _theMotionSingleton;
    private static ThingMotion _thePoseSingleton;

    private PointLatLonAlt _origin = null;
    public PointLatLonAlt Origin
    {
        set 
        {
            _origin = value;
            _things.Origin = value;
        }
        get => _origin;
    }

    // public
    // public string IP = "127.0.0.1"; default local
    public int _port; // define > init
    //public float Roll;
    //public float Pitch;
    //public float Yaw;
    public MotionLib.Motion.MotionStruct Motiondata = new MotionLib.Motion.MotionStruct(true);
    private bool _isCollectingStats = false;
    private MotionLib.MotionStats _MotionStats;

    // infos
    public string lastReceivedUDPPacket = "";
    public string allReceivedUDPPackets = ""; // clean up this from time to time!

    public enum modeEnum
    {
        none,
        motion,
        pose
    };

    private ThingCollection _things = new ThingCollection();

    private modeEnum _mode = modeEnum.none;

    //private ThingPose _pose = new ThingPose();

    //public ThingPose PoseData => _pose;

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// 
    //*************************************************************************

    public static ThingMotion GetMotionObject()
    {
        int port = 45678;

        if (null == _theMotionSingleton)
        {
            Debug.Log("###### NEW MOTION SINGLETON ######");
            _theMotionSingleton = new ThingMotion();
            _theMotionSingleton.Init(port, modeEnum.motion);
        }
        else
        {
            Debug.Log("------ Reused MOTION SINGLETON ------");
        }

        return _theMotionSingleton;
    }

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// 
    //*************************************************************************

    public static ThingMotion GetPoseObject(int port)
    {
        if (null == _thePoseSingleton)
        {
            Debug.Log("###### NEW POSE SINGLETON ######");
            _thePoseSingleton = new ThingMotion();
            _thePoseSingleton.Init(port, modeEnum.pose);
        }
        else
        {
            Debug.Log("------ Reused POSE SINGLETON ------");
        }

        return _thePoseSingleton;
    }


#if !UNITY_WSA_10_0

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// 
    //*************************************************************************
    public void Init(int udpPort, modeEnum mode)
    {
        _port = udpPort;
        _mode = mode;

        _receiveThread = new Thread(ReceiveData) { IsBackground = true };
        _receiveThread.Start();
    }

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// 
    //*************************************************************************
    private void ReceiveData()
    {
        var groupEp = new IPEndPoint(IPAddress.Any, _port);
        //_client = new UdpClient(_port);
        _client = new UdpClient {ExclusiveAddressUse = false, EnableBroadcast = true};

        _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
        _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        _client.Client.Bind(groupEp);
        
        var anyIp = new IPEndPoint(IPAddress.Any, 0);

        switch (_mode)
        {
            case modeEnum.motion:
                _motion = new MotionLib.Motion();
                break;
            case modeEnum.pose:
                _motion = new MotionLib.Motion();
                break;
        }

        while (true)
        {
            try
            {
                var data = _client.Receive(ref anyIp);
                
                switch (_mode)
                {
                    case modeEnum.motion:
                        var text = ByteArrayToString(data, 0, data.Length);
                        //print(">> " + text);
                        lastReceivedUDPPacket = text;
                        ProcessMotionRecords(data, data.Length);
                        //allReceivedUDPPackets=allReceivedUDPPackets+text;
                        break;
                    case modeEnum.pose:
                        ProcessPoseRecords(data, data.Length);
                        break;
                }
            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

#else

    string msg = "x";

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// 
    //*************************************************************************

    public async void Init()
        {
            port = 45678;

            if(null == _motion)
                _motion = new MotionLib.Motion();

            socket = new DatagramSocket();
            socket.MessageReceived += Socket_MessageReceived;

            try
            {
                await socket.BindEndpointAsync(null, "45678");
                //await socket.BindServiceNameAsync(port.ToString());
            }
            catch (Exception e)
            {

                msg = e.Message;

                //Debug.Log(e.ToString());
                //Debug.Log(SocketError.GetStatus(e.HResult).ToString());
                //return;
            }

            var msg2 = msg;

        }

        //*************************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// 
        //*************************************************************************

    private async void Socket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender,
            Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
    {
        //var udpInStream = args.GetDataStream().AsStreamForRead();
        int dataReadlength;

        try
        {
            //var udpInStream = args.GetDataStream().AsStreamForRead();
            var udpInStream1 = args.GetDataStream();
            var udpInStream = udpInStream1.AsStreamForRead();
            dataReadlength = await udpInStream.ReadAsync(_udpInBuffer, 0, _udpInBuffer.Length);
        }
        catch (Exception ex)
        {
            Debug.Log($"Socket_MessageReceived() 1: {ex.Message} --- {ex.StackTrace}");
            return;
        }

        try
        {
            lastReceivedUDPPacket = ByteArrayToString(_udpInBuffer, 0, dataReadlength);

            ProcessRecords(_udpInBuffer, dataReadlength);

            //lastReceivedUDPPacket = ByteArrayToString(_udpInBuffer, 0, dataReadlength);
            //print(">> " + lastReceivedUDPPacket);

            //Debug.Log("MESSAGE: " + message);
        }
        catch (Exception ex)
        {
            Debug.Log($"Socket_MessageReceived() 2: {ex.Message} --- {ex.StackTrace}");
        }
    }

#endif

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// 
    //*************************************************************************

    public string getLatestUDPPacket()
    {
        allReceivedUDPPackets = "";
        return lastReceivedUDPPacket;
    }

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// 
    //*************************************************************************

    public void OnDisable()
    {
#if !UNITY_WSA_10_0
        if (_receiveThread != null)
            _receiveThread.Abort();

        _client.Close();
#endif
    }

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// 
    //*************************************************************************

    public void OnApplicationQuit()
    {
#if !UNITY_WSA_10_0
        if (_receiveThread != null)
            _receiveThread.Abort();
#endif
        //receiver.Close();
    }

    const int UdpPacketLenMpu6050 = 22;
    const int UdpPacketLenMpu9250 = 28;

    //*********************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dataPkt"></param>
    /// <param name="dataLength"></param>
    /// 
    //*********************************************************************

#if UNITY_WSA_10_0
    private async Task ProcessPoseRecords(byte[] dataPkt, int dataLength)
#else
    private void ProcessPoseRecords(byte[] dataPkt, int dataLength)
#endif
    {
        try
        {
            var text = Encoding.ASCII.GetString(dataPkt, 0, dataLength);
            print(">> " + text);

            _things.SetPose(text);

            //_pose.SetVals(text);
        }
        catch (Exception e)
        {
            throw new Exception(
                $"ProcessPoseRecords() : unable to process record : '{e.Message}'", 
                e.InnerException);
        }
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thingId"></param>
    //*********************************************************************

    public ThingPose GetThingPose(string thingId)
    {
        return _things.GetPose(thingId);
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*********************************************************************

    public List<Thing> GetThings()
    {
        return _things.GetThings();
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thingId"></param>
    /// <param name="type"></param>
    /// <param name="self"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    //*********************************************************************

    public Thing SetThing(string thingId, Thing.TypeEnum type,
        Thing.SelfEnum self, Thing.RoleEnum role)
    {
        return _things.SetThing(thingId, type, self, role);
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*********************************************************************

    public List<ThingPose> GetThingsPose()
    {
        return _things.GetPoses();
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dataPkt"></param>
    //*********************************************************************

    private void ProcessRecords_notyet(byte[] dataPkt)
    {
        var length = UdpPacketLenMpu9250;
        MotionLib.Motion.MotionStruct ms;
        var start = 0;

        for (var index = 0; start < dataPkt.Length; index++)
        {
            _totalCount++;
            var segment = new ArraySegment<byte>(dataPkt, start, length);

            //var data = ByteArrayToString(segment.ToArray(), 0, length);

            //var datas = string.Format("{0}-{1}-{2}-{3}-{4}",
            //    data.Substring(0, 4), data.Substring(4, 16), 
            //    data.Substring(20, 12), data.Substring(32, 8), 
            //    data.Substring(40, 4));

            //System.Diagnostics.Debug.WriteLine(data);
            //System.Diagnostics.Debug.WriteLine(datas);

            //Debug.WriteLine("Hit:\t{0}", _totalCount);

            if (0 == index++ % 2)
            {
                ms = _motion.GetMotion(segment.Array, 1, Motiondata);

                if (0 == _startTimeMs)
                {
                    _startTimeMs = ms.ElapsedTimeMs;
                    _stopwatch = Stopwatch.StartNew();
                }

                while (_stopwatch.ElapsedMilliseconds + _startTimeMs < ms.ElapsedTimeMs) ;


                //_MotionCallback?.Invoke(ms);
            }

            //_ms.ypr.y = ms.ypr.y;
            //_ms.ypr.p = ms.ypr.p;
            //_ms.ypr.r = ms.ypr.r;

            //embeddedImage1.Rotation = _ms.ypr.y;
            //embeddedImage2.Rotation = _ms.ypr.p;
            //embeddedImage3.Rotation = _ms.ypr.r;

            start += length;
        }
    }

    //*********************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dataPkt"></param>
    /// <param name="dataLength"></param>
    /// 
    //*********************************************************************

#if UNITY_WSA_10_0
    private async Task ProcessMotionRecords(byte[] dataPkt, int dataLength)
#else
    private void ProcessMotionRecords(byte[] dataPkt, int dataLength)
#endif
    {
        ArraySegment<byte> segment;
        var length = UdpPacketLenMpu9250;
        var start = 0;
        MotionLib.Motion.MotionStruct ms;

        for (var index = 0; start < dataLength; index++)
        {
            _totalCount++;
            segment = new ArraySegment<byte>(dataPkt, start, length);

            /*var data = ByteArrayToString(segment.Array, 0, length);

            var datas = string.Format("{0}-{1}-{2}-{3}-{4}",
                data.Substring(0, 4), data.Substring(4, 16),
                data.Substring(20, 12), data.Substring(32, 8),
                data.Substring(40, 4));*/

            //System.Diagnostics.Debug.WriteLine(data);
            //System.Diagnostics.Debug.WriteLine(datas);
            //print(datas);

            //Debug.WriteLine("Hit:\t{0}", _totalCount);
            //print("Hit:\t" + _totalCount);


            if (0 == index++ % 3)
            {
                ms = _motion.GetMotion(segment.Array, 1, Motiondata);

                if (_isCollectingStats)
                    _MotionStats.AddRecord(ms);

                if (0 == _startTimeMs)
                {
                    _startTimeMs = ms.ElapsedTimeMs;
                    _stopwatch = Stopwatch.StartNew();
                }

#if UNITY_WSA_10_0
                var deltaTime = ms.ElapsedTimeMs - (_stopwatch.ElapsedMilliseconds + _startTimeMs);

                if (0 < deltaTime)
                    await Task.Delay((int)(deltaTime));
#else
                while (_stopwatch.ElapsedMilliseconds + _startTimeMs < ms.ElapsedTimeMs) ;
#endif
                //_MotionCallback?.Invoke(ms);
            }

            //*** Set public values 
            //Roll = (float)Motiondata.ypr.r;
            //Pitch = (float)Motiondata.ypr.p;
            //Yaw = (float)Motiondata.ypr.y;


            //Motiondata.ypr.y = ms.ypr.y;
            //Motiondata.ypr.p = ms.ypr.p;
            //Motiondata.ypr.r = ms.ypr.r;

            //embeddedImage1.Rotation = Motiondata.ypr.y;
            //embeddedImage2.Rotation = Motiondata.ypr.p;
            //embeddedImage3.Rotation = Motiondata.ypr.r;

            start += length;
        }
    }

    private const int StatsInterval = 1000;

#if UNITY_WSA_10_0
    private void StatsTimer(object state)
    {
        _MotionStats.SetEventAsync(MotionLib.Motion.MotionStructEventTypeEnum.Trigger);
    }
    private async Task StatsTimery()
    {
        while (true)
        {
            await System.Threading.Tasks.Task.Delay(StatsInterval);
            _MotionStats.SetEventAsync(MotionLib.Motion.MotionStructEventTypeEnum.Trigger);
        }
    }
    public  async Task TriggerEventAsync()
    {
        if(null != _MotionStats)
            await _MotionStats.SetEventAsync(MotionLib.Motion.MotionStructEventTypeEnum.Trigger);
    }
#endif

    public void TriggerEvent()
    {
#if UNITY_WSA_10_0
        TriggerEventAsync();
#endif
    }


    System.Threading.Timer _statsTimer;

    //*********************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// 
    //*********************************************************************

#if UNITY_WSA_10_0
    public async Task StartCollectingStats(MessageLib.MessageRecordReceivedDelegate statsReadyCallback)
    {
        var motionStatsConfig = new MotionLib.MotionStatsConfig()
        {
            PreTriggerSeconds = 1,
            PostTriggerSeconds = 0,
            MessageRatePerSecond = 10 * 10
        };

        if (null == _MotionStats)
            _MotionStats = new MotionLib.MotionStats(motionStatsConfig);

        await _MotionStats.StartCollecting(statsReadyCallback);
        _isCollectingStats = true;

        //int secondsInterval = 2;
        //_statsTimer = new System.Threading.Timer(StatsTimer, null, 0, secondsInterval * 1000);

        //StatsTimer();
    }
#else
    public void StartCollectingStats(MessageLib.MessageRecordReceivedDelegate statsReadyCallback)
    {
    }
#endif

    //*********************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ba"></param>
    /// <param name="start"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    /// 
    //*********************************************************************

    string ByteArrayToString(Byte[] ba, int start, int length)
    {
        var hex = BitConverter.ToString(ba, start, length);
        return hex.Replace("-", "");
    }

    void print(string text)
    {

    }
}



 
