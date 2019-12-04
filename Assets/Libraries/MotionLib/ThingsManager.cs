using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GeoLib;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

//*************************************************************************
/// <summary>
/// 
/// </summary>
//*************************************************************************

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

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    void Start()
    {
        _TM = ThingMotion.GetPoseObject(Port);
        _TM.SetThing(MyThingId, Thing.TypeEnum.Person, Thing.SelfEnum.Self, Thing.RoleEnum.Observer);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Update()
    {
        var things = _TM.GetThings();

        //at each update, we check max and min distances of the things
        _maxThingDistance = _maxMaxThingDistance;
        _minThingDistance = _minMinThingDistance;

        things?.ForEach(thing => {if(thing.Self == Thing.SelfEnum.Self) SetSelf(thing); else SetOther(thing); });
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pose"></param>
    //*************************************************************************
    void SetOrigin(ThingPose pose)
    {
        _origin = new PointLatLonAlt(pose.PointGeo);
        _TM.Origin = _origin;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thing"></param>
    //*************************************************************************
    void SetSelf(Thing thing)
    {
        // set the origin to the first reported location of self
        if (null == _origin) 
            if(thing.Pose.PointGeoUsable == ThingPose.PointGeoUsableEnum.Yes)
                SetOrigin(thing.Pose);

        if (null == thing.GameObjectObject)
            thing.GameObjectObject = ThingGameObject.CreateGameObject(thing);

        _self = thing;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thing"></param>
    //*************************************************************************
    void SetOther(Thing thing)
    {
        if (null == thing.GameObjectObject)
            thing.GameObjectObject = ThingGameObject.CreateGameObject(thing);
        else
            ThingGameObject.UpdateThing(thing);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="distance"></param>
    //*************************************************************************
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

//*************************************************************************
/// <summary>
/// 
/// </summary>
//*************************************************************************

public class ThingGameObject
{
    protected Thing _thing = null;
    protected GameObject _gameObject = null;
    protected GameObject _haloObject = null;
    protected Interactable _interactableObject = null;
    protected ThemeDefinition _newThemeType;
    protected BoxCollider _boxCollider;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public ThingGameObject()
    {
        _gameObject = new GameObject();
        SetupInteractable();
        AddFocusEvents();
    }

    //*************************************************************************
    /// <summary>
    /// https://microsoft.github.io/MixedRealityToolkit-Unity/Documentation/README_Interactable.html
    /// </summary>
    //*************************************************************************

    private void SetupInteractable()
    {
        _interactableObject = _gameObject.AddComponent<Interactable>();
        _boxCollider = _gameObject.AddComponent<BoxCollider>();

        /*_newThemeType = ThemeDefinition.GetDefaultThemeDefinition<InteractableColorTheme>().Value;

        // Define a color for every state in our Default Interactable States
        _newThemeType.StateProperties[0].Values = new List<ThemePropertyValue>()
        {
            new ThemePropertyValue() { Color = Color.red},  // Default
            new ThemePropertyValue() { Color = Color.cyan}, // Focus
            //new ThemePropertyValue() { Color = UnityEngine.Random.ColorHSV()},   // Pressed
            new ThemePropertyValue() { Color = Color.green},   // Pressed
            new ThemePropertyValue() { Color = Color.yellow},   // Disabled
        };

        _interactableObject.Profiles = new List<InteractableProfileItem>()
        {
            new InteractableProfileItem()
            {
                Themes = new List<Theme>()
                {
                    Interactable.GetDefaultThemeAsset(new List<ThemeDefinition>() { _newThemeType })
                },
                Target = _gameObject,
            },
        };*/
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public void AddHalo()
    {
        GameObject objPrefab = Resources.Load("ThingHaloRing") as GameObject;
        _haloObject = UnityEngine.Object.Instantiate(objPrefab) as GameObject;
        _haloObject.name = _gameObject.name + "Halo";
        _haloObject.AddComponent<MeshRenderer>();
        _haloObject.transform.SetParent(_gameObject.transform, true);
        _haloObject.transform.localScale = new Vector3(1, 1, 1);
        _haloObject.transform.eulerAngles = new Vector3(90, 0, 0);
        _haloObject.transform.position = new Vector3(0, 0, 0);
        _haloObject.GetComponent<Renderer>().material.color = new Color(0, 0, 255);
        _haloObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    private void AddSolver()
    {
        var _solverHandler = _gameObject.AddComponent<SolverHandler>();
        var _constantViewSize = _gameObject.AddComponent<ConstantViewSize>();

        //set track to camera view
        _solverHandler.TrackedTargetType = TrackedObjectType.Head;

        // The object take up this percent vertically in our view (not technically a percent use 0.5 for 50%)
        var x1 = _constantViewSize.TargetViewPercentV;
        // If the object is closer than MinDistance, the distance used is clamped here
        var x2 = _constantViewSize.MinDistance;
        // If the object is farther than MaxDistance, the distance used is clamped here
        var x3 = _constantViewSize.MaxDistance;
        // Minimum scale value possible (world space scale)
        var x4 = _constantViewSize.MinScale;
        // Maximum scale value possible (world space scale)
        var x5 = _constantViewSize.MaxScale;
        // Used for dead zone for scaling
        var x6 = _constantViewSize.ScaleBuffer;
        // Overrides auto size calculation with provided manual size. If 0, solver calculates size
        var x7 = _constantViewSize.ManualObjectSize;
        var x8 = _constantViewSize.ScaleState;
        // 0 to 1 between MinScale and MaxScale. If current is less than max, then scaling is being applied.
        // This value is subject to inaccuracies due to smoothing/interpolation/momentum.
        var x9 = _constantViewSize.CurrentScalePercent;
        // 0 to 1 between MinDistance and MaxDistance. If current is less than max, object is potentially on
        // a surface [or some other condition like interpolating] (since it may still be on surface, but scale
        // percent may be clamped at max).
        // This value is subject to inaccuracies due to smoothing/interpolation/momentum.
        var x10 = _constantViewSize.CurrentDistancePercent;
        // Returns the scale to be applied based on the FOV. This scale will be multiplied by distance as part
        // of the final scale calculation, so this is the ratio of vertical fov to distance.
        var x11 = _constantViewSize.FovScale;

        var x12 = _constantViewSize.MoveLerpTime;
        var x13 = _constantViewSize.RotateLerpTime;
        var x14 = _constantViewSize.ScaleLerpTime;

        //var x15 = _constantViewSize.MaintainScale;
        var x16 = _constantViewSize.Smoothing;
        //var x17 = _constantViewSize.Lifetime;
    }

    private void AddDirectionbIndicator()
    {
        //Microsoft.MixedReality.Toolkit.Experimental.Utilities.DirectionalIndicator
        //https://microsoft.github.io/MixedRealityToolkit-Unity/Documentation/README_Solver.html#directional-indicator
        //https://microsoft.github.io/MixedRealityToolkit-Unity/api/Microsoft.MixedReality.Toolkit.Experimental.Utilities.DirectionalIndicator.html
        //https://github.com/microsoft/MixedRealityToolkit-Unity/blob/mrtk_development/Assets/MixedRealityToolkit.Examples/Experimental/Solvers/DirectionalIndicatorExample.unity
        //https://docs.microsoft.com/en-us/windows/mixed-reality/holograms-210
        //https://forums.hololens.com/discussion/787/how-to-use-the-directionindicator-script
        //var toolTipSpawner = _gameObject.AddComponent<Directional>();
        //toolTipSpawner 
    }

    private void AddToolTip()
    {
        var toolTipSpawner = _gameObject.AddComponent<ToolTipSpawner>();
        //toolTipSpawner 
    }

    private void AddOnClick()
    {
        _interactableObject.OnClick.AddListener(() => Debug.Log("Interactable clicked"));
    }

    private void AddFocusEvents()
    {
        var onFocusReceiver = _interactableObject.AddReceiver<InteractableOnFocusReceiver>();

        onFocusReceiver.OnFocusOn.AddListener(
            () => Debug.Log("Focus on"));
        onFocusReceiver.OnFocusOff.AddListener(
            () => Debug.Log("Focus off"));
    }

    private void AddToggleEvents()
    {
        var toggleReceiver = _interactableObject.AddReceiver<InteractableOnToggleReceiver>();

        // Make the interactable have toggle capability, from code.
        // In the gui editor it's much easier
        _interactableObject.NumOfDimensions = 2;
        _interactableObject.CanSelect = true;
        _interactableObject.CanDeselect = true;

        toggleReceiver.OnSelect.AddListener(() => Debug.Log("Toggle selected"));
        toggleReceiver.OnDeselect.AddListener(() => Debug.Log("Toggle un-selected"));
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thing"></param>
    /// <returns></returns>
    //*************************************************************************

    public static ThingGameObject CreateGameObject(Thing thing)
    {
        ThingGameObject thingGameObject  = null;

        switch (thing.Type)
        {
            case Thing.TypeEnum.Uav:
                thingGameObject = new ThingUavObject();
                break;
            case Thing.TypeEnum.Person:
                thingGameObject = new ThingPersonObject();
                break;
            default:
                //thingGameObject = new ThingUnInitObject();
                thingGameObject = new ThingUavObject();       //TODO * Temporary
                break;
        }

        return thingGameObject;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thing"></param>
    //*************************************************************************

    public static void UpdateThing(Thing thing)
    {
        var gameObj = thing.GameObjectObject as ThingGameObject;
        gameObj?.Update(thing);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    //*************************************************************************

    private static double GetDistance(PointENU from, PointENU to)
    {
        return Math.Sqrt(
            Math.Pow((to.E - from.E), 2) + 
            Math.Pow((to.N - from.N), 2) + 
            Math.Pow((to.U - from.U), 2));
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    //*************************************************************************

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

    private static UnityEngine.Quaternion ConvertQuat(System.Numerics.Quaternion quatIn)
    {
        return new UnityEngine.Quaternion(quatIn.X, quatIn.Y, quatIn.Z, quatIn.W);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thing"></param>
    //*************************************************************************

    public virtual void Update(Thing thing)
    {
        _thing = thing;

        //find distance to 'self'
        thing.DistanceToObserver = GetDistance(ThingsManager.Self, _thing);

        //Let the ThingsManager know our distance so that it can
        //optimize the frustum size
        ThingsManager.ValidateFrustum(thing.DistanceToObserver);

        //x = E, y = U, z = N
        _gameObject.transform.position = new Vector3(
            Convert.ToSingle(_thing.Pose.PointEnu.E),
            Convert.ToSingle(_thing.Pose.PointEnu.U),
            Convert.ToSingle(_thing.Pose.PointEnu.N));

        _gameObject.transform.localRotation = ConvertQuat(_thing.Pose.Orient.Quat);

        UpdateHalo();
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    private void UpdateHalo()
    {
        float haloScale = 1;

        if (null != _haloObject)
        {
            // https://docs.unity3d.com/ScriptReference/Transform.LookAt.html
            if (ThingsManager.Self.GameObjectObject is ThingGameObject selfThing)
                if (null != selfThing._gameObject)
                {
                    _haloObject.transform.LookAt(selfThing._gameObject.transform);
                    //add another 90
                    _haloObject.transform.eulerAngles += new Vector3(90, 0, 0);

                    if (_thing.DistanceToObserver > 8)
                        haloScale = (float)_thing.DistanceToObserver / 8F;

                    _haloObject.transform.localScale = new Vector3(
                        haloScale,haloScale,haloScale);
                }
        }
    }
}

//*************************************************************************
/// <summary>
/// 
/// </summary>
//*************************************************************************

public class ThingUavObject : ThingGameObject
{
    private static int _count = 0;

    //*************************************************************************
    /// <summary>
    /// 
    /// https://forum.unity.com/threads/instantiating-objects-from-assets.18764/ 
    /// https://docs.unity3d.com/ScriptReference/Resources.Load.html
    /// https://docs.unity3d.com/ScriptReference/Object.Instantiate.html
    /// </summary>
    //*************************************************************************

    public ThingUavObject()
    {
        GameObject resourceObject = Resources.Load("ThingDrone") as GameObject;
        base._gameObject = UnityEngine.Object.Instantiate(resourceObject) as GameObject;
        base._gameObject.name = "UAV"  + _count++.ToString();
        base._gameObject.AddComponent<MeshRenderer>();
        base._gameObject.transform.localScale = new Vector3(1, 1, 1);
        base._gameObject.transform.position = new Vector3(0, 0, 0);
        base._gameObject.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        base._gameObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;

        AddHalo();
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thing"></param>
    //*************************************************************************
    public override void Update(Thing thing)
    {
        base.Update(thing);
    }
}

//*************************************************************************
/// <summary>
/// 
/// </summary>
//*************************************************************************

public class ThingPersonObject : ThingGameObject
{
    private static int _count = 0;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public ThingPersonObject()
    {
        base._gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        base._gameObject.name = "Person" + _count++.ToString();
        _gameObject.transform.localScale = new Vector3(1, 1, 1);

        //TODO * Assume person is self for now
        _gameObject.transform.SetParent(Camera.main.transform, false);

        //TODO * Do not render for now
    }
}

//*************************************************************************
/// <summary>
/// 
/// </summary>
//*************************************************************************

public class ThingUnInitObject : ThingGameObject
{
    private static int _count = 0;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public ThingUnInitObject()
    {
        base._gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        base._gameObject.name = "UAV" + _count++.ToString(); 
        _gameObject.transform.localScale = new Vector3(1, 1, 1);
        _gameObject.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        _gameObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;
    }
}
