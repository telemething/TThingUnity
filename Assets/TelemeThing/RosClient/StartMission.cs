using System.Collections;
using System.Collections.Generic;
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

    public class StartMission : IRosOp
    {
        const string _RosServiceName = "/tt_mavros_wp_mission/StartMission_service";
        private RosClient _rosClient;

        #region Rosbridge message types

        /// <summary>
        /// Used to send a request to the /tt_mavros_wp_mission/StartMission_service
        /// </summary>
        class StartMissionPushReq : RosSharp.RosBridgeClient.Message
        {
            public string arg1;

            public StartMissionPushReq()
            {
            }
        }

        /// <summary>
        /// Used to send a receive a response from the /tt_mavros_wp_mission/StartMission_service
        /// </summary>
        class StartMissionReqRespInt : RosSharp.RosBridgeClient.Message
        {
            public string result;

            public StartMissionReqRespInt()
            {
            }

            public override string ToString()
            {
                return $"result: {result}";
            }
        }

        /// <summary>
        /// Used by callers of the callback version of StartMission()
        /// </summary>
        public class StartMissionReqResp : IRosMessage
        {
            public bool success;

            public StartMissionReqResp()
            {
            }
        }

        #endregion

        //*********************************************************************
        //*
        //*
        //*
        //*********************************************************************

        public StartMission()
        {
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

        async Task<object> CallService(IRosClient rosClient)
        {
            object response = null;
            var tcs = new TaskCompletionSource<bool>();

            try
            {
                var wpr = new StartMissionPushReq() { arg1 = "Hi" };

                var serviceId2 = rosClient.CallService<
                    StartMissionPushReq, StartMissionReqRespInt>(
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

        void CallService<TOut>(IRosClient rosClient,
            RosClient.ServiceCallback<TOut> callback) where TOut : RosClientLib.IRosMessage
        {
            try
            {
                var wpr = new StartMissionPushReq() { arg1 = "Hi" };

                var serviceId2 = rosClient.CallService<
                    StartMissionPushReq, StartMissionReqRespInt>(
                    _RosServiceName, (resp) =>
                    {
                        var respOut = new StartMissionReqResp();
                        if (null != resp)
                        {
                            respOut.success = true;    //TODO * This is just for testing
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
            return await CallService(rosClient);
        }

        //*********************************************************************
        //*
        //* From Interface
        //*
        //*********************************************************************

        void IRosOp.CallService<TOut>(IRosClient rosClient,
            RosClient.ServiceCallback<TOut> callback)
        {
            CallService(rosClient, callback);
        }

        #region Tests


        #endregion
    }
}


