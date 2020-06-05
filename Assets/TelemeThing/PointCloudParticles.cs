using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosSharp;
using RosSharp.RosBridgeClient;
using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;

public class PointCloudParticles : MonoBehaviour
{
    ParticleSystem.Particle[] cloud;
    bool bPointsUpdated = false;
    private ParticleSystem ps;

    RosSocket rosSocket;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Start()
    {
        ConnectToRos();

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

        SetPoints(pos, colors);

        ps = GetComponent<ParticleSystem>();
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
            //main.startColor = new Color32(100, 100, 100, 255);

            ps.SetParticles(cloud, cloud.Length);
            bPointsUpdated = false;
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="positions"></param>
    /// <param name="colors"></param>
    //*************************************************************************
    public void SetPoints(Vector3[] positions, Color[] colors)
    {
        cloud = new ParticleSystem.Particle[positions.Length];

        for (int ii = 0; ii < positions.Length; ++ii)
        {
            cloud[ii].position = positions[ii];
            //cloud[ii].color = colors[ii];
            //cloud[ii].size = 0.1f;
            cloud[ii].color = colors[ii];
            cloud[ii].startSize = 0.4f;

        }

        bPointsUpdated = true;
    }

    //*************************************************************************
    /// <summary>
    /// https://github.com/siemens/ros-sharp/wiki/Info_CodeExample
    /// </summary>
    //*************************************************************************
    private void ConnectToRos()
    {
        rosSocket = new RosSocket(new RosSharp.RosBridgeClient.Protocols.WebSocketNetProtocol("ws://192.168.1.30:9090"));

        string subscription_id = rosSocket.Subscribe<std_msgs.String>("/chatter", SubscriptionHandler);
        //subscription_id = rosSocket.Subscribe<std_msgs.String>("/subscription_test", SubscriptionHandler);

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
    private static void SubscriptionHandler(std_msgs.String message)
    {
        var ff = (message).data;
        //Console.WriteLine((message).data);
    }
}
