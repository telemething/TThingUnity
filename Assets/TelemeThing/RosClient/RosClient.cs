using System.Threading.Tasks;
using System;
using System.Linq;

//*****************************************************************************
//
// <summary>
//  
// </summary>
// 
//*****************************************************************************

namespace RosClientLib
{
    using RosSharp.RosBridgeClient;
    //using std_msgs = RosSharp.RosBridgeClient.Messages.Standard;
    //using std_srvs = RosSharp.RosBridgeClient.Services.Standard;
    //using rosapi = RosSharp.RosBridgeClient.Services.RosApi;

    using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;
    //using std_srvs = RosSharp.RosBridgeClient.Services.Standard;
    //using rosapi = RosSharp.RosBridgeClient.Services.RosApi;

    //*** NEW ***
    // this is supposed to be defined in WebSocketNetProtocol.cs
    public class RbConnectionEventArgs : EventArgs
    {
        public RbConnectionEventArgs(Exception exception, string message)
        {
            this.exception = exception;
            this.message = message;
        }

        public Exception exception { get; set; }
        public string message { get; set; }
    }

    public class ConnectionEventArgs : EventArgs
    {
        public ConnectionEventArgs(Exception exception, string message)
        {
            this.exception = exception;
            this.message = message;
        }

        public ConnectionEventArgs(RbConnectionEventArgs rbArgs)
        {
            if (null == rbArgs)
                return;

            this.exception = rbArgs.exception;
            this.message = rbArgs.message;
        }

        public Exception exception { get; set; }
        public string message { get; set; }
    }

    public interface IRosClient
    {
        void Connect(string uri, EventHandler onConnected,
            EventHandler onConnectionFailed);

        Task DisConnect();

        string Subscribe<T>(string topic, SubscriptionHandler<T> subscriptionHandler,
            int throttleRate = 0, int queueLength = 1, int fragmentSize = int.MaxValue,
            string compression = "none") where T : Message;

        void Unsubscribe(string id);

        string CallService<Tin, Tout>(string service,
            ServiceResponseHandler<Tout> serviceResponseHandler, Tin serviceArguments)
            where Tin : Message where Tout : Message;

        string Advertise<T>(string topic) where T : Message;

        void Publish(string id, Message message);

        void Unadvertise(string id);

        string AdvertiseService<Tin, Tout>(string service,
            ServiceCallHandler<Tin, Tout> serviceCallHandler)
            where Tin : Message where Tout : Message;

        void UnadvertiseService(string id);

        Task<object> CallServiceAsync(IRosOp rosOp);

        void CallService<TOut>(
            IRosOp rosOp, RosClientLib.RosClient.ServiceCallback<TOut> callback)
            where TOut : IRosMessage;

        void FetchTopicList(string topicTypeName,
            RosClientLib.RosClient.ServiceCallback<TopicList.TopicListReqResp> callback);
    }

    public class RosClient : IRosClient
    {
        public static readonly string TestUri = "ws://192.168.1.30:9090";

        public delegate void ServiceCallback<TOut>(TOut t) where TOut : IRosMessage;
        private RosSocket _rosSocket;
        private string _webSocketUri;
        private RosSharp.RosBridgeClient.Protocols.WebSocketNetProtocol _webSocketProtocol;

        //*********************************************************************
        //*
        //*
        //*
        //*********************************************************************

        public RosClient()
        {
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="onConnected">ConnectionEventArgs</param>
        /// <param name="onConnectionFailed">ConnectionEventArgs</param>
        ///
        //*********************************************************************

        public RosClient(string uri, EventHandler onConnected,
            EventHandler onConnectionFailed)
        {
            Connect(uri, onConnected, onConnectionFailed);
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="onConnected">ConnectionEventArgs</param>
        /// <param name="onConnectionFailed">ConnectionEventArgs</param>
        ///
        //*********************************************************************

        public void Connect(string uri, EventHandler onConnected,
            EventHandler onConnectionFailed)
        {
            try
            {
                _webSocketUri = uri;
                _webSocketProtocol =
                    new RosSharp.RosBridgeClient.Protocols.WebSocketNetProtocol(uri);

                //*** NEW ***
                /*_rosSocket = new RosSocket(_webSocketProtocol,
                    (sender, args) => onConnected?.Invoke(
                        sender, new ConnectionEventArgs(args as RbConnectionEventArgs)),
                    (sender, args) => onConnectionFailed?.Invoke(
                        sender, new ConnectionEventArgs(args as RbConnectionEventArgs)),
                    RosSocket.SerializerEnum.JSON);*/

                _rosSocket = new RosSocket(_webSocketProtocol, RosSocket.SerializerEnum.JSON);
            }
            catch (Exception e)
            {
                throw new Exception($"Connect({uri}) failed : {e.Message}");
            }
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        ///
        //*********************************************************************

        public async Task DisConnect()
        {
            // We need to figure out what to do here
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic"></param>
        /// <param name="subscriptionHandler"></param>
        /// <param name="throttleRate"></param>
        /// <param name="queueLength"></param>
        /// <param name="fragmentSize"></param>
        /// <param name="compression"></param>
        /// <returns></returns>
        ///
        //*********************************************************************
        string IRosClient.Subscribe<T>(string topic,
            SubscriptionHandler<T> subscriptionHandler, int throttleRate,
            int queueLength, int fragmentSize, string compression)
        {
            return _rosSocket.Subscribe<T>(topic, subscriptionHandler,
                throttleRate, queueLength, fragmentSize, compression);
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        ///
        //*********************************************************************
        void IRosClient.Unsubscribe(string id)
        {
            _rosSocket.Unsubscribe(id);
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic"></param>
        /// <returns></returns>
        ///
        //*********************************************************************
        string IRosClient.Advertise<T>(string topic)
        {
            return _rosSocket.Advertise<T>(topic);
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="message"></param>
        ///
        //*********************************************************************
        void IRosClient.Publish(string id, Message message)
        {
            _rosSocket.Publish(id, message);
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        ///
        //*********************************************************************
        void IRosClient.Unadvertise(string id)
        {
            _rosSocket.Unadvertise(id);
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Tin"></typeparam>
        /// <typeparam name="Tout"></typeparam>
        /// <param name="service"></param>
        /// <param name="serviceResponseHandler"></param>
        /// <param name="serviceArguments"></param>
        /// <returns></returns>
        ///
        //*********************************************************************
        public string CallService<Tin, Tout>(string service,
            ServiceResponseHandler<Tout> serviceResponseHandler,
            Tin serviceArguments) where Tin : Message where Tout : Message
        {
            return _rosSocket.CallService<Tin, Tout>(
                service, serviceResponseHandler, serviceArguments);
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Tin"></typeparam>
        /// <typeparam name="Tout"></typeparam>
        /// <param name="service"></param>
        /// <param name="serviceCallHandler"></param>
        /// <returns></returns>
        ///
        //*********************************************************************
        string IRosClient.AdvertiseService<Tin, Tout>(string service,
            ServiceCallHandler<Tin, Tout> serviceCallHandler)
        {
            return _rosSocket.AdvertiseService<Tin, Tout>(service, serviceCallHandler);
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        ///
        //*********************************************************************
        void IRosClient.UnadvertiseService(string id)
        {
            _rosSocket.UnadvertiseService(id);
        }

        //*********************************************************************
        //*
        //* Call service, wait for response
        //*
        //*********************************************************************

        async Task<object> IRosClient.CallServiceAsync(IRosOp rosOp)
        {
            return await rosOp.CallServiceAsync(this);
        }

        //*********************************************************************
        //*
        //* Call service, pass in response handler, return immediately
        //*
        //*********************************************************************

        void IRosClient.CallService<TOut>(
            IRosOp rosOp, ServiceCallback<TOut> callback)
        {
            rosOp.CallService(this, callback);
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        ///
        //*********************************************************************

        async void IRosClient.FetchTopicList(string topicTypeName,
            ServiceCallback<TopicList.TopicListReqResp> callback)
        {
            IRosOp TListService = new TopicList();

            TListService.CallService<TopicList.TopicListReqResp>(this,
                resp =>
                {
                    if (resp.success)
                    {
                        try
                        {
                            var topics = resp.rosTopics.Where(topic => topic.type.Equals(topicTypeName));
                            var topicsList = topics.ToList<TopicList.RosTopic>();
                            topicsList.Sort((x, y) => string.Compare(x.topic, y.topic));
                            callback.Invoke(new TopicList.TopicListReqResp() { rosTopics = topicsList, success = true });
                        }
                        catch (Exception)
                        {
                            callback.Invoke(new TopicList.TopicListReqResp() { success = false });
                        }
                    }
                    else
                    {
                        callback.Invoke(new TopicList.TopicListReqResp() { success = false });
                    }
                });
        }

        #region Samples

        //*********************************************************************
        ///
        /// <summary>
        /// Call a service and wait for results
        /// </summary>
        ///
        //*********************************************************************

        public static void WaypointTest()
        {
            IRosClient rc = new RosClientLib.RosClient(TestUri, null, null);
            var wp = new RosClientLib.Waypoints();
            wp.CreateTestWaypoints();
            var resp = rc.CallServiceAsync(wp).Result;
        }

        //*********************************************************************
        ///
        /// <summary>
        /// Call a service, pass in a callback, return immediately 
        /// </summary>
        ///
        //*********************************************************************

        public static void WaypointTest2()
        {
            IRosClient rc = new RosClientLib.RosClient(TestUri, null, null);
            var wp = new RosClientLib.Waypoints();
            wp.CreateTestWaypoints();
            rc.CallService<Waypoints.WaypointReqResp>(wp,
                (resp) =>
                { Console.WriteLine($"WaypointTest2Callback() : success: {resp.success} transferred: {resp.wp_transfered}"); });

            Console.WriteLine("Press any key to close...");
            Console.ReadKey(true);
        }

        //*********************************************************************
        ///
        /// <summary>
        /// Call a service and wait for results
        /// </summary>
        ///
        //*********************************************************************

        /*public static void GeneralTest()
        {
            try
            {
                var rc = new RosClientLib.RosClient(TestUri,
                    (sender, args) => { },
                    (sender, args) =>
                    {
                        if (args is ConnectionEventArgs rr)
                            Console.WriteLine($"################## Connection Exception: {rr.message}");
                    });
                rc.TestTopicSubscription();
            }
            catch (Exception e)
            {
                Console.WriteLine($"################## Exception: {e.Message}");
                Console.ReadKey(true);
            }
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        ///
        //*********************************************************************

        public void TestTopicPublish()
        {
            std_msgs.String message = new std_msgs.String();
            message.data = "publication test message data";
            string publicationId = _rosSocket.Advertise<std_msgs.String>("publication_test");
            _rosSocket.Publish(publicationId, message);

            Console.WriteLine("Press any key to close...");
            Console.ReadKey(true);

            _rosSocket.Unadvertise(publicationId);
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        ///
        //*********************************************************************

        public void TestTopicSubscription()
        {
            var subscriptionId = _rosSocket.Subscribe
                <RosSharp.RosBridgeClient.Messages.Test.MissionStatus>(
                "/tt_mavros_wp_mission/MissionStatus",
                (message) =>
                { Console.WriteLine((message).ToString()); });

            Console.WriteLine("Press any key to close...");
            Console.ReadKey(true);
            _rosSocket.Unsubscribe(subscriptionId);
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        ///
        //*********************************************************************

        public void TestServiceCall()
        {
            _rosSocket.CallService<
                    RosSharp.RosBridgeClient.Messages.Test.Req,
                    RosSharp.RosBridgeClient.Messages.Test.Resp>
                    ("/tt_mavros_wp_mission/StartMission_service",
                (message) =>
                {
                    Console.WriteLine("response: " + message.result);
                },
                    new RosSharp.RosBridgeClient.Messages.Test.Req("hey"));

            Console.WriteLine("Press any key to close...");
            Console.ReadKey(true);
        }

        //*********************************************************************
        ///
        /// <summary>
        /// 
        /// </summary>
        ///
        //*********************************************************************

        public void TestServiceProvider()
        {
            string serviceId = _rosSocket.AdvertiseService<
                    std_srvs.TriggerRequest, std_srvs.TriggerResponse>
                    ("/service_response_test", (
                        std_srvs.TriggerRequest arguments,
                        out std_srvs.TriggerResponse result) =>
                    {
                        result = new std_srvs.TriggerResponse(true, "service response message");
                        return true;
                    });

            Console.WriteLine("Press any key to close...");
            Console.ReadKey(true);
            _rosSocket.UnadvertiseService(serviceId);
        }*/

        #endregion

        #region Old Code

        /*public static void Test()
        {
            string webSocketUri = "ws://192.168.1.30:9090";

            //RosSocket rosSocket = new RosSocket(new RosBridgeClient.Protocols.WebSocketSharpProtocol(uri));
            RosSocket rosSocket = new RosSocket(new RosSharp.RosBridgeClient.Protocols.WebSocketNetProtocol(webSocketUri));

            // Publication:
            std_msgs.String message = new std_msgs.String();
            message.data = "publication test message data";

            string publicationId = rosSocket.Advertise<std_msgs.String>("publication_test");
            rosSocket.Publish(publicationId, message);


            // Subscription:
            string subscriptionId = rosSocket.Subscribe<RosSharp.RosBridgeClient.Messages.Test.MissionStatus>("/tt_mavros_wp_mission/MissionStatus", SubscriptionHandler<>);

            // Service Call:
            rosSocket.CallService<RosSharp.RosBridgeClient.Messages.Test.Req, RosSharp.RosBridgeClient.Messages.Test.Resp>
                ("/tt_mavros_wp_mission/StartMission_service", ServiceCallHandler, new RosSharp.RosBridgeClient.Messages.Test.Req("hey"));

            // Service Response:
            string serviceId = rosSocket.AdvertiseService<std_srvs.TriggerRequest, std_srvs.TriggerResponse>
               ("/service_response_test", ServiceResponseHandler);

            Console.WriteLine("Press any key to unsubscribe...");
            Console.ReadKey(true);
            //rosSocket.Unadvertise(publicationId);
            rosSocket.Unsubscribe(subscriptionId);
            //rosSocket.UnadvertiseService(serviceId);

            Console.WriteLine("Press any key to close...");
            Console.ReadKey(true);
            rosSocket.Close();
        }
        public static void TestOrig()
        {
            string webSocketUri = "ws://192.168.1.30:9090";

            //RosSocket rosSocket = new RosSocket(new RosBridgeClient.Protocols.WebSocketSharpProtocol(uri));
            RosSocket rosSocket = new RosSocket(new RosSharp.RosBridgeClient.Protocols.WebSocketNetProtocol(webSocketUri));

            // Publication:
            std_msgs.String message = new std_msgs.String();
            message.data = "publication test message data";

            string publicationId = rosSocket.Advertise<std_msgs.String>("publication_test");
            rosSocket.Publish(publicationId, message);


            // Subscription:
            string subscriptionId = rosSocket.Subscribe<std_msgs.String>("/subscription_test", SubscriptionHandler<>);

            // Service Call:
            rosSocket.CallService<RosSharp.RosBridgeClient.Messages.Test.Req, RosSharp.RosBridgeClient.Messages.Test.Resp>
                ("/tt_mission_master/tt_mission_service", ServiceCallHandler, new RosSharp.RosBridgeClient.Messages.Test.Req("hey"));

            // Service Response:
            string serviceId = rosSocket.AdvertiseService<std_srvs.TriggerRequest, std_srvs.TriggerResponse>
                ("/service_response_test", ServiceResponseHandler);

            Console.WriteLine("Press any key to unsubscribe...");
            Console.ReadKey(true);
            rosSocket.Unadvertise(publicationId);
            rosSocket.Unsubscribe(subscriptionId);
            rosSocket.UnadvertiseService(serviceId);

            Console.WriteLine("Press any key to close...");
            Console.ReadKey(true);
            rosSocket.Close();
        }
        private static void SubscriptionHandler(std_msgs.String message)
        {
            Console.WriteLine((message).data);
        }

        private static void SubscriptionHandler(RosSharp.RosBridgeClient.Messages.Test.MissionStatus message)
        {
            Console.WriteLine((message).z_alt);
        }


        private static void ServiceCallHandler(RosSharp.RosBridgeClient.Messages.Test.Resp message)
        {
            Console.WriteLine("response: " + message.result);
        }

        private static bool ServiceResponseHandler(std_srvs.TriggerRequest arguments, out std_srvs.TriggerResponse result)
        {
            result = new std_srvs.TriggerResponse(true, "service response message");
            return true;
        }**/
    #endregion

    }

}




