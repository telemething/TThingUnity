using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosSharp;
using RosSharp.RosBridgeClient;
using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;
using System.Threading.Tasks;
using System;

public class PointCloudFrame
{
    public Vector3[] Points { set; get; } = new Vector3[1];
    public Color[] Colors { set; get; } = new Color[1];

    public int PointCount = 0;

    public void Rightsize(int size)
    {
        if (size > Points.Length)
            Points = new Vector3[size];

        if (size > Colors.Length)
            Colors = new Color[size];
    }
}

public class PointCloudParticles : MonoBehaviour
{
    ParticleSystem.Particle[] cloud;
    bool bPointsUpdated = false;
    private ParticleSystem ps;
    private UnityEngine.UI.Text _statusText = null;
    private Queue<Action> _exeQueue = new Queue<Action>();
    RosSocket rosSocket;
    int _framesToDisplay = 10;
    int _frameIndex = 0;
    PointCloudFrame[] _pointCloudFrames;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Start()
    {
        _statusText = Utils.FindObjectComponentInScene<UnityEngine.UI.Text>("StatusText");

        SetNumberOfFramesToDisplay(_framesToDisplay);

        var rosBridgeUrl = (string)AppSettings.App.RosSettings.RosBridgeUrl.Value;
        Task.Run(() => SetupRosBridge(rosBridgeUrl));

        //DisplayTestCloud();

        ps = GetComponent<ParticleSystem>();

        T1.CLogger.LogThis("ThingsManager.Start()");
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="frameCount"></param>
    //*************************************************************************
    private void SetNumberOfFramesToDisplay(int frameCount)
    {
        _frameIndex = 0;
        _pointCloudFrames = new PointCloudFrame[frameCount];

        for (int index = 0; index < frameCount; index++)
            _pointCloudFrames[index] = new PointCloudFrame();
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void DisplayTestCloud()
    {
        float v = 1.0f;

        Vector3[] pos = new Vector3[4];
        pos[0] = new Vector3(v, v, v);
        pos[1] = new Vector3(-v, v, -v);
        pos[2] = new Vector3(-v, v, v);
        pos[3] = new Vector3(v, v, -v);

        Color[] colors = new Color[4];
        colors[0] = new Color32(0, 0, 255, 255);
        colors[1] = new Color32(0, 255, 0, 255);
        colors[2] = new Color32(255, 0, 0, 255);
        colors[3] = new Color32(255, 100, 0, 255);

        SetPoints(pos, colors, pos.Length);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Update()
    {
        if (bPointsUpdated)
        {
            var main = ps.main;

            ps.SetParticles(cloud, cloud.Length);
            bPointsUpdated = false;
        }

        while (_exeQueue.Count > 0)
        {
            _exeQueue.Dequeue().Invoke();
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="positions"></param>
    /// <param name="colors"></param>
    //*************************************************************************
    public void SetPoints(Vector3[] positions, Color[] colors, int count)
    {
        cloud = new ParticleSystem.Particle[positions.Length];

        for (int ii = 0; ii < count; ++ii)
        {
            cloud[ii].position = positions[ii];
            cloud[ii].startColor = colors[ii];
            cloud[ii].startSize = 0.1f;
        }

        bPointsUpdated = true;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pointCloudFrame"></param>
    //*************************************************************************
    public void SetPoints(PointCloudFrame pointCloudFrame)
    {
        SetPoints(pointCloudFrame.Points, pointCloudFrame.Colors, pointCloudFrame.PointCount);
        bPointsUpdated = true;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pointCloudFrames"></param>
    //*************************************************************************
    public void SetPoints(PointCloudFrame[] pointCloudFrames)
    {
        int totalLength = 0;
        int ci = 0;

        foreach(var pcf in pointCloudFrames)
            totalLength += pcf.PointCount;

        cloud = new ParticleSystem.Particle[totalLength];

        foreach (var pcf in pointCloudFrames)
        {
            for (int pi = 0; pi < pcf.PointCount; ++pi)
            {
                cloud[ci].position = pcf.Points[pi];
                cloud[ci].startColor = pcf.Colors[pi];
                cloud[ci].startSize = 0.1f;
                ci++;
            }
        }

        bPointsUpdated = true;
    }

    //*************************************************************************
    /// <summary>
    /// https://github.com/siemens/ros-sharp/wiki/Info_CodeExample
    /// </summary>
    //*************************************************************************
    private void SetupRosBridge( string RosBridgeUrl)
    {
        rosSocket = new RosSocket(
            new RosSharp.RosBridgeClient.Protocols.WebSocketNetProtocol(RosBridgeUrl));

        //ChatterTest();

        //SubscribeToPointCloud("/rtabmap/octomap_occupied_space");
        SubscribeToPointCloud("/livox/lidar");
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*********************************************************************

    private async void SubscribeToPointCloud(string rosTopicName)
    {
        var subscriptionId = rosSocket.Subscribe
            <RosSharp.RosBridgeClient.MessageTypes.Sensor.PointCloud2>(
                rosTopicName, PointCloudSubscriptionHandler);
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pc"></param>
    //*********************************************************************
    private int pointCloudMessageCount = 1;
    private long pointCloudAccumulatedSize = 0;

    private void PointCloudSubscriptionHandler(
    RosSharp.RosBridgeClient.MessageTypes.Sensor.PointCloud2 pc)
    {
        pointCloudAccumulatedSize += pc.data.Length;

        var statusMessage = 
            $"PointCloud Data {pointCloudMessageCount++}, size: {pc.data.Length}, total: {pointCloudAccumulatedSize}";

        System.Diagnostics.Debug.WriteLine(statusMessage);
        _exeQueue.Enqueue(() => _statusText.text = statusMessage);

        var count = ExtractPoints(pc, _pointCloudFrames[_frameIndex++]);
        if (_frameIndex == _framesToDisplay)
            _frameIndex = 0;

        //SetPoints(_pointCloudFrame.Points, _pointCloudFrame.Colors, count);
        //SetPoints(_pointCloudFrames[0]);
        SetPoints(_pointCloudFrames);

    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pc"></param>
    /// <param name="points"></param>
    /// <param name="colors"></param>
    /// <returns></returns>
    //*************************************************************************
    private int ExtractPoints(
        RosSharp.RosBridgeClient.MessageTypes.Sensor.PointCloud2 pc,
        PointCloudFrame pointCloudFrame)
    {
        int dataIndex = 0;
        int pointIndex = 0;
        int dataLen = 4;
        bool removeDups = true;
        bool skip = false;
        float x, y, z;

        pointCloudFrame.Rightsize((int)pc.width);
        var points = pointCloudFrame.Points;
        var colors = pointCloudFrame.Colors;

        for (int index = 0; index < pc.width; index++)
        {
            x = BitConverter.ToSingle(pc.data, dataIndex);
            y = BitConverter.ToSingle(pc.data, dataIndex + dataLen);
            z = BitConverter.ToSingle(pc.data, dataIndex + (dataLen * 2));

            if(removeDups) 
            if(pointIndex > 0)
            {
                if ((x == 0.0) & (y == 0.0) & (z == 0.0))
                    skip = true;
                else if ((points[pointIndex - 1].x == x) & (points[pointIndex - 1].y == y) & (points[pointIndex - 1].z == z))
                    skip = true;
                else
                    skip = false;
            }

            if (!skip)
            {
                float i = BitConverter.ToSingle(pc.data, dataIndex + (dataLen * 3));

                if (null == points[pointIndex])
                {
                    points[pointIndex] = new Vector3(x, y, z);
                    colors[pointIndex] = new Color(255, 0, 0, 255);
                }
                else
                {
                    points[pointIndex].x = x;
                    points[pointIndex].y = y;
                    points[pointIndex].z = z;
                    colors[pointIndex].r = 255;
                    colors[pointIndex].g = 0;
                    colors[pointIndex].b = 0;
                    colors[pointIndex].a = 255;
                }

                pointIndex++;
            }

            dataIndex += (int)pc.point_step;
        }

        pointCloudFrame.PointCount = pointIndex;
        return pointIndex;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    private void ChatterTest()
    {
        //Test
        string subscription_id = 
            rosSocket.Subscribe<std_msgs.String>("/chatter", ChatterSubscriptionHandler);

        // Publication:
        string publication_id = rosSocket.Advertise<std_msgs.String>("publication_test");
        std_msgs.String message = new std_msgs.String("publication test message data");
        rosSocket.Publish(publication_id, message);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    //*************************************************************************
    private void ChatterSubscriptionHandler(std_msgs.String message)
    {
        var ff = (message).data;
        _exeQueue.Enqueue(() => _statusText.text = ff);
    }
}
