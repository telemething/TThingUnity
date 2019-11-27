using System;
using System.Collections;
using System.Collections.Generic;
using GeoLib;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

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
        if (null == thing.GameObjectObject)
            thing.GameObjectObject = ThingGameObject.CreateGameObject(thing);
        else
            ThingGameObject.UpdateThing(thing);
    }
}

public class ThingGameObject
{
    protected GameObject _gameObject = null;

    public ThingGameObject()
    {
        _gameObject = new GameObject();
    }

    public static ThingGameObject CreateGameObject(Thing thing)
    {
        ThingGameObject thingGameObject  = null;

        switch (thing.Type)
        {
            case Thing.TypeEnum.Uav:
                thingGameObject = new ThingUavObject();
                break;
            default:
                thingGameObject = new ThingUnInitObject();
                break;
        }

        return thingGameObject;
    }

    public static void UpdateThing(Thing thing)
    {
        var gameObj = thing.GameObjectObject as ThingGameObject;
        gameObj?.Update(thing);
    }

    public void Update(Thing thing)
    {
        //x = E, y = U, z = N
        /*_gameObject.transform.position = new Vector3(
            Convert.ToSingle(thing.Pose.PointEnu.E),
            Convert.ToSingle(thing.Pose.PointEnu.U),
            Convert.ToSingle(thing.Pose.PointEnu.N));*/

        //x = E, y = U, z = N
        _gameObject.transform.position = new Vector3(
            Convert.ToSingle(thing.Pose.PointEnu.E),
            Convert.ToSingle(0),
            Convert.ToSingle(thing.Pose.PointEnu.N));
    }
}

public class ThingUavObject : ThingGameObject
{
    public ThingUavObject()
    {
        base._gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        base._gameObject.name = "UAV";
        _gameObject.transform.localScale = new Vector3(1, 1, 1);
        _gameObject.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
    }
}

public class ThingUnInitObject : ThingGameObject
{
    public ThingUnInitObject()
    {
        base._gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        base._gameObject.name = "UAV";
        _gameObject.transform.localScale = new Vector3(1, 1, 1);
        _gameObject.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
    }
}
