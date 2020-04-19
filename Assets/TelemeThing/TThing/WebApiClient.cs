using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using EmbedIO;
//using EmbedIO.WebApi;
using System;
using System.IO;
using System.Net;
//using EmbedIO.Actions;
//using EmbedIO.Routing;
using System.Threading.Tasks;
using System.Diagnostics;

using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
//using EmbedIO.WebSockets;

using System.Threading;
//using EmbedIO.Files;
//using EmbedIO.Security;

//using Swan.Logging;

using System.Collections.Generic;
using System.Collections.Specialized;

//using EmbedIO.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace WebApiLib
{
    //*************************************************************************
    /// <summary>
    /// A collection of app specific WebApi method names
    /// </summary>
    //*************************************************************************
    public class WebApiMethodNames
    {
        public static string Settings_RegisterRemoteSettings { get; } = 
            "Settings.RegisterRemoteSettings";
    }

    #region Message

    public enum MessageTypeEnum { unknown, request, response, connection }
    public enum ResultEnum { unknown, notfound, ok, timeout, exception }

    //*************************************************************************
    /// <summary>
    /// WebApi argument
    /// </summary>
    //*************************************************************************
    public class Argument
    {
        public string Name { set; get; }
        public object Value { set; get; }
        public Type Type { set; get; }

        public Argument()
        {
        }

        public Argument(string name, object value)
        {
            Name = name;
            Value = value;
            Type = value.GetType();
        }
    }

    //*************************************************************************
    /// <summary>
    /// WebApi Event
    /// </summary>
    //*************************************************************************
    public class ApiEvent
    {
        public enum EventTypeEnum { unknown, connect, disconnect }

        public Guid GUID { set; get; }

        public EventTypeEnum EventType { set; get; }

        public List<Argument> Arguments { set; get; }

        public ApiEvent()
        {
        }

        public ApiEvent(EventTypeEnum eventType, List<Argument> arguments)
        {
            EventType = eventType;
            Arguments = arguments;
            GUID = Guid.NewGuid();
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        //*********************************************************************
        public static ApiEvent Deserialize(string data)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            //settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            settings.Converters.Add(new StringEnumConverter(true));
            return JsonConvert.DeserializeObject<ApiEvent>(data, settings);
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //*********************************************************************
        public string Serialize()
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            //settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            settings.Converters.Add(new StringEnumConverter(true));
            return JsonConvert.SerializeObject(this, settings);
        }
    }

    //*************************************************************************
    /// <summary>
    /// WebApi Request
    /// </summary>
    //*************************************************************************
    public class Request
    {
        public Guid GUID { set; get; }

        public string MethodName { set; get; }

        public List<Argument> Arguments { set; get; }

        public Request()
        {
        }

        public Request(string methodName, List<Argument> arguments)
        {
            MethodName = methodName;
            Arguments = arguments;
            GUID = Guid.NewGuid();
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        //*********************************************************************
        public static Request Deserialize(string data)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            //settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            settings.Converters.Add(new StringEnumConverter(true));
            return JsonConvert.DeserializeObject<Request>(data, settings);
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //*********************************************************************
        public string Serialize()
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            //settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            settings.Converters.Add(new StringEnumConverter(true));
            return JsonConvert.SerializeObject(this, settings);
        }
    }

    //*************************************************************************
    /// <summary>
    /// WebApi Response
    /// </summary>
    //*************************************************************************
    public class Response
    {
        public Guid RequestGUID { set; get; }

        public ResultEnum Result { set; get; }

        public List<Argument> Arguments { set; get; }

        public Exception Exception { set; get; }

        public Response()
        {
        }

        public Response(Guid requestGUID, ResultEnum result,
            List<Argument> arguments, Exception exception)
        {
            RequestGUID = requestGUID;
            Result = result;
            Arguments = Arguments;
            Exception = exception;
        }

        public Response(ResultEnum result, List<Argument> arguments,
            Exception exception)
        {
            RequestGUID = new Guid();
            Result = result;
            Arguments = Arguments;
            Exception = exception;
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        //*********************************************************************
        public static Response Deserialize(string data)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            //settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            settings.Converters.Add(new StringEnumConverter(true));
            return JsonConvert.DeserializeObject<Response>(data, settings);
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //*********************************************************************
        public string Serialize()
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            //settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            settings.Converters.Add(new StringEnumConverter(true));
            return JsonConvert.SerializeObject(this, settings);
        }
    }

    //*************************************************************************
    /// <summary>
    /// WebApi Message
    /// </summary>
    //*************************************************************************
    public class Message
    {
        public MessageTypeEnum MessageType { set; get; }
        public Request Request { set; get; }
        public Response Response { set; get; }

        public Message()
        {
            MessageType = MessageTypeEnum.unknown;
        }
        public Message(Request request)
        {
            Request = request;
            MessageType = MessageTypeEnum.request;
        }
        public Message(Response response)
        {
            Response = response;
            MessageType = MessageTypeEnum.response;
        }
        public Message(MessageTypeEnum messageType)
        {
            MessageType = messageType;
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        //*********************************************************************
        public static Message Deserialize(string data)
        {
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore };
            //settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            settings.Converters.Add(new StringEnumConverter(true));
            return JsonConvert.DeserializeObject<Message>(data, settings);
        }

        //*********************************************************************
        /// <summary>
        /// 
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

    }

    #endregion

    #region WebApiCore

    //*************************************************************************
    /// <summary>
    /// Web API core functionality
    /// </summary>
    //*************************************************************************
    public class WebApiCore
    {
        //signature of request handling methods
        public delegate List<WebApiLib.Argument> MethodCallback(List<WebApiLib.Argument> args);

        //signature of event handling methods
        public delegate void EventCallback(WebApiLib.ApiEvent apiEvent);

        //list of request handling methods
        Dictionary<string, MethodCallback> _methodList = new Dictionary<string, MethodCallback>();

        //list of event handling methods
        protected List<EventCallback> _eventCallbackList = new List<EventCallback>();

        protected EventWaitHandle _gotNewResponse = new EventWaitHandle(false, EventResetMode.ManualReset);
        protected Queue<Request> RequestsReceived = new Queue<Request>();
        protected Queue<Response> ResponsesReceived = new Queue<Response>();
        protected bool _connectedToApi = false;
        protected WebServerLib.TTWebSocketClient _client = null;

        //*************************************************************************
        /// <summary>
        /// Register an Api method handler to be invoked by name
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="methodCallback"></param>
        //*************************************************************************
        public void AddApiMethod(string methodName, MethodCallback methodCallback)
        {
            _methodList.Add(methodName, methodCallback);
        }

        //*************************************************************************
        /// <summary>
        /// Register an event handler to be invoked for every event
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="methodCallback"></param>
        //*************************************************************************
        public void AddEventCallback(EventCallback eventCallback)
        {
            _eventCallbackList.Add(eventCallback);
        }

        //*************************************************************************
        /// <summary>
        /// Handle a method request
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        //*************************************************************************
        private WebApiLib.Response HandleRequest(WebApiLib.Request req)
        {
            try
            {
                if (!_methodList.TryGetValue(req.MethodName, out MethodCallback method))
                    return new WebApiLib.Response(WebApiLib.ResultEnum.notfound, null, null);

                return new WebApiLib.Response(WebApiLib.ResultEnum.ok, method.Invoke(req.Arguments), null);
            }
            catch (Exception ex)
            {
                return new WebApiLib.Response(WebApiLib.ResultEnum.exception, null, ex);
            }
        }

        //*********************************************************************
        /// <summary>
        /// Called by the WebSocket client when a message is received
        /// </summary>
        /// <param name="data"></param>
        //*********************************************************************
        protected string GotMessageCallback(string data)
        {
            //what kind of message is this?

            var message = Message.Deserialize(data);

            switch (message.MessageType)
            {
                case MessageTypeEnum.connection:
                    _connectedToApi = true;
                    break;
                case MessageTypeEnum.request:
                    var resp = new Message(HandleRequest(message.Request));
                    _client.Send(resp.ToString());
                    break;
                case MessageTypeEnum.response:
                    ResponsesReceived.Enqueue(message.Response);
                    _gotNewResponse.Set();
                    break;
            }

            return null;
        }

        //*********************************************************************
        /// <summary>
        /// Wait for a response to a request made to the connected party
        /// </summary>
        /// <param name="request"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        //*********************************************************************
        public async Task<Response> WaitForResponse(Request request, int timeoutMs)
        {
            Response response = null;
            int spinCount = 0;
            int _spinCountLimit = 1000;

            // wait for response
            while (true)
            {
                try
                {
                    if (_gotNewResponse.WaitOne(timeoutMs))
                    {
                        //sometimes the queue doesn't immidiately register additions.
                        //spin until the count registers one or until we timeout
                        while (0 == ResponsesReceived.Count)
                        {
                            if (spinCount > _spinCountLimit)
                                throw new Exception(
                                    "Response queue spin count exceeded limit of "
                                    + _spinCountLimit.ToString());

                            Thread.Sleep(1);
                        }

                        response = ResponsesReceived.Dequeue();
                    }
                    else
                    {
                        response = new Response(request.GUID, ResultEnum.timeout, null, null);
                    }
                }
                catch (Exception ex)
                {
                    response = new Response(request.GUID, ResultEnum.exception, null, ex);
                }

                break;
            }

            return response;
        }
    }

    #endregion

    #region WebApiClient

    //*************************************************************************
    /// <summary>
    /// Web API Client
    /// </summary>
    //*************************************************************************
    public class WebApiClient : WebApiCore
    {
        public static WebApiClient Singleton { get { return _singleton; } }

        private static WebApiClient _singleton = new WebApiClient();

        //WebServerLib.TTWebSocketClient _client = new WebServerLib.TTWebSocketClient();
        bool _connected = false;

        //*********************************************************************
        /// <summary>
        /// Private constructor
        /// </summary>
        //*********************************************************************
        private WebApiClient()
        {

        }

        //*********************************************************************
        /// <summary>
        /// Connect to a server
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        //*********************************************************************
        public async Task<bool> Connect(string url)
        {
            _client = new WebServerLib.TTWebSocketClient();
            _connected = await _client.Connect(url);

            if (_connected)
            {
                _client.Listen(GotMessageCallback, CancellationToken.None);
                foreach(var ecb in _eventCallbackList)
                    ecb?.Invoke(new ApiEvent(ApiEvent.EventTypeEnum.connect, null));
            }

            return _connected;
        }

        //*********************************************************************
        /// <summary>
        /// Invoke a WebApi method
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="argumentList"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        //*********************************************************************
        public async Task<Response> Invoke(Request request, int timeoutMs = 15000)
        {
            Response response = null;

            var message = new Message(request);

            // send to server
            await _client.Send(message.Serialize());

            // wait for the server to send a response
            return await WaitForResponse(request, timeoutMs);
        }

        public async Task<Response> Invoke(string methodName,
            List<Argument> argumentList, int timeoutMs = 15000)
        {
            var request = new Request(methodName, argumentList);

            return await Invoke(request, timeoutMs);
        }
    }

    #endregion
}

namespace WebServerLib
{
    #region WebSocketClient

    //*************************************************************************
    /// <summary>
    /// Web socket base
    /// </summary>
    //*************************************************************************
    public class TTWebSocket
    {
        public delegate string GotMessageCallback(string data);
        protected GotMessageCallback _gotMessageCallback;
    }

    //https://docs.microsoft.com/en-us/dotnet/api/system.net.websockets.clientwebsocket?view=netstandard-2.0
    //https://thecodegarden.net/websocket-client-dotnet

    //*************************************************************************
    /// <summary>
    /// Web socket client
    /// </summary>
    //*************************************************************************
    public class TTWebSocketClient : TTWebSocket
    {
        //public delegate void GotMessageCallback(string data);
        //private GotMessageCallback _gotMessageCallback;

        ClientWebSocket sock = new ClientWebSocket();

        public TTWebSocketClient()
        { }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        //*********************************************************************
        public async Task<bool> Connect(string url)
        {
            try
            {
                await sock.ConnectAsync(new Uri(url), CancellationToken.None);
                //Receive(CancellationToken.None);

                int sleepTimeMs = 50;
                int abandonTimeMs = 10000;
                int tryCount = 0;

                while (sock.State != WebSocketState.Open)
                {
                    if (tryCount++ > abandonTimeMs / sleepTimeMs)
                    {
                        //TODO do something
                        return false;
                    }

                    Thread.Sleep(sleepTimeMs);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        //*********************************************************************
        /// <summary>
        /// Send a message
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        //*********************************************************************
        public async Task Send(string data)
        {
            try
            {
                while (sock.State != WebSocketState.Open)
                {
                    Thread.Sleep(500);
                }
                //Receive(CancellationToken.None);
                var aseg = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(data));
                await sock.SendAsync(aseg, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }
        }

        //*********************************************************************
        /// <summary>
        /// Listem for messages, call callback when a message is received
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="stoppingToken"></param>
        //*********************************************************************
        public async void Listen(GotMessageCallback callback,
            CancellationToken stoppingToken)
        {
            _gotMessageCallback = callback;

            int sleepTimeMs = 50;
            int abandonTimeMs = 5000;
            int tryCount = 0;

            var buffer = new ArraySegment<byte>(new byte[2048]);

            while (sock.State != WebSocketState.Open)
            {
                if(tryCount++ > abandonTimeMs / sleepTimeMs)
                {
                    //TODO do something
                    return;
                }

                Thread.Sleep(sleepTimeMs);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    WebSocketReceiveResult result;
                    using (var ms = new MemoryStream())
                    {
                        do
                        {
                            result = await sock.ReceiveAsync(buffer, stoppingToken);
                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        } while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close)
                            break;

                        ms.Seek(0, SeekOrigin.Begin);
                        using (var reader = new StreamReader(ms, System.Text.Encoding.UTF8))
                        {
                            //Console.WriteLine(await reader.ReadToEndAsync());

                            string data = await reader.ReadToEndAsync();
                            _gotMessageCallback?.Invoke(data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //TODO Do Something
                }
            };
        }
    }

    #endregion
}