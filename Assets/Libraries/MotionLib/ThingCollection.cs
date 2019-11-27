using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeoLib;
using UnityEngine;

public class Thing
{
    private ThingPose _pose = new ThingPose();
    private string _id;
    private TypeEnum _type = TypeEnum.UnInit;
    private SelfEnum _self = SelfEnum.UnInit;
    private RoleEnum _role = RoleEnum.UnInit;
    private object _tag = null;
    private object _gameObjectObject = null;
    private Dictionary<string,object> _tagList = null;

    public enum TypeEnum { UnInit, Unknown, Uav, Rover, Stationary, Person }

    public enum SelfEnum { UnInit, Unknown, Self, Other }

    public enum RoleEnum { UnInit, Unknown, Observer, Observed }

    public ThingPose Pose => _pose;


    public string Id
    {
        set => _id = value;
        get => _id;
    }
    public TypeEnum Type
    {
        set => _type = value;
        get => _type;
    }
    public SelfEnum Self
    {
        set => _self = value;
        get => _self;
    }
    public RoleEnum Role
    {
        set => _role = value;
        get => _role;
    }

    public object Tag
    {
        set => _tag = value;
        get => _tag;
    }

    public object GameObjectObject
    {
        set => _gameObjectObject = value;
        get => _gameObjectObject;
    }

    public void SetTag<T>(T value, string tagId)
    {
        if (!_tagList.ContainsKey(tagId))
            _tagList.Add(tagId, value);
        else
            _tagList[tagId] = value;
    }

    public T GetTag<T>(string tagId) where T:class
    {
        if (!_tagList.ContainsKey(tagId))
            return default(T);
        
        return _tagList[tagId] as T;
    }

    public Thing(string id)
    {
        _id = id;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="type"></param>
    /// <param name="self"></param>
    /// <param name="role"></param>
    //*************************************************************************

    public Thing(string id, TypeEnum type, SelfEnum self, RoleEnum role)
    {
        _id = id;
        _type = type;
        _role = role;
        _self = self;
    }
}

//*************************************************************************
/// <summary>
///
/// 
/// </summary>
//*************************************************************************

public class ThingCollection
{
    private PointLatLonAlt _origin = null;
    private Dictionary<string, Thing> _things = new Dictionary<string, Thing>();

   public PointLatLonAlt Origin
    {
        set
        {
            _origin = value;
            foreach (var thing in _things)
                thing.Value.Pose.Origin = _origin;
        }
        get => _origin;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public ThingCollection()
    {

    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thingId"></param>
    /// <param name="pose"></param>
    //*************************************************************************

    public void SetPose(string thingId, ThingPose pose)
    {
        GetMakeThing(thingId).Pose.SetVals(pose);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thingId"></param>
    /// <param name="jsonString"></param>
    //*************************************************************************

    public void SetPose(string thingId, string jsonString)
    {
        GetMakeThing(thingId).Pose.SetVals(jsonString);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="jsonString"></param>
    //*************************************************************************

    public void SetPose(string jsonString)
    {
        var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(jsonString);
        string thingId = jsonObj["id"].ToString();
        GetMakeThing(thingId).Pose.SetVals(jsonObj);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thingId"></param>
    //*************************************************************************

    private Thing GetMakeThing(string thingId)
    {
        if (!_things.ContainsKey(thingId))
        {
            var thing = new Thing(thingId);
            thing.Pose.Origin = _origin;
            _things.Add(thingId, thing);
        }

        return _things[thingId];
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thingId"></param>
    /// <param name="type"></param>
    /// <param name="self"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    //*************************************************************************

    public Thing SetThing(string thingId, Thing.TypeEnum type, 
        Thing.SelfEnum self, Thing.RoleEnum role)
    {
        Thing thing;

        if (!_things.ContainsKey(thingId))
        {
            thing = new Thing(thingId);
            thing.Pose.Origin = _origin;
            _things.Add(thingId, thing);
        }
        else
            thing = _things[thingId];

        thing.Type = type;
        thing.Role = role;
        thing.Self = self;

        return thing;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thingId"></param>
    //*************************************************************************

    public Thing GetThing(string thingId)
    {
        return !_things.ContainsKey(thingId) ? null : _things[thingId];
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public List<Thing> GetThings()
    {
        return _things.Select(thing => thing.Value).ToList();
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thingId"></param>
    //*************************************************************************

    public ThingPose GetPose(string thingId)
    {
        var theThing = GetThing(thingId);
        return theThing?.Pose;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public List<ThingPose> GetPoses()
    {
        return _things.Select(thing => thing.Value.Pose).ToList();
    }
}
