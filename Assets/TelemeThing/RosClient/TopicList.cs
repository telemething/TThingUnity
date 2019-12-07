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

    public class TopicList : IRosOp
    {
        const string _RosServiceName = "/rosapi/topics";
        private RosClient _rosClient;

        #region Rosbridge message types

        /// <summary>
        /// Used to send a request to the /rosapi/topics_service
        /// </summary>
        class TopicListReq : RosSharp.RosBridgeClient.Message
        {
            public TopicListReq()
            {
            }
        }

        /// <summary>
        /// Used to send a receive a response from the /rosapi/topics_service
        /// </summary>
        class TopicListReqRespInt : RosSharp.RosBridgeClient.Message
        {
            public string[] topics;
            public string[] types;

            public TopicListReqRespInt()
            {
            }

            public override string ToString()
            {
                //return $"result: {result}";
                return $"result: ---";
            }
        }

        /// <summary>
        /// Used by callers of the callback version of TopicList()
        /// </summary>
        public class RosTopic
        {
            public string topic { get; set; }
            public string type { get; set; }
        }

        /// <summary>
        /// Used by callers of the callback version of TopicList()
        /// </summary>
        public class TopicListReqResp : IRosMessage
        {
            public bool success;
            public List<RosTopic> rosTopics = new List<RosTopic>(100);

            public TopicListReqResp()
            {
            }
        }

        #endregion

        //*********************************************************************
        //*
        //*
        //*
        //*********************************************************************

        public TopicList()
        {
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
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
                var wpr = new TopicListReq();

                var serviceId2 = rosClient.CallService<
                    TopicListReq, TopicListReqRespInt>(
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
        /// <param name="rosClient"></param>
        /// <param name="callback"></param>
        ///
        //*********************************************************************

        void CallService<TOut>(IRosClient rosClient,
            RosClient.ServiceCallback<TOut> callback) where TOut : RosClientLib.IRosMessage
        {
            try
            {
                var wpr = new TopicListReq();

                var serviceId2 = rosClient.CallService<
                    TopicListReq, TopicListReqRespInt>(
                    _RosServiceName, (resp) =>
                    {
                        var respOut = new TopicListReqResp();
                        if (null != resp)
                        {
                            if (resp.topics.Length != resp.types.Length)
                            {
                                respOut.success = false;
                            }
                            else
                            {
                                respOut.success = true;

                                for (var index = 0; index < resp.topics.Length; index++)
                                    respOut.rosTopics.Add(new RosTopic()
                                    { topic = resp.topics[index], type = resp.types[index] });
                            }
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

