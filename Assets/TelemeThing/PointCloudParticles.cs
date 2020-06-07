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

public class ColorMap
{
    float[] r = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0.9365079365079367f, 0.8571428571428572f, 0.7777777777777777f, 0.6984126984126986f, 0.6190476190476191f, 0.53968253968254f, 0.4603174603174605f, 0.3809523809523814f, 0.3015873015873018f, 0.2222222222222223f, 0.1428571428571432f, 0.06349206349206415f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.03174603174603208f, 0.08465608465608465f, 0.1375661375661377f, 0.1904761904761907f, 0.2433862433862437f, 0.2962962962962963f, 0.3492063492063493f, 0.4021164021164023f, 0.4550264550264553f, 0.5079365079365079f, 0.5608465608465609f, 0.6137566137566139f, 0.666666666666667f };
    float[] g = { 0, 0.03968253968253968f, 0.07936507936507936f, 0.119047619047619f, 0.1587301587301587f, 0.1984126984126984f, 0.2380952380952381f, 0.2777777777777778f, 0.3174603174603174f, 0.3571428571428571f, 0.3968253968253968f, 0.4365079365079365f, 0.4761904761904762f, 0.5158730158730158f, 0.5555555555555556f, 0.5952380952380952f, 0.6349206349206349f, 0.6746031746031745f, 0.7142857142857142f, 0.753968253968254f, 0.7936507936507936f, 0.8333333333333333f, 0.873015873015873f, 0.9126984126984127f, 0.9523809523809523f, 0.992063492063492f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0.9841269841269842f, 0.9047619047619047f, 0.8253968253968256f, 0.7460317460317465f, 0.666666666666667f, 0.587301587301587f, 0.5079365079365079f, 0.4285714285714288f, 0.3492063492063493f, 0.2698412698412698f, 0.1904761904761907f, 0.1111111111111116f, 0.03174603174603208f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    float[] b = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.01587301587301582f, 0.09523809523809534f, 0.1746031746031744f, 0.2539682539682535f, 0.333333333333333f, 0.412698412698413f, 0.4920634920634921f, 0.5714285714285712f, 0.6507936507936507f, 0.7301587301587302f, 0.8095238095238093f, 0.8888888888888884f, 0.9682539682539679f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

    public ColorMap()
    {
        var x = r.Length;
        var y = g.Length;
        var z = b.Length;
    }

    public Color MapColor(float inVal, ref Color color, int alpha, float minVal, float maxVal)
    {
        if (maxVal == minVal)
            maxVal += 1;

        int index = (int)(63 * (inVal - minVal) / (maxVal - minVal));

        if (null == color)
            color = new Color(r[index], g[index], b[index], alpha);
        else
        {
            color.r = r[index];
            color.g = g[index];
            color.b = b[index];
            color.a = alpha;
        }

        return color;
    }
}

public class PointCloudParticles : MonoBehaviour
{
    ParticleSystem.Particle[] cloud;
    bool bPointsUpdated = false;
    private ParticleSystem ps;
    private UnityEngine.UI.Text _statusText = null;
    private UnityEngine.UI.Slider _sliderX = null;
    private UnityEngine.UI.Slider _sliderY = null;
    private UnityEngine.UI.Slider _sliderZ = null;
    private Queue<Action> _exeQueue = new Queue<Action>();
    private RosSocket rosSocket;
    private int _framesToDisplay = 30;
    private int _frameIndex = 0;
    private PointCloudFrame[] _pointCloudFrames;
    private Pose _observerPose;
    private ColorMap _colorMap = null;

    private float _rangeLimitMax = 1;
    private float _rangeLimitMin = 255;
    private float _rangeLimitRange = 1;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Start()
    {
        _statusText = Utils.FindObjectComponentInScene<UnityEngine.UI.Text>("StatusText");
        _sliderX = Utils.FindObjectComponentInScene<UnityEngine.UI.Slider>("SliderX");
        _sliderY = Utils.FindObjectComponentInScene<UnityEngine.UI.Slider>("SliderY");
        _sliderZ = Utils.FindObjectComponentInScene<UnityEngine.UI.Slider>("SliderZ");

        SetNumberOfFramesToDisplay(_framesToDisplay);
        SetObserverPose(0, 0, 0);
        _rangeLimitRange = _rangeLimitMax - _rangeLimitMin;
        _colorMap = new ColorMap();

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
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    //*************************************************************************
    private void SetObserverPose(float x, float y, float z)
    {
        _observerPose.position = new Vector3(x, y, z);
        _observerPose.rotation = Quaternion.Euler(0, 0, 0);
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
        //SetCloudPose();

        if(_sliderX != null)
            SetCloudPose(_sliderX.value, _sliderY.value, _sliderZ.value);

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

    float x;
    private void SetCloudPose()
    {
        //ps.transform.Rotate(Vector3.right * Time.deltaTime * 10);
        //ps.transform.Rotate(Vector3.up * Time.deltaTime * 10);
        //ps.transform.Rotate(Vector3.forward * Time.deltaTime * 10);

        x += Time.deltaTime * 10;
        transform.rotation = Quaternion.Euler(x, 0, 0);
    }

    private void SetCloudPose(float x, float y, float z)
    {
        //ps.transform.Rotate(Vector3.right * Time.deltaTime * 10);
        //ps.transform.Rotate(Vector3.up * Time.deltaTime * 10);
        //ps.transform.Rotate(Vector3.forward * Time.deltaTime * 10);

        ps.transform.rotation = Quaternion.Euler(x, y, z);
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

        if(null != _statusText)
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
    /// <param name="inVal"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    //*************************************************************************
    private Color Float2Colory(float inVal, int alpha, Color color)
    {
        float r = 0, g = 0, b = 0;

        inVal = (inVal - _rangeLimitMin) / _rangeLimitRange;

        var a = (1 - inVal) / 0.25; //invert and group
        var X = Math.Floor(a);  //this is the integer part
        var Y = (float)Math.Floor(255 * (a - X)); //fractional part from 0 to 255
        switch ((int)X)
        {
            case 0: r = 255; g = Y; b = 0; break;
            case 1: r = 255 - Y; g = 255; b = 0; break;
            case 2: r = 0; g = 255; b = Y; break;
            case 3: r = 0; g = 255 - Y; b = 255; break;
            case 4: r = 0; g = 0; b = 255; break;
        }

        if (null == color)
            color = new Color(r, g, b, alpha);
        else
        {
            color.r = r;
            color.g = g;
            color.b = b;
            color.a = alpha;
        }

        return color;
    }

    //*************************************************************************
    private Color Float2Color(float inVal, int alpha, Color color)
    {
        float r = 0, g = 0, b = 0;

        inVal = (inVal - _rangeLimitMin) / _rangeLimitRange;

        var a = (1 - inVal) / 0.25; //invert and group
        var X = Math.Floor(a);  //this is the integer part
        var Y = (float)Math.Floor(255 * (a - X)); //fractional part from 0 to 255
        switch ((int)X)
        {
            case 0: r = 255; g = Y; b = 0; break;
            case 1: r = 255 - Y; g = 255; b = 0; break;
            case 2: r = 0; g = 255; b = Y; break;
            case 3: r = 0; g = 255 - Y; b = 255; break;
            case 4: r = 0; g = 0; b = 255; break;
        }

        if (null == color)
        {
            //color = new Color(r, g, b, alpha);
            color = new Color(2.0f * inVal, 2.0f * (1 - inVal), 0);
        }
        else
        {
            color.r = 2.0f * inVal;
            color.g = 2.0f * (1 - inVal);
            color.b = 0;
            color.a = alpha;
        }

        return color;
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
        float distanceToObserver = 1;
        Vector3 point = new Vector3();

        pointCloudFrame.Rightsize((int)pc.width);
        var points = pointCloudFrame.Points;
        var colors = pointCloudFrame.Colors;

        for (int index = 0; index < pc.width; index++)
        {
            //x = BitConverter.ToSingle(pc.data, dataIndex);
            //y = BitConverter.ToSingle(pc.data, dataIndex + dataLen);
            //z = BitConverter.ToSingle(pc.data, dataIndex + (dataLen * 2));

            //tricky bits. ROS is right hand Z up, Unity is left hand Y up.
            //so ROS x,y,z map to Unity -y,z,x. 
            //y = -BitConverter.ToSingle(pc.data, dataIndex);
            //z = BitConverter.ToSingle(pc.data, dataIndex + dataLen);
            //x = BitConverter.ToSingle(pc.data, dataIndex + (dataLen * 2));

            //tricky bits. Livox is x fwd, y right Z up, Unity is z fwd x left y up
            //so ROS x,y,z map to Unity -y,z,x. 
            z = BitConverter.ToSingle(pc.data, dataIndex);
            x = - BitConverter.ToSingle(pc.data, dataIndex + dataLen);
            y = BitConverter.ToSingle(pc.data, dataIndex + (dataLen * 2));

            skip = false;
            if (removeDups)
            {
                if ((x == 0.0) & (y == 0.0) & (z == 0.0))
                    skip = true;
                else if (pointIndex > 0)
                    if ((points[pointIndex - 1].x == x) & (points[pointIndex - 1].y == y) & (points[pointIndex - 1].z == z))
                        skip = true;
            }

            if (!skip)
            {
                point.x = x;
                point.y = y;
                point.z = z;

                distanceToObserver = Vector3.Distance(point, _observerPose.position);

                if (distanceToObserver < 10)
                    skip = true;
            }

            if (!skip)
            {
                float i = BitConverter.ToSingle(pc.data, dataIndex + (dataLen * 4));

                if (null == points[pointIndex])
                {
                    points[pointIndex] = new Vector3(x, y, z);
                }
                else
                {
                    points[pointIndex].x = x;
                    points[pointIndex].y = y;
                    points[pointIndex].z = z;
                    //colors[pointIndex].r = 255;
                    //colors[pointIndex].g = 0;
                    //colors[pointIndex].b = 0;
                    //colors[pointIndex].a = 255;
                }

                _rangeLimitMax = Math.Max(_rangeLimitMax, distanceToObserver);
                _rangeLimitMin = Math.Min(_rangeLimitMin, distanceToObserver);
                _rangeLimitRange = _rangeLimitMax - _rangeLimitMin;

                //colors[pointIndex] = Float2Color(distanceToObserver, 255, colors[pointIndex]);

                colors[pointIndex] = _colorMap.MapColor(distanceToObserver, ref colors[pointIndex], 255, _rangeLimitMin, _rangeLimitMax);

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

        if(null != _statusText)
            _exeQueue.Enqueue(() => _statusText.text = ff);
    }
}
