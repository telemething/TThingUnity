using System;
using System.Collections;
using System.Collections.Generic;
using GeoLib;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

public class ThingsManager : MonoBehaviour
{
    public int Port = 45679;
    public UnityEngine.UI.Text TextObject;
    public string MyThingId = "self";
    private PointLatLonAlt _origin = null;
    private static Thing _self = null;

    private static double _maxMaxThingDistance = 50.0;
    private static double _minMinThingDistance = 0.3;

    private static double _maxThingDistance = _maxMaxThingDistance;
    private static double _minThingDistance = _minMinThingDistance;

    private ThingMotion _TM;

    public static Thing Self
    {
        get => _self;
        set => _self = value;
    }

    public static double MinThingDistance
    {
        get => _minThingDistance;
        set => _minThingDistance = value;
    }

    public static double MaxThingDistance
    {
        get => _maxThingDistance;
        set => _maxThingDistance = value;
    }

    void Start()
    {
        _TM = ThingMotion.GetPoseObject(Port);
        _TM.SetThing(MyThingId, Thing.TypeEnum.Person, Thing.SelfEnum.Self, Thing.RoleEnum.Observer);
    }

    void Update()
    {
        var things = _TM.GetThings();

        //at each update, we check max and min distances of the things
        _maxThingDistance = _maxMaxThingDistance;
        _minThingDistance = _minMinThingDistance;

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

        _self = thing;
    }

    void SetOther(Thing thing)
    {
        if (null == thing.GameObjectObject)
            thing.GameObjectObject = ThingGameObject.CreateGameObject(thing);
        else
            ThingGameObject.UpdateThing(thing);
    }

    public static void ValidateFrustum(double distance)
    {
        if (distance > _maxThingDistance)
            _maxThingDistance = distance;

        if (distance < _minThingDistance)
            _minThingDistance = distance;

        //if thing gets within 10M of clip plane, grow clip plane by 25M
        if (_maxThingDistance > Camera.main.farClipPlane - 10)
            Camera.main.farClipPlane += 25;

        //TODO * Optimize frustum by moving farClipPlane as close as possible
        //TODO * Optimize frustum by moving nearClipPlane as far away as possible
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

    private static double GetDistance(PointENU from, PointENU to)
    {
        return Math.Sqrt(
            Math.Pow((to.E - from.E), 2) + 
            Math.Pow((to.N - from.N), 2) + 
            Math.Pow((to.U - from.U), 2));
    }

    private static double GetDistance(Thing from, Thing to)
    {
        if (null == from)
            return 0;

        if (null == to)
            return 0;

        if (null == from.Pose)
            return 0;

        if (null == to.Pose)
            return 0;

        if (null == from.Pose.PointEnu)
            return 0;

        if (null == to.Pose.PointEnu)
            return 0;

        return GetDistance(from.Pose.PointEnu, to.Pose.PointEnu);
    }

    public void Update(Thing thing)
    {
        //find distance to 'self'
        thing.DistanceToObserver = GetDistance(ThingsManager.Self, thing);

        //Let the ThingsManager know our distance so that it can
        //optimize the frustum size
        ThingsManager.ValidateFrustum(thing.DistanceToObserver);

        //x = E, y = U, z = N
        _gameObject.transform.position = new Vector3(
            Convert.ToSingle(thing.Pose.PointEnu.E),
            Convert.ToSingle(thing.Pose.PointEnu.U),
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
        _gameObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;
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
        _gameObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;
    }
}
