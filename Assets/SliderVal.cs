using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderVal : MonoBehaviour
{
    private ThingMotion _TM;

    // public string IP = "127.0.0.1"; default local
    //public int port; // define > init
    public UnityEngine.UI.Slider _slider;
    public bool RotR;
    public bool RotP;
    public bool RotY;
    public bool PosX;
    public bool PosY;
    public bool PosZ;

    private float _sliderVal = 0;

    public MotionLib.Motion.MotionStruct Motiondata = new MotionLib.Motion.MotionStruct(true);

    // Use this for initialization
    void Start()
    {
        _TM = ThingMotion.GetMotionObject();
        Motiondata = _TM.Motiondata;
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

        _slider.value = _sliderVal;
    }
}