using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using RosSharp.RosBridgeClient;
using UnityEngine;

//*****************************************************************************
//*
//*
//*
//*****************************************************************************

namespace RosClientLib
{
    using System.Threading;
    using System.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    //using Flitesys.GeographicLib;
    //using RosbridgeNet.RosbridgeClient.Common.Attributes;

    public class Waypoints : IRosOp
    {
        //const string _RosServiceName = "/tt_mission_master/WaypointPush_service";
        const string _RosServiceName = "/tt_mavros_wp_mission/WaypointPush_service";
        private RosClient _rosClient;
        private List<Waypoint> _waypoints;

        //*** NEW *** Commented out
        //private readonly Geodesic _geo = Geodesic.WGS84;

        private Waypoint _lastWaypoint;
        private ushort _waypointIndex = 0;

        #region Rosbridge message types

        //[RosMessageType("tt_master/Waypoint")]

        //*** NEW *** Commented out
        //[RosMessageType("tt_mavros_wp_mission/Waypoint")]

        //*** NEW *** added base class
        public class Waypoint : RosSharp.RosBridgeClient.Message
        {
            //*** NEW *** added 2 lines
            [JsonIgnore]
            public const string RosMessageName = "tt_mavros_wp_mission/Waypoint";

            public Waypoint()
            {
            }

            public byte frame;
            public ushort command;
            public bool is_current;
            public bool autocontinue;
            public float param1;
            public float param2;
            public float param3;
            public float param4;
            public double z_alt;
            public double x_lat;
            public double y_long;

            public override string ToString()
            {
                return $"z_alt: {z_alt}, x_lat: {x_lat}, y_long: {y_long}";
            }
        }

        /// <summary>
        /// Used to send a request to the tt_mavros_wp_mission/Waypoint service
        /// </summary>
        class WaypointPushReq : RosSharp.RosBridgeClient.Message
        {
            public ushort start_index;
            public Waypoint[] waypoints;

            public WaypointPushReq()
            {
            }
        }

        /// <summary>
        /// Used to send a receive a response from the tt_mavros_wp_mission/Waypoint service
        /// </summary>
        class WaypointReqRespInt : RosSharp.RosBridgeClient.Message
        {
            public bool success;
            public uint wp_transfered;

            public WaypointReqRespInt()
            {
            }

            public override string ToString()
            {
                return $"success: {success}, transfered: {wp_transfered}";
            }
        }

        /// <summary>
        /// Used by callers of the callback version of PushWaypoints()
        /// </summary>
        public class WaypointReqResp : IRosMessage
        {
            public bool success;
            public uint wp_transfered;

            public WaypointReqResp()
            {
            }
        }

        #endregion

        //*********************************************************************
        //*
        //*
        //*
        //*********************************************************************

        public Waypoints()
        {
            StartNewMission();
        }

        //*********************************************************************
        //*
        //*
        //*
        //*********************************************************************

        public Waypoints(RosClient rosClient)
        {
            _rosClient = rosClient;
            StartNewMission();
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="waypoints"></param>
        /// <param name="rosClient"></param>
        /// <returns></returns>
        ///
        //*********************************************************************

        async Task<object> PushWaypointsManualEvent(List<Waypoint> waypoints, IRosClient rosClient)
        {
            object response = null;
            var doneSignal = new ManualResetEvent(false);

            try
            {
                var wpr = new WaypointPushReq() { start_index = 0, waypoints = waypoints.ToArray() };

                var serviceId2 = rosClient.CallService<WaypointPushReq, WaypointReqRespInt>(
                    _RosServiceName, (resp) => {
                        response = resp;
                        doneSignal.Set();
                    }, wpr);

                doneSignal.WaitOne(10000);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return response;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="waypoints"></param>
        /// <param name="rosClient"></param>
        /// <returns></returns>
        ///
        //*********************************************************************

        async Task<object> PushWaypoints(List<Waypoint> waypoints, IRosClient rosClient)
        {
            object response = null;
            var tcs = new TaskCompletionSource<bool>();

            try
            {
                var wpr = new WaypointPushReq() { start_index = 0, waypoints = waypoints.ToArray() };

                var serviceId2 = rosClient.CallService<WaypointPushReq, WaypointReqRespInt>(
                    _RosServiceName, (resp) => {
                        response = resp;
                        tcs?.TrySetResult(true);
                    }, wpr);

                await tcs.Task;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return response;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="waypoints"></param>
        /// <param name="rosClient"></param>
        /// <param name="callback"></param>
        ///
        //*********************************************************************

        void PushWaypoints<TOut>(List<Waypoint> waypoints, IRosClient rosClient,
            RosClient.ServiceCallback<TOut> callback) where TOut : RosClientLib.IRosMessage
        {
            try
            {
                var wpr = new WaypointPushReq() { start_index = 0, waypoints = waypoints.ToArray() };

                var serviceId2 = rosClient.CallService<WaypointPushReq, WaypointReqRespInt>(
                    _RosServiceName, (resp) =>
                    {
                        var respOut = new WaypointReqResp();
                        if (null != resp)
                        {
                            respOut.success = resp.success;
                            respOut.wp_transfered = resp.wp_transfered;
                        }

                        callback.Invoke(respOut as TOut);
                    }, wpr);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        //*********************************************************************
        //*
        //* From Interface
        //*
        //*********************************************************************

        async Task<object> IRosOp.CallServiceAsync(IRosClient rosClient)
        {
            return await PushWaypoints(_waypoints, rosClient);
        }

        //*********************************************************************
        //*
        //* From Interface
        //*
        //*********************************************************************

        void IRosOp.CallService<TOut>(IRosClient rosClient,
            RosClient.ServiceCallback<TOut> callback)
        {
            PushWaypoints(_waypoints, rosClient, callback);
        }

        //*********************************************************************
        //*
        //*
        //*
        //*********************************************************************

        public void AddWaypoint(Waypoint waypoint)
        {
            _lastWaypoint = waypoint;
            _waypoints.Add(waypoint);
        }

        //*********************************************************************
        //*
        //*
        //*
        //*********************************************************************

        public void AddWaypoint(Mavros.Command command, double lat,
            double lon, double alt,
            bool isCurrent, bool autoContinue,
            float param1, float param2, float param3, float param4)
        {
            double currentAlt;

            // if lat & long = 0, then we fetch the current position from the unit
            if (0.0d == lat && 0.0d == lon)
                (lat, lon, currentAlt) = FetchCurrentPosition();

            AddWaypoint(new Waypoint()
            {
                autocontinue = autoContinue,
                command = (ushort)command,
                frame = (byte)Mavros.NavFrame.FRAME_GLOBAL_REL_ALT,
                is_current = isCurrent,
                param1 = param1,
                param2 = param2,
                param3 = param3,
                param4 = param4,
                x_lat = lat,
                y_long = lon,
                z_alt = alt
            });
        }

        //*********************************************************************
        //*
        //*
        //*
        //*********************************************************************

        //*** NEW * Commented out, might need to find replacement for _geo
        /*public void AddWaypoint(Mavros.Command command, Int32 direction,
            double distance, double alt, bool isCurrent, bool autoContinue,
            float param1, float param2, float param3, float param4)
        {

            // we can't move relative if we haven't taken off
            if (null == _lastWaypoint)
            {
                throw new Exception("_lastWaypoint == NULL");
            }

            // calculate new lat & long
            var coords = _geo.Direct(
                _lastWaypoint.x_lat, _lastWaypoint.y_long, direction, distance);

            AddWaypoint(command, coords.Latitude2, coords.Longitude2,
                alt, isCurrent, autoContinue, param1, param2, param3, param4);
        }*/

        //*********************************************************************
        //*
        //*
        //*
        //*********************************************************************

        public (double lat, double lon, double alt) FetchCurrentPosition()
        {
            double lat = 47.468502;
            double lon = -121.7674;
            double alt = 0;

            return (lat, lon, alt);
        }

        //*********************************************************************
        //*
        //*
        //*
        //*********************************************************************

        public void StartNewMission()
        {
            _lastWaypoint = null;
            _waypointIndex = 0;

            if (null != _waypoints)
                _waypoints.Clear();
            else
                _waypoints = new List<Waypoint>(4);
        }

        #region Tests

        //*********************************************************************
        //*
        //*
        //*
        //*********************************************************************

        public void CreateTestWaypoints()
        {
            StartNewMission();
            AddWaypoint(Mavros.Command.NAV_TAKEOFF, 0.0, 0.0, 10, true, true, 5, 0, 0, 0);
            AddWaypoint(Mavros.Command.NAV_WAYPOINT, 90, 1, 10, false, true, 5, 0, 0, 0);
            AddWaypoint(Mavros.Command.NAV_WAYPOINT, 180, 1, 10, false, true, 5, 0, 0, 0);
            AddWaypoint(Mavros.Command.NAV_LAND, 270, 1, 10, false, true, 5, 0, 0, 0);
        }

        //*********************************************************************
        //*
        //*
        //*
        //*********************************************************************

        //*** NEW *** Commented out
        /*public void CreateTestWaypointsy()
        {
            GeodesicData coords = new GeodesicData() { Latitude2 = 47.468502, Longitude2 = -121.7674 };

            var geo = Geodesic.WGS84;

            var waypoints = new List<Waypoint>(4);

            waypoints.Add(new Waypoint()
            {
                autocontinue = true,
                command = 22,
                frame = 3,
                is_current = true,
                param1 = 5,
                param2 = 0,
                param3 = 0,
                param4 = 0,
                x_lat = (float)coords.Latitude2,
                y_long = (float)coords.Longitude2,
                z_alt = 10
            });

            coords = _geo.Direct(coords.Latitude2, coords.Longitude2, 90, 1.0);

            waypoints.Add(new Waypoint()
            {
                autocontinue = true,
                command = 16,
                frame = 3,
                is_current = true,
                param1 = 5,
                param2 = 0,
                param3 = 0,
                param4 = 0,
                x_lat = (float)coords.Latitude2,
                y_long = (float)coords.Longitude2,
                z_alt = 10
            });

            coords = geo.Direct(coords.Latitude2, coords.Longitude2, 180, 1.0);

            waypoints.Add(new Waypoint()
            {
                autocontinue = true,
                command = 16,
                frame = 3,
                is_current = true,
                param1 = 5,
                param2 = 0,
                param3 = 0,
                param4 = 0,
                x_lat = (float)coords.Latitude2,
                y_long = (float)coords.Longitude2,
                z_alt = 10
            });

            coords = geo.Direct(coords.Latitude2, coords.Longitude2, 270, 1.0);

            waypoints.Add(new Waypoint()
            {
                autocontinue = true,
                command = 16,
                frame = 3,
                is_current = true,
                param1 = 5,
                param2 = 0,
                param3 = 0,
                param4 = 0,
                x_lat = (float)coords.Latitude2,
                y_long = (float)coords.Longitude2,
                z_alt = 10
            });

            _waypoints = waypoints;
        }*/

        #endregion
    }
}


