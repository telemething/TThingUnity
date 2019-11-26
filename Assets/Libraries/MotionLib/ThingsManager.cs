using System.Collections;
using System.Collections.Generic;
using GeoLib;
using UnityEngine;

public class ThingsManager : MonoBehaviour
{
    public int Port = 45679;
    public UnityEngine.UI.Text TextObject;
    public string MyThingId = "self";
    private PointLatLonAlt _origin = null;

    private ThingMotion _TM;

    void Start()
    {
        _TM = ThingMotion.GetPoseObject(Port);
        _TM.SetThing(MyThingId, Thing.TypeEnum.Person, Thing.SelfEnum.Self, Thing.RoleEnum.Observer);
    }

    void Update()
    {
        var things = _TM.GetThings();
        things?.ForEach(thing => {if(thing.Self == Thing.SelfEnum.Self) SetSelf(thing); else SetOther(thing); });
    }

    void SetOrigin(ThingPose pose)
    {
        _origin = new PointLatLonAlt(pose.PointGeo);
        _TM.Origin = _origin;
    }

    void SetSelf(Thing thing)
    {
        // set the origin to the first reported location of self
        if (null == _origin) 
            if(thing.Pose.PointGeoUsable == ThingPose.PointGeoUsableEnum.Yes)
                SetOrigin(thing.Pose);
    }

    void SetOther(Thing thing)
    {
        switch (thing.Type)
        {

        }
    }
}
