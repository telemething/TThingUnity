using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseVal : MonoBehaviour
{
    private ThingMotion _TM;

    // public string IP = "127.0.0.1"; default local
    public int Port = 45679;
    public UnityEngine.UI.Text TextObject;
    public string ThingId = "self";
    public bool RotR;
    public bool RotP;
    public bool RotY;
    public bool PosX;
    public bool PosY;
    public bool PosZ;

    private float _sliderVal = 0;

    // Use this for initialization
    void Start()
    {
        _TM = ThingMotion.GetPoseObject(Port);
    }

    // Update is called once per frame
    void Update()
    {
        /*if (RotR) _sliderVal = (float)Motiondata.YPR.r;
        else if (RotP) _sliderVal = (float)Motiondata.YPR.p;
        else if (RotY) _sliderVal = (float)Motiondata.YPR.y;
        else if (PosX) _sliderVal = (float)Motiondata.Accell.x;
        else if (PosY) _sliderVal = (float)Motiondata.Accell.y;
        else if (PosZ) _sliderVal = (float)Motiondata.Accell.z;

        _slider.value = _sliderVal;*/

        //var pose = _TM.GetThingPose(ThingId) + "\r\nnext line +\r\n line three";
        //TextObject.text = null == pose ? "---" : pose.ToString();

        string outText = "";

        var things = _TM.GetThings();

        foreach (var thing in things)
            outText += thing.Pose.ToString() + "\r\n";

        TextObject.text = outText;
    }
}
