using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using GeoLib;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
//using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.UI;
using NetStandardClassLibraryT1;
//using NetStandardClassLibraryT2;

using System.Runtime.InteropServices;

namespace T1
{
    public class T2
    {
        private const string DLL = "__Internal";
        [DllImport(DLL)]
        public static extern int
            CountLettersInString([MarshalAs(UnmanagedType.LPWStr)]string str);
    }
}

#region ThingsManager

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

    //TODO * For now, terrain is just a flat plane with a point at 0,0,0
    private static Plane _horizontalPlane = new Plane(Vector3.up, new Vector3(0,-4,0));

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

    public static Plane HorizontalPlane
    {
        get => _horizontalPlane;
    }

    private void LogThis(Exception ex, string prefix, bool showOnConsole)
    {
        if (showOnConsole)
            Debug.Log(ex.Message);

        Debug.unityLogger.LogException(ex, this);
    }

    private void LogThis(string message, bool showOnConsole)
    {
        if (showOnConsole)
            Debug.Log(message);

        Debug.unityLogger.logHandler.LogFormat(LogType.Exception, this, message);
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

        //var t1 = new T1.T2();
        //var lis = T1.T2.CountLettersInString("1234");

        MyUtilities utils = new MyUtilities();
        utils.AddValues(2, 3);
        print("2 + 3 = " + utils.c);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Update()
    {
        //return;//TODO Temp

        if (null == _TM)
        {
            LogThis("ThingsManager.Update _TM == null", true);
            return;
        }

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
    ///We translate objects from real world geo coords to local game coords
    ///in the ENU ref. At the beginning of operation, the main camera is 
    ///pointing due north in ENU. In order to make the main camera pose
    ///match the real world pose we need to rotate it. We calculate the
    ///required rotation from the orient value of the pose given to us by
    ///the self object (which is a MPU/compass in the real world).
    /// </summary>
    /// <param name="pose"></param>
    //*************************************************************************
    void AlignToCompass(ThingPose pose)
    {
        //filter out noise, only adjust when necessary

        //set the rotation of the play space to true compass
        MixedRealityPlayspace.Rotation = Quaternion.Euler(Vector3.up * (float)pose.Orient.True);
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

        // set the rotation of world space to true compass of self
        AlignToCompass(thing.Pose);

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
            ThingGameObject.SetThing(thing);
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

#endregion //ThingsManager

#region ThingGameObject

//*************************************************************************
/// <summary>
/// 
/// </summary>
//*************************************************************************

public class ThingGameObject
{
    // TODO * we need to adjust this dynamically to match transmission rate
    private static float DefaultLerpTimeSpan = 1.0f;

    protected Thing _thing = null;
    protected GameObject _gameObject = null;
    protected GameObject _haloObject = null;
    protected GameObject _gimbal = null;
    protected GameObject _gimbalCameraHousing = null;
    protected GameObject _gimbalCameraGazeHitpoint = null;
    protected Camera _gimbalCamera = null;
    protected Interactable _interactableObject = null;
    protected ThemeDefinition _newThemeType;
    protected BoxCollider _boxCollider;

    protected float _lerpStartTime;
    protected bool _gotLerpStart = false;
    protected Vector3 _lerpStartPosition;
    protected Vector3 _lerpEndPosition;
    protected float _lerpTimeSpan;

    protected HoloToolkit.Unity.DirectionIndicator _directionIndicator;

    //The position of the gimbal camera's gaze intersection with the terrain
    Vector3 _gimbalCameraGazeTerrainIntersctionPoint;
    bool _haveGimbalCameraGazeTerrainIntersctionPoint = false;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public ThingGameObject()
    {
        _gameObject = new GameObject();

        _lerpTimeSpan = DefaultLerpTimeSpan;

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

    public void AddDirectionIndicator()
    {
        _directionIndicator = _gameObject.AddComponent<HoloToolkit.Unity.DirectionIndicator>();
        GameObject objPrefab = Resources.Load("Chevron") as GameObject;
        _directionIndicator.DirectionIndicatorObject = objPrefab;
        _directionIndicator.Cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _directionIndicator.Cursor.transform.position = new Vector3(0,0,20.0f);
        _directionIndicator.Cursor.transform.localScale = new Vector3(.1f,.1f,.1f);
        _directionIndicator.MetersFromCursor = 2f;

        var rv = _directionIndicator.Cursor.AddComponent<
            Microsoft.MixedReality.Toolkit.Utilities.Solvers.RadialView>();
        rv.MinDistance = 19.0f;
        rv.MaxDistance = 21.0f;
        rv.MinViewDegrees = 1.0f;
        rv.MaxViewDegrees = 5.0f;
        rv.MoveLerpTime = 0.5f;
        rv.RotateLerpTime = 0.5f;

        _directionIndicator.Awake2();
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
        _haloObject.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        _haloObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(255, 0, 0));
        _haloObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public void AddGimbalCameraGazeHitpoint()
    {
        _gimbalCameraGazeHitpoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _gimbalCameraGazeHitpoint.name = "Hitpoint";
        _gimbalCameraGazeHitpoint.transform.localScale = new Vector3(1, 1, 1);
        _gimbalCameraGazeHitpoint.transform.position = new Vector3(0, 0, 0);
        _gimbalCameraGazeHitpoint.AddComponent<MeshRenderer>();
        _gimbalCameraGazeHitpoint.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        _gimbalCameraGazeHitpoint.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;
        _gimbalCameraGazeHitpoint.SetActive(false);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public ThingLandingPadObject AddLandingPad(Vector3 uavPosition)
    {
        ThingLandingPadObject landingPad = null;

        //Find the spot on the terrain below the UAV.
        //The arguments are the position of the UAV, and the down vector
        if (FindRayTerrainIntersection(
            uavPosition,
            Vector3.down,
            out var padPosition))
        {
            landingPad = new ThingLandingPadObject();
            landingPad._gameObject.transform.position = padPosition;
        }

        return landingPad;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public ThingBreadcrumbObject AddBreadcrumb(Vector3 uavPosition)
    {
        ThingBreadcrumbObject breadcrumb = null;

        //Find the spot on the terrain below the UAV.
        //The arguments are the position of the UAV, and the down vector
        if (FindRayTerrainIntersection(
            uavPosition,
            Vector3.down,
            out var padPosition))
        {
            breadcrumb = new ThingBreadcrumbObject();
            breadcrumb._gameObject.transform.position = padPosition;
        }

        return breadcrumb;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public void SetupGimbal()
    {
        var xform = _gameObject.transform.Find("Gimbal");

        if (null == xform)
            return;

        _gimbal = xform.gameObject;

        if (null == _gimbal) 
            return;

        xform = _gimbal.transform.Find("CameraHousing");

        if (null == xform)
            return;

        if (null == xform.gameObject)
            return;

        _gimbalCameraHousing = xform.gameObject;

        xform = _gimbalCameraHousing.transform.Find("Camera");

        if (null == xform)
            return;

        if (null == xform.gameObject)
            return;

        _gimbalCamera = xform.gameObject.GetComponent<Camera>();

        //---------------

        var pirt = GameObject.Find("PipImageRenderTexture");

        var texture = RenderTexture.FindObjectsOfType< RenderTexture>();
        
        /*var texture = RenderTexture.FindObjectsOfType(typeof(RenderTexture));

        if (texture)
            Debug.Log("GUITexture object found: " + texture.name);
        else
            Debug.Log("No GUITexture object could be found");*/

        //---------------------------------

        _gimbalCamera.targetTexture = texture[0];

        AddGimbalCameraGazeHitpoint();
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

    public static void SetThing(Thing thing)
    {
        var gameObj = thing.GameObjectObject as ThingGameObject;
        gameObj?.Set(thing);
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

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="quatIn"></param>
    /// <returns></returns>
    //*************************************************************************

    private static UnityEngine.Quaternion ConvertQuat(System.Numerics.Quaternion quatIn)
    {
        return new UnityEngine.Quaternion(quatIn.X, quatIn.Y, quatIn.Z, quatIn.W);
    }

    public static Vector3 ConvertPoint(PointENU pointENU)
    {
        return new Vector3(
            Convert.ToSingle(pointENU.E), 
            Convert.ToSingle(pointENU.U),
            Convert.ToSingle(pointENU.N));
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thing"></param>
    /// <returns></returns>
    //*************************************************************************

    private bool MovedMuch(Thing thing)
    {
        if (.1 <
            Math.Pow((_thing.Pose.PointEnu.E - _lerpStartPosition.x), 2) +
            Math.Pow((_thing.Pose.PointEnu.U - _lerpStartPosition.y), 2) +
            Math.Pow((_thing.Pose.PointEnu.N - _lerpStartPosition.z), 2))
            return true;

        //return false;

        //*** TODO * Check other changes, like frame orientation and gimbal
        return true;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thing"></param>
    //*************************************************************************

    public virtual void Set(Thing thing)
    {
        _thing = thing;

        if (thing.Pose.CheckForNewData())
        {
            //we have a position update. Start a lerp

            //first time through we don't have a start, so start and end are the
            //same. Otherwise, set new start pose to last end pose
            if (!_gotLerpStart)
            {
                _lerpStartPosition.x = Convert.ToSingle(_thing.Pose.PointEnu.E);
                _lerpStartPosition.y = Convert.ToSingle(_thing.Pose.PointEnu.U);
                _lerpStartPosition.z = Convert.ToSingle(_thing.Pose.PointEnu.N);

                _gotLerpStart = true;
            }
            else
            {
                //if we haven't moved much, don't update position
                if (!MovedMuch(thing))
                    return;

                _lerpStartPosition = _lerpEndPosition;
            }

            _lerpEndPosition.x = Convert.ToSingle(_thing.Pose.PointEnu.E);
            _lerpEndPosition.y = Convert.ToSingle(_thing.Pose.PointEnu.U);
            _lerpEndPosition.z = Convert.ToSingle(_thing.Pose.PointEnu.N);

            _lerpStartTime = UnityEngine.Time.time;

            //Let the ThingsManager know our distance so that it can
            //optimize the frustum size
            ThingsManager.ValidateFrustum(thing.DistanceToObserver);
        }

        var difftime = UnityEngine.Time.time - _lerpStartTime;
        
        //make sure difftime does not equal zero
        if (0 == difftime)
            difftime = .001f;

        var lerpVal = difftime / _lerpTimeSpan;

        //if lerpVal > 1, then we have fully moved and don't have new pose data
        //so save power and leave
        if (lerpVal > 1.1)
            return;

        //find distance to 'self'
        thing.DistanceToObserver = GetDistance(ThingsManager.Self, _thing);

        //x = E, y = U, z = N
        _gameObject.transform.position = Vector3.Lerp(
            _lerpStartPosition, _lerpEndPosition, lerpVal );

        _gameObject.transform.localRotation = ConvertQuat(_thing.Pose.Orient.Quat);

        SetHalo();
        SetGimbal(ConvertQuat(_thing.Pose.GimbalOrient.Quat));
        SetGimbalCamera(_gimbalCamera);

        //AddLandingPad(ConvertPoint(this._thing.Pose.PointEnu));
    }

    //*************************************************************************
    /// <summary>
    /// Find the intersection of a camera's gaze and the terrain.
    /// </summary>
    /// <param name="origin"></param> The position of the camera
    /// <param name="direction"></param> The orientation of the camera
    /// <param name="intersectionPoint"></param> Hitpoint
    /// <returns></returns>
    //*************************************************************************
    private bool FindRayTerrainIntersection(Vector3 origin, 
        Vector3 direction,out Vector3 intersectionPoint)
    {
        Ray ray = new Ray(origin, direction);

        //TODO * For now, terrain is just a flat plane with a point at 0,0,0
        if (ThingsManager.HorizontalPlane.Raycast(ray, out var distance))
        {
            intersectionPoint = ray.GetPoint(distance);
            return true;
        }

        intersectionPoint = new Vector3(0,0,0);
        return false;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="hitpoint"></param>
    /// <param name="active"></param>
    //*************************************************************************
    private void SetGimbalCameraGazeHitpoint(Vector3 hitpoint, bool active)
    {
        if (null == _gimbalCameraGazeHitpoint)
            return;

        _gimbalCameraGazeHitpoint.SetActive(active);

        if(active)
            _gimbalCameraGazeHitpoint.transform.position = hitpoint;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="gimbalCamera"></param>
    //*************************************************************************
    private void SetGimbalCamera(Camera gimbalCamera)
    {
        if (null == gimbalCamera)
            return;

        //Find the intersection of the gimbal camera's gaze and the terrain.
        //The arguments are the position of the gimbal camera, and the
        //world space rotation of the gimbal (to which the camera is mounted)
        _haveGimbalCameraGazeTerrainIntersctionPoint = FindRayTerrainIntersection(
            gimbalCamera.transform.position,
            gimbalCamera.transform.forward,
            out _gimbalCameraGazeTerrainIntersctionPoint);

        SetGimbalCameraGazeHitpoint(
            _gimbalCameraGazeTerrainIntersctionPoint,
            _haveGimbalCameraGazeTerrainIntersctionPoint);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    private void SetGimbal(UnityEngine.Quaternion orient)
    {
        var go = _gameObject.transform.Find("Gimbal");
        
        var gimbal = go?.gameObject;

        if (null != gimbal)
            _gimbal.transform.localRotation = orient;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    private void SetHalo()
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
                        haloScale, haloScale, haloScale);
                }
        }
    }
}

#endregion //ThingGameObject

#region ThingUavObject

//*************************************************************************
/// <summary>
/// 
/// </summary>
//*************************************************************************

public class ThingUavObject : ThingGameObject
{
    private bool _autoLandingPad = true;
    private bool _leaveBreadcrumbs = false;

    private static int _count = 0;
    private ThingLandingPadObject _landingPad = null;
    private List<ThingBreadcrumbObject> _breadcrumbs = new List<ThingBreadcrumbObject>();

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
        //GameObject resourceObject = Resources.Load("ThingDrone") as GameObject;
        GameObject resourceObject = Resources.Load("UavObject") as GameObject;
        base._gameObject = UnityEngine.Object.Instantiate(resourceObject) as GameObject;
        base._gameObject.name = "UAV" + _count++.ToString();
        base._gameObject.AddComponent<MeshRenderer>();
        base._gameObject.transform.localScale = new Vector3(1, 1, 1);
        base._gameObject.transform.position = new Vector3(0, 0, 0);
        base._gameObject.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        base._gameObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;

        AddHalo();
        SetupGimbal();
        AddDirectionIndicator();
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thing"></param>
    //*************************************************************************
    public override void Set(Thing thing)
    {
        base.Set(thing);

        if (thing.Pose?.PointEnu != null)
        {
            if (_autoLandingPad) if (null == _landingPad)
                _landingPad = AddLandingPad(ConvertPoint(thing.Pose.PointEnu));
            if (_leaveBreadcrumbs)
                _breadcrumbs.Add(AddBreadcrumb(ConvertPoint(thing.Pose.PointEnu)));
        }
    }
}

#endregion //ThingUavObject

#region ThingLandingPadObject

//*************************************************************************
/// <summary>
/// 
/// </summary>
//*************************************************************************

public class ThingLandingPadObject : ThingGameObject
{
    private static int _count = 0;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public ThingLandingPadObject()
    {
        //GameObject resourceObject = Resources.Load("ThingDrone") as GameObject;
        GameObject resourceObject = Resources.Load("LandingPad") as GameObject;
        base._gameObject = UnityEngine.Object.Instantiate(resourceObject) as GameObject;
        base._gameObject.name = "LANDINGPAD" + _count++.ToString();
        base._gameObject.AddComponent<MeshRenderer>();
        base._gameObject.transform.localScale = new Vector3(2, 2, 2);
        base._gameObject.transform.position = new Vector3(0, 0, 0);
        //base._gameObject.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        base._gameObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;
    }

    public ThingLandingPadObject(Vector3 position)
    {
        //GameObject resourceObject = Resources.Load("ThingDrone") as GameObject;
        GameObject resourceObject = Resources.Load("LandingPad") as GameObject;
        base._gameObject = UnityEngine.Object.Instantiate(resourceObject) as GameObject;
        base._gameObject.name = "LANDINGPAD" + _count++.ToString();
        base._gameObject.AddComponent<MeshRenderer>();
        base._gameObject.transform.localScale = new Vector3(4, 4, 4);
        base._gameObject.transform.position = position;
        //base._gameObject.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        base._gameObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thing"></param>
    //*************************************************************************
    public override void Set(Thing thing)
    {
        base.Set(thing);
    }
}

#endregion //ThingLandingPadObject

#region ThingBreadcrumbObject

//*************************************************************************
/// <summary>
/// 
/// </summary>
//*************************************************************************

public class ThingBreadcrumbObject : ThingGameObject
{
    private static int _count = 0;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public ThingBreadcrumbObject()
    {
        //GameObject resourceObject = Resources.Load("ThingDrone") as GameObject;
        GameObject resourceObject = Resources.Load("Breadcrumb") as GameObject;
        base._gameObject = UnityEngine.Object.Instantiate(resourceObject) as GameObject;
        base._gameObject.name = "LANDINGPAD" + _count++.ToString();
        base._gameObject.AddComponent<MeshRenderer>();
        base._gameObject.transform.localScale = new Vector3(.5f, .5f, .5f);
        base._gameObject.transform.position = new Vector3(0, 0, 0);
        //base._gameObject.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        base._gameObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;
    }

    public ThingBreadcrumbObject(Vector3 position)
    {
        //GameObject resourceObject = Resources.Load("ThingDrone") as GameObject;
        GameObject resourceObject = Resources.Load("Breadcrumb") as GameObject;
        base._gameObject = UnityEngine.Object.Instantiate(resourceObject) as GameObject;
        base._gameObject.name = "BREADCRUMB" + _count++.ToString();
        base._gameObject.AddComponent<MeshRenderer>();
        base._gameObject.transform.localScale = new Vector3(.5f, .5f, .5f);
        base._gameObject.transform.position = position;
        //base._gameObject.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        base._gameObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thing"></param>
    //*************************************************************************
    public override void Set(Thing thing)
    {
        base.Set(thing);
    }
}

#endregion //ThingBreadcrumbObject

#region ThingPersonObject

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
        _gameObject.transform.localScale = new Vector3(.1f, 1, .1f);

        //TODO * Assume person is self for now
        _gameObject.transform.SetParent(Camera.main.transform, false);

        //TODO * Do not render for now
    }
}

#endregion //ThingPersonObject

#region ThingUninitObject

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

#endregion //ThingGameObject
