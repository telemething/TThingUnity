using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using RosSharp.RosBridgeClient.Messages.Test;

//*****************************************************************************
//* The reference to Newtonsoft.JSON here requires special handling in order
//* to function correctly when building for Unity IL2CPP. Using Nuget to import
//* Newtonsoft.JSON does NOT work, it will fail at runtime due to reflection.
//* 1) In Unity, import the 'JSON.NET for Unity' asset.
//* 2) If the solution refers to other projects which themselves refer to
//*    Newtonsoft.JSON, then they must be also refer to 'JSON.NET for Unity'.
//* 3) To do that, simply refer them to refer to:
//*    "..\Assets\JsonDotNet\Assemblies\Windows\Newtonsoft.Json.dll"
//*****************************************************************************

//*** TODO * We define this here to get DialectThingTelem class to build on 
//*** HoloLens, but it is already defined in TTMobilClient solution, so will
//*** conflict if copied to that solution
/*namespace RosSharp.RosBridgeClient
{
    public class Message
    {
        public virtual string ToString()
        {
            return "";
        }
    }
}*/
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
    public enum MessageTypeEnum
    { Telem, Command, Config }
    public enum NetworkTypeEnum
    { Unknown, TCP, UDP, WsAPI }
    public enum ServiceTypeEnum
    { Unknown, SelfTelem, GroundStation, Config, GeoTile }
    public enum ServiceRoleEnum
    { Unknown, Client, Server, Both }
    public enum CommandIdEnum
    { StartMission, StopDrone, ReturnHome, LandDrone }
    public enum MissionStateEnum
    { Unknown, None, Sending, Sent, Starting, Underway, Failed, Completed }
    public enum LandedStateEnum
    { Unknown, Grounded, Liftoff, Flying, Landing, Failed }

    public class NetworkService
    {
        public string URL { get; set; }
        public NetworkTypeEnum NetworkType { get; set; }
        public ServiceTypeEnum ServiceType { get; set; }
        public ServiceRoleEnum ServiceRole { get; set; }

        public NetworkService(string url, NetworkTypeEnum networkType,
            ServiceTypeEnum serviceType, ServiceRoleEnum serviceRole)
        {
            URL = url;
            NetworkType = networkType;
            ServiceType = serviceType;
            ServiceRole = serviceRole;
        }
    }

    public class Coord
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double Alt { get; set; }

        public Coord() { }

        public Coord(double lat, double lon, double alt)
        {
            Lat = lat;
            Lon = lon;
            Alt = alt;
        }
    }

    public class Orient
    {
        public double Mag { get; set; }
        public double True { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double W { get; set; }

        public Orient() { }

        public Orient(double mag, double True, double x, double y, double z, double w)
        {
            Mag = mag;
            this.True = True;
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }

    public class Gimbal
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double W { get; set; }

        public Gimbal() { }

        public Gimbal(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }

    public class Argument
    {
        public string Name { get; set; }
        public List<string> Values { get; set; }
    }

    public class Command
    {
        public TThingComLib.Messages.CommandIdEnum CommandId { get; set; }

        public List<TThingComLib.Messages.Argument> Arguments { get; set; }

        public Command() { }

        public Command(TThingComLib.Messages.CommandIdEnum commandId)
        {
            this.CommandId = commandId;
        }

        public Command(TThingComLib.Messages.CommandIdEnum commandId, List<TThingComLib.Messages.Argument> args)
        {
            this.CommandId = commandId;
            this.Arguments = args;
        }
    }

    public class Message
    {
        public TThingComLib.Messages.MessageTypeEnum Type { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Time { get; set; }
        public TThingComLib.Messages.Coord Coord { get; set; }
        public TThingComLib.Messages.Orient Orient { get; set; }
        public TThingComLib.Messages.Gimbal Gimbal { get; set; }
        public TThingComLib.Messages.MissionStateEnum MissionState { get; set; }
        public TThingComLib.Messages.LandedStateEnum LandedState { get; set; }
        public List<TThingComLib.Messages.Command> Commands { get; set; }
        public List<TThingComLib.Messages.NetworkService> NetworkServices { get; set; }

        public Message()
        { }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="time"></param>
        //*********************************************************************
        public Message(TThingComLib.Messages.MessageTypeEnum messageType,
            string from, string to, string time = null)
        {
            if (null == time)
                time = DateTime.UtcNow.ToBinary().ToString();

            this.Type = messageType;
            this.From = from;
            this.To = to;
            this.Time = time;
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="commandId"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        //*********************************************************************
        public static TThingComLib.Messages.Message CreateCommand(string from,
            string to, TThingComLib.Messages.CommandIdEnum commandId, string time = null)
        {
            var output = new TThingComLib.Messages.Message(
                TThingComLib.Messages.MessageTypeEnum.Command, from, to, time);
            output.Add(new TThingComLib.Messages.Command(commandId));
            return output;
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="coord"></param>
        /// <param name="orient"></param>
        /// <param name="gimbal"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        //*********************************************************************
        public static TThingComLib.Messages.Message CreateTelem(string from,
            string to, TThingComLib.Messages.Coord coord,
            TThingComLib.Messages.Orient orient,
            TThingComLib.Messages.Gimbal gimbal, string time = null)
        {
            var output = new TThingComLib.Messages.Message(
                TThingComLib.Messages.MessageTypeEnum.Telem, from, to, time)
            {
                Coord = coord,
                Orient = orient,
                Gimbal = gimbal
            };
            return output;
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="commandId"></param>
        /// <param name="args"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        //*********************************************************************
        public static TThingComLib.Messages.Message CreateCommand(string from,
            string to, TThingComLib.Messages.CommandIdEnum commandId,
            List<TThingComLib.Messages.Argument> args, string time = null)
        {
            var output = new TThingComLib.Messages.Message(
                TThingComLib.Messages.MessageTypeEnum.Command, from, to, time);
            output.Add(new TThingComLib.Messages.Command(commandId));
            return output;
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        //*********************************************************************
        public void Add(TThingComLib.Messages.Command command)
        {
            if (null == Commands) Commands = new List<Command>();
            Commands.Add(command);
        }

        //*********************************************************************
        /// <summary>
        /// https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to
        /// </summary>
        /// <returns></returns>
        //*********************************************************************
        public string Serialize()
        {
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            //settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            settings.Converters.Add(new StringEnumConverter(true));
            return JsonConvert.SerializeObject(this, settings);
        }

        //*********************************************************************
        /// <summary>
        /// https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        //*********************************************************************
        public static TThingComLib.Messages.Message DeSerialize(string message)
        {
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore };
            //settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            settings.Converters.Add(new StringEnumConverter(true));
            //return JsonConvert.DeserializeObject<TThingComLib.Messages.Message>(message, settings);

            TThingComLib.Messages.Message ret = null;

            try
            {
                ret = JsonConvert.DeserializeObject<TThingComLib.Messages.Message>(message, settings);
            }
            catch(Exception ex)
            {
                T1.CLogger.LogThis("DeSerialize() " + message);
            }

            return ret;
        }
    }

    public class StartMission : Message
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
        private TThingComLib.Repeater _telemetryRepeater = new TThingComLib.Repeater();
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
                    TThingComLib.Repeater.TransportEnum.UDP,
                    TThingComLib.Repeater.DialectEnum.ThingTelem,
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
        public void Send(TThingComLib.Messages.Message message)
        {
            _telemetryRepeater.Send(message);
        }


    }
    //}

    //namespace RosClientLib
    //{
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

        public async Task<bool> Send(TThingComLib.Messages.Message message)
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
        public abstract string Translate(TThingComLib.Messages.Message message);
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

        public override string Translate(TThingComLib.Messages.Message message)
        {
            string outMessage = "";

            outMessage = message.Serialize();

            /*switch (message)
            {
                case TThingComLib.Messages.StartMission ms:
                    string commandFormat =
                        "{{\"type\": \"command\",\"from\" : \"{0}\", \"to\" : \"{1}\", \"commands\" : [{{ \"commandId\": \"{2}\" }}]}}";
                    outMessage = string.Format(commandFormat, "TheHololens", "TheDrone", "StartMission");
                    break;
                default:
                    throw new NotImplementedException("Message type translation not implemented");
            }*/

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

        public async Task<bool> Send(TThingComLib.Messages.Message message)
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
        /// <param name="minimumTimeSpanMs"></param>
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
                System.Net.Sockets.ProtocolType.Udp)
            { ExclusiveAddressUse = false, EnableBroadcast = true };

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
            var bytesSent = sock.SendTo(message, endpoint);
            return true;
        }
    }

    #endregion
}


