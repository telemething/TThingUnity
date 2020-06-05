using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosSharp;
using RosSharp.RosBridgeClient;
using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;
using System.Threading.Tasks;
using System;

public class PointCloudParticles : MonoBehaviour
{
    ParticleSystem.Particle[] cloud;
    bool bPointsUpdated = false;
    private ParticleSystem ps;
    private UnityEngine.UI.Text _statusText = null;
    private Queue<Action> _exeQueue = new Queue<Action>();
    RosSocket rosSocket;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Start()
    {
        _statusText = Utils.FindObjectComponentInScene<UnityEngine.UI.Text>("StatusText");

        var rosBridgeUrl = (string)AppSettings.App.RosSettings.RosBridgeUrl.Value;
        Task.Run(() => SetupRosBridge(rosBridgeUrl));

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

        ps = GetComponent<ParticleSystem>();
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************rosbag 
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
        Vector3[] points;
        Color[] colors;
        pointCloudAccumulatedSize += pc.data.Length;

        var statusMessage = 
            $"PointCloud Data {pointCloudMessageCount++}, size: {pc.data.Length}, total: {pointCloudAccumulatedSize}";

        System.Diagnostics.Debug.WriteLine(statusMessage);
        _exeQueue.Enqueue(() => _statusText.text = statusMessage);

        var count = ExtractPoints(pc, out points, out colors);

        SetPoints(points, colors, count);
    }

    private int ExtractPoints(
        RosSharp.RosBridgeClient.MessageTypes.Sensor.PointCloud2 pc, 
        out Vector3[] points, out Color[] colors)
    {
        int dataIndex = 0;
        int pointIndex = 0;
        int dataLen = 4;
        bool removeDups = true;
        bool skip = false;
        float x, y, z;

        points = new Vector3[pc.width];
        colors = new Color[pc.width];
        
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

                points[pointIndex] = new Vector3(x, y, z);
                colors[pointIndex++] = new Color(255,0,0,255);
            }


            dataIndex += (int)pc.point_step;
        }

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
