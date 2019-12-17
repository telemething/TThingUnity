/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Repeater : MonoBehaviour
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
using System.Diagnostics;
//using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
//using MavLink4Net.Messages.Common;
using Newtonsoft.Json;
using RosSharp.RosBridgeClient.Messages.Test;
using TThingComLib.Messages;

//*** TODO * We define this here to get DialectThingTelem class to build on 
//*** HoloLens, but it is already defined in TTMobilClient solution, so will
//*** conflict if copied to that solution
namespace RosSharp.RosBridgeClient.Messages.Test
{
    public class MissionStatus : Message
    {
        [JsonIgnore]
        public const string RosMessageName = "tt_mavros_wp_mission/MissionStatus";
        public double x_lat;
        public double y_long;
        public double z_alt;
        public string landed_state;

        public MissionStatus()
        {
        }

        public override string ToString()
        {
            return $"landed_state:{landed_state}, x_lat:{x_lat}, y_long:{y_long}, z_alt:{z_alt}";
        }
    }
}

namespace TThingComLib.Messages
{

public class TThingTelemMessage
{ }

public class StartMission : TThingTelemMessage
{ }

}

namespace TThingComLib
{

//*** TODO * We can complicate it later, but for now just send ThingTelem on UDP

//*************************************************************************
/// <summary>
/// 
/// </summary>
//*************************************************************************
public class TThingCom
{
    private RosClientLib.Repeater _telemetryRepeater = new RosClientLib.Repeater();
    string _udpBroadcaseIP = "192.168.1.255"; //*** TODO * Make this a config item
    int _thingTelemPort = 45679; //*** TODO * Make this a config item
    int _minimumTimeSpanMs = 1; //*** TODO * Make this a config item

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*********************************************************************
    public TThingCom()
    { }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    //*********************************************************************
    public async Task<bool> Connect()
    {
        return await StartRepeater();
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    //*********************************************************************
    public async Task<bool> StartRepeater()
    {
        try
        {
            _telemetryRepeater.AddTransport(
                RosClientLib.Repeater.TransportEnum.UDP,
                RosClientLib.Repeater.DialectEnum.ThingTelem, 
                _udpBroadcaseIP, _thingTelemPort, _minimumTimeSpanMs);
        }
        catch (Exception)
        {
            //logger.DebugLogError("Fatal Error on permissions: " + ex.Message);
            //await App.Current.MainPage.DisplayAlert(
            //    "Error", "Error: " + ex.Message, "Ok");
            return false;
        }

        return true;
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    //*********************************************************************
    public void Send(TThingComLib.Messages.TThingTelemMessage message)
    {
        _telemetryRepeater.Send(message);
    }


}
}

namespace RosClientLib
{
    #region Repeater

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public class Repeater
    {
        private List<Transport> _TransportList = new List<Transport>(2);

        public enum TransportEnum { Uninit, UDP, TCP }
        public enum DialectEnum { Uninit, Mavlink, ThingTelem }

        public Repeater()
        { }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="transport"></param>
        /// <param name="dialect"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        //*********************************************************************

        public void AddTransport(TransportEnum transport,
            DialectEnum dialect, string address, int port, int minimumTimeSpanMs)
        {
            Transport newTransport;
            Dialect newDialect;

            switch (dialect)
            {
                case DialectEnum.ThingTelem:
                    newDialect = new DialectThingTelem();
                    break;
                default:
                    throw new NotImplementedException("Dialect type not implemented");
            }

            switch (transport)
            {
                case TransportEnum.UDP:
                    newTransport = new TransportUdp(address, port, newDialect, minimumTimeSpanMs);
                    _TransportList.Add(newTransport);
                    break;
                default:
                    throw new NotImplementedException("Transport type not implemented");
            }
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        //*********************************************************************

        public async Task<bool> Send(RosSharp.RosBridgeClient.Message message)
        {
            foreach (var tp in _TransportList)
            {
                await tp.Send(message);
            }

            return true;
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        //*********************************************************************

        public async Task<bool> Send(TThingComLib.Messages.TThingTelemMessage message)
        {
            foreach (var tp in _TransportList)
            {
                await tp.Send(message);
            }

            return true;
        }
    }

    #endregion

    #region Dialect

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    abstract class Dialect
    {
        protected Dialect()
        { }

        protected string GetSenderId()
        {
            return "aDrone"; //*** TODO * Obviously this needs to be changed
        }

        public abstract string Translate(RosSharp.RosBridgeClient.Message message);
        public abstract string Translate(TThingTelemMessage message);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    class DialectThingTelem : Dialect
    {
        public DialectThingTelem()
        { }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        //*********************************************************************

        public override string Translate(RosSharp.RosBridgeClient.Message message)
        {
            string outMessage = "";

            switch (message)
            {
                case MissionStatus ms:
                    //string poseFormat =
                    //    "{{\"type\": \"pose\",\"id\" : \"{0}\", \"tow\" : {1}, \"coord\" : {{ \"lat\": {2}, \"lon\" : {3}, \"alt\" : {4} }}, \"orient\" : {{ \"mag\": {5}, \"true\" : {6}, \"x\" : {7}, \"y\" : {8}, \"z\" : {9}, \"w\" : {10} }}, \"gimbal0\" : {{ \"x\" : {11}, \"y\" : {12}, \"z\" : {13}, \"w\" : {14} }}}}";
                    string poseFormat =
                        "{{\"type\": \"pose\",\"id\" : \"{0}\", \"tow\" : {1}, \"coord\" : {{ \"lat\": {2}, \"lon\" : {3}, \"alt\" : {4} }}}}";
                    outMessage = string.Format(poseFormat, GetSenderId(), DateTime.UtcNow.Second,
                        ms.x_lat, ms.y_long, ms.z_alt);
                    break;
                default:
                    throw new NotImplementedException("Message type translation not implemented");
            }

            return outMessage;
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        //*********************************************************************

        public override string Translate(TThingComLib.Messages.TThingTelemMessage message)
        {
            string outMessage = "";

            switch (message)
            {
                case TThingComLib.Messages.StartMission ms:
                    string commandFormat =
                        "{{\"type\": \"command\",\"from\" : \"{0}\", \"to\" : {1}, \"command\" : {{ \"id\": {2} }}}}";
                    outMessage = string.Format(commandFormat, "TheHololens", "TheDrone", "StartMission");
                    break;
                default:
                    throw new NotImplementedException("Message type translation not implemented");
            }

            return outMessage;
        }
    }

    #endregion

    #region Transport

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    abstract class Transport
    {
        protected string _destIP = ""; //***
        protected int _destPort = 0;
        protected int _MinimumTimeSpanMs = 1;
        protected Dialect _dialect;
        protected Transport _handlerTransport;

        private Stopwatch _stopwatch = new Stopwatch();

        protected Transport()
        {
            _stopwatch.Start();
        }

        //public abstract Task<bool> Send(RosSharp.RosBridgeClient.Message message);

        public abstract Task<bool> Send(Byte[] message);

        public async Task<bool> Send(RosSharp.RosBridgeClient.Message message)
        {
            if (_stopwatch.ElapsedMilliseconds > _MinimumTimeSpanMs)
            {
                _stopwatch.Restart();
                _handlerTransport.Send(Encoding.ASCII.GetBytes(_dialect.Translate(message)));
            }

            return true;
        }

        public async Task<bool> Send(TThingTelemMessage message)
        {
            if (_stopwatch.ElapsedMilliseconds > _MinimumTimeSpanMs)
            {
                _stopwatch.Restart();
                _handlerTransport.Send(Encoding.ASCII.GetBytes(_dialect.Translate(message)));
            }

            return true;
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    class TransportUdp : Transport
    {
        private System.Net.Sockets.Socket sock;
        private System.Net.IPAddress ipaddr;
        private System.Net.IPEndPoint endpoint;

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="dialect"></param>
        //*********************************************************************

        public TransportUdp(string address, int port, Dialect dialect, int minimumTimeSpanMs)
        {
            _handlerTransport = this;
            _destIP = address;
            _destPort = port;
            _dialect = dialect;
            _MinimumTimeSpanMs = minimumTimeSpanMs;

            sock = new System.Net.Sockets.Socket(
                System.Net.Sockets.AddressFamily.InterNetwork,
                System.Net.Sockets.SocketType.Dgram,
                System.Net.Sockets.ProtocolType.Udp);

            ipaddr = System.Net.IPAddress.Parse(_destIP);
            endpoint = new System.Net.IPEndPoint(ipaddr, _destPort);
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        //*********************************************************************

        /*public override async Task<bool> Send(RosSharp.RosBridgeClient.Message message)
        {
            sock.SendTo(Encoding.ASCII.GetBytes(_dialect.Translate(message)), endpoint);
            return true;
        }*/

        public override async Task<bool> Send(Byte[] message)
        {
            sock.SendTo(message, endpoint);
            return true;
        }
    }

    #endregion
}

