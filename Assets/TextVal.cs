using System;
using System.Collections;
using System.Collections.Generic;
using MessageLib;
using MotionLib;
using UnityEngine;
using Motion = MotionLib.Motion;

public class TextVal : MonoBehaviour
{

    private ThingMotion _TM;
    private MotionStats _motionStats;
    private MotionStatRecord _motionStatRecord;

    // public string IP = "127.0.0.1"; default local
    //public int port; // define > init
    public UnityEngine.UI.Text _text;
    public bool RotR;
    public bool RotP;
    public bool RotY;
    public bool PosX;
    public bool PosY;
    public bool PosZ;
    public bool RotStddev;

    private float _sliderVal = 0;
    private string _analysis;

    public MotionLib.Motion.MotionStruct Motiondata = new MotionLib.Motion.MotionStruct(true);

    // Use this for initialization
    void Start()
    {
        _TM = ThingMotion.GetMotionObject();
        Motiondata = _TM.Motiondata;

        if (RotStddev)
            _TM.StartCollectingStats(StatsReadyCallback);
    }

    // Update is called once per frame
    void Update()
    {
        if (RotR) _sliderVal = (float)Motiondata.YPR.r;
        else if (RotP) _sliderVal = (float)Motiondata.YPR.p;
        else if (RotY) _sliderVal = (float)Motiondata.YPR.y;
        else if (PosX) _sliderVal = (float)Motiondata.Accell.x;
        else if (PosY) _sliderVal = (float)Motiondata.Accell.y;
        else if (PosZ) _sliderVal = (float)Motiondata.Accell.z;
        else if (null != _motionStatRecord)
        {
            if (RotStddev)
            {
                _text.text = _analysis;
                return;
            }
        }

        _text.text = _sliderVal.ToString();
    }

    private void StatsReadyCallback(MessageRecordI statsMessage)
    {
        _motionStatRecord = statsMessage as MotionStatRecord;

        if (null == _motionStatRecord)
        {
            //*** TODO * Bad format, alert
            return;
        }

        ShowAnalysis();
    }

    private void ShowAnalysis()
    {
        _analysis = "Analyzing ...";

#if UNITY_WSA_10_0
        //System.Threading.Tasks.Task.Delay((int)(1000)).Wait();
#else
        while (_stopwatch.ElapsedMilliseconds + _startTimeMs < ms.ElapsedTimeMs) ;
#endif
        var simpleQ = (float)(250 *
                             (_motionStatRecord.RotStddev.y +
                              _motionStatRecord.RotStddev.p +
                              _motionStatRecord.RotStddev.r) / 3);

        simpleQ = (float)Math.Round(simpleQ, 3);

        var _Classification = "Too Low Error";

        if (simpleQ < 2)
            _Classification = "Bench";
        else if (simpleQ < 20)
            _Classification = "Very Good";
        else if (simpleQ < 50)
            _Classification = "Good";
        else if (simpleQ < 100)
            _Classification = "OK";
        else if (simpleQ < 200)
            _Classification = "Meh";
        else
            _Classification = "Don't quit day job";

        _analysis = string.Format("Analysis: SRQ:{0} Class:{1}", simpleQ, _Classification);
    }
}

