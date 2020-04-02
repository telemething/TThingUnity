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
//using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.UI;
using NetStandardClassLibraryT1;
//using NetStandardClassLibraryT2;

using System.Runtime.InteropServices;
using Microsoft.MixedReality.Toolkit.Examples.Demos.EyeTracking.Logging;
using T1;
using System.Diagnostics;
using System.Threading;


namespace T1
{
    //*************************************************************************
    //* This class references C++ code defined in NativeFunctions.cpp. This 
    //* provides a convenient place to set breakpoints in the code generated
    //* by IL2CPP.
    //*************************************************************************

    public class CLogger
    {
#if UNITY_EDITOR
        public static int CountLettersInString(string str)
        { return 0; }

        public static void LogThis(string str)
        { }
#else
        private const string DLL = "__Internal";
        [DllImport(DLL)]
        public static extern int
            CountLettersInString([MarshalAs(UnmanagedType.LPWStr)]string str);

        [DllImport(DLL)]
        public static extern void
            LogThis([MarshalAs(UnmanagedType.LPWStr)]string str);
#endif
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
    //private T1.CLogger _cLogger;

    public int Port = (int)AppSettings.App.ThingsManagerSettings.TelemetryPort.Value;
    public string MyThingId = (string)AppSettings.App.SelfSettings.MyThingId.Value;

    public UnityEngine.UI.Text TextObject;
    private PointLatLonAlt _origin = null;
    private static Thing _self = null;
    private UnityEngine.UI.Text _infoTextLarge = null;

    private static double _maxMaxThingDistance = 50.0;
    private static double _minMinThingDistance = 0.3;

    private static double _maxThingDistance = _maxMaxThingDistance;
    private static double _minThingDistance = _minMinThingDistance;

    private GameObject _terrain = null;
    public static GameObject MainTerrain = null;

    private bool _gotMainCameraAltitudeOverTerrain = false;
    private float _mainCameraAltitudeOverTerrain = 0f;
    private float _mainCameraAltitudeOverTerrainOffset = 
        (float)AppSettings.App.SelfSettings.MainCameraAltitudeOverTerrainOffset.Value;

    public enum TerrainStateEnum { uninit, placing, placedLatLon, placed }
    private TerrainStateEnum _terrainState = TerrainStateEnum.uninit;

    int zoomLevel = (int)AppSettings.App.TerrainSettings.TerrainZoomLevel.Value;
    int tilesPerSide = (int)AppSettings.App.TerrainSettings.TerrainTilesPerSide.Value;

    private ThingMotion _TM;

    //TODO * For now, terrain is just a flat plane with a point at 0,0,0
    private static Plane _horizontalPlane = new Plane(Vector3.up, new Vector3(0, -4, 0));

    public enum CompassAlignmentStatusEnum { Unaligned, Aligning, Showing, Aligned, Broken }
    CompassAlignmentStatusEnum _compassAlignmentStatus = CompassAlignmentStatusEnum.Unaligned;
    Stopwatch _compassAlignementTimer = new Stopwatch();
    long _compassAlignementTimeSpanMs = 5000;
    long _compassAlignementShowingTimeSpanMs = 2000;
    float _compassAlignementCameraStartingRotation = 0;
    float _compassAlignementCameraMaxAllowedRotation = 2;
    double _compassAlignementCompassReadingSum = 0;
    int _compassAlignementCompassReadingCount = 0;

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
            UnityEngine.Debug.Log(ex.Message);

        UnityEngine.Debug.unityLogger.LogException(ex, this);
    }

    private void LogThis(string message, bool showOnConsole)
    {
        if (showOnConsole)
            UnityEngine.Debug.Log(message);

        UnityEngine.Debug.unityLogger.logHandler.LogFormat(LogType.Exception, this, message);
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

        T1.CLogger.LogThis("ThingsManager.Start()");

        _infoTextLarge = Utils.FindObjectComponentInScene<UnityEngine.UI.Text>("InfoTextLarge");

        //MyUtilities utils = new MyUtilities();
        //utils.AddValues(2, 3);
        //print("2 + 3 = " + utils.c);

        //TestLerc();

        var terrains = Utils.FindObjectsInScene("Terrain");

        if (terrains.Count != 0)
        {
            _terrain = terrains[0];
            MainTerrain = _terrain;
        }

        //allow raycast to hit underside of terrain mesh
        Physics.queriesHitBackfaces = true;
    }

    static float _manualDeclinationChange = 0f;

    public static float ManualDeclinationChange
        { set { _manualDeclinationChange = value; } }

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

        //T1.CLogger.LogThis("ThingsManager.Update()");

        things?.ForEach(thing => 
        {
            try
            {
                if (null == thing)
                    return;
                if (thing.Self == Thing.SelfEnum.Self)
                    SetSelf(thing);
                else
                    SetOther(thing);
            }
            catch(Exception ex)
            {
                var ff = ex.Message;
            }
        });

        if(0f != _manualDeclinationChange)
        {
            //rotate playspace by diff angle
            MixedRealityPlayspace.Rotation *= Quaternion.Euler(Vector3.up * _manualDeclinationChange);
            _manualDeclinationChange = 0f;
        }
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="altitude"></param>
    /// <returns></returns>
    //*********************************************************************
    bool TryGetAltitudeOverTerrain(out float altitude)
    {
        altitude = 0;
        RaycastHit hit;

        if (null == _terrain)
            return false;

        int layerMask = 1 << _terrain.layer;

        // look down
        if (Physics.Raycast(Camera.main.transform.position, Vector3.down,
            out hit, Mathf.Infinity, layerMask))
        {
            altitude = hit.distance;
            return true;
        }

        // look up
        if (Physics.Raycast(Camera.main.transform.position, Vector3.up,
            out hit, Mathf.Infinity, layerMask))
        {
            altitude = -hit.distance;
            return true;
        }

        return false;
    }

    private bool FindRayTerrainIntersection(Vector3 origin,
    Vector3 direction, out Vector3 intersectionPoint)
    {
        Ray ray = new Ray(origin, direction);

        /*if (_useFlatTerrain)
        {
            //Use a terrain which is just a flat plane with a point at 0,0,0
            if (ThingsManager.HorizontalPlane.Raycast(ray, out var distance))
            {
                intersectionPoint = ray.GetPoint(distance);
                return true;
            }
        }
        else*/
        {
            //Use the terrain which was built from GEO data
            int layerMask = 1 << ThingsManager.MainTerrain.layer;
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                intersectionPoint = hit.point;
                return true;
            }
        }

        intersectionPoint = new Vector3(0, 0, 0);
        return false;
    }


    public bool HeightAboveTerrain(Vector3 objectPosition, out float height)
    {
        Vector3 terrainHitPosition;
        height = 0;

        //Find the spot on the terrain below the object.
        //The arguments are the position of the UAV, and the down vector
        if (FindRayTerrainIntersection(
            objectPosition,
            Vector3.down,
            out terrainHitPosition))
        {
            height = objectPosition.y - terrainHitPosition.y;
            return true;
        }

        //Find the spot on the terrain above the object.
        //The arguments are the position of the UAV, and the up vector
        if (FindRayTerrainIntersection(
            objectPosition,
            Vector3.up,
            out terrainHitPosition))
        {
            height = objectPosition.y - terrainHitPosition.y;
            return true;
        }

        return false;
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    //*********************************************************************
    static byte[] GetData(string fileName)
    {
        try
        {
            System.IO.FileStream fs = System.IO.File.Open(fileName, System.IO.FileMode.Open);

            long fileSize = fs.Length;
            byte[] fileData = new byte[fileSize];
            fs.Read(fileData, 0, (int)fileSize);

            return fileData;
        }
        catch (Exception ex)
        {
            throw new Exception("GetData() : " + ex.Message);
        }
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="encodedData"></param>
    /// <returns></returns>
    //*********************************************************************
    static object[] Decode(byte[] encodedData)
    {
        try
        {
            //LercLibNet.Lerc c1 = new LercLibNet.Lerc();

            var encodedDataSize = encodedData.Length;
            int infoArraySize = 10;
            uint[] infoArray = new uint[infoArraySize];
            int dataRangeArraySize = 10;
            double[] dataRangeArray = new double[dataRangeArraySize];

            //c1.GetBlobInfo(encodedData, (uint)encodedDataSize, infoArray,
            //    dataRangeArray, infoArraySize, dataRangeArraySize);
            
            Esri.PrototypeLab.HoloLens.Unity.LercDecoder.lerc_getBlobInfo(
                encodedData, (uint)encodedDataSize, infoArray,
                dataRangeArray, infoArraySize, dataRangeArraySize);

            uint version = infoArray[0];
            uint dataType = infoArray[1];
            uint nDim = infoArray[2];
            uint nCols = infoArray[3];
            uint nRows = infoArray[4];
            uint nBands = infoArray[5];
            uint nValidPixels = infoArray[6];
            uint blobSize = infoArray[7];

            var validBytesMask = new byte[nCols * nRows];
            object[] decodedData = null;

            switch (dataType)
            {
                case 0:
                    //char
                    throw new Exception("Data type 'char' not implemented");
                    break;
                case 1:
                    //uchar
                    throw new Exception("Data type 'uchar' not implemented");
                    break;
                case 2:
                    //short
                    throw new Exception("Data type 'short' not implemented");
                    break;
                case 3:
                    //ushort
                    throw new Exception("Data type 'ushort' not implemented");
                    break;
                case 4:
                    //int
                    throw new Exception("Data type 'int' not implemented");
                    break;
                case 5:
                    //uint
                    throw new Exception("Data type 'uint' not implemented");
                    break;
                case 6:
                    //float
                    var decodedDataF = new float[nCols * nRows];
                    //c1.Decode(encodedData, (uint)encodedDataSize, validBytesMask,
                    //    (int)nDim, (int)nCols, (int)nRows, (int)nBands, dataType, decodedDataF);
                    Esri.PrototypeLab.HoloLens.Unity.LercDecoder.lerc_decode(encodedData, (uint)encodedDataSize, validBytesMask,
                        (int)nDim, (int)nCols, (int)nRows, (int)nBands, (int)dataType, decodedDataF);
                    decodedData = Array.ConvertAll(decodedDataF, x => (object)x);
                    break;
                case 7:
                    //double
                    throw new Exception("Data type 'double' not implemented");
                    break;
            }

            return decodedData;
        }
        catch (Exception ex)
        {
            throw new Exception("Decode() : " + ex.Message);
        }
    }


    void TestLerc()
    {
        try
        {
            string fileName = Application.dataPath + "\\742";
            var decoded = Decode(GetData(fileName));

            if (null == decoded)
            {
                Console.WriteLine("Decoded data is NULL");
                return;
            }

            if (0 == decoded.Length)
            {
                Console.WriteLine("Decoded data length == 0");
                return;
            }

            Console.WriteLine("Decoded data: type: {0}, length: {1}",
                decoded[0].GetType().ToString(), decoded.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.Message);
        }
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
        Quaternion compassOrientation;
        Quaternion cameraOrientation;

        switch (_compassAlignmentStatus)
        {
            case CompassAlignmentStatusEnum.Aligned:
                //TODO * Check if we have drifted out of alignment
                return;

            case CompassAlignmentStatusEnum.Showing:

                if (_compassAlignementShowingTimeSpanMs < 
                    _compassAlignementTimer.ElapsedMilliseconds)
                {
                    _infoTextLarge.text = "";
                    _compassAlignmentStatus = CompassAlignmentStatusEnum.Aligned;
                    _compassAlignementTimer.Stop();
                }

                return;

            case CompassAlignmentStatusEnum.Unaligned:

                _infoTextLarge.text = "Aligning Orientation. Hold Still";
                _compassAlignmentStatus = CompassAlignmentStatusEnum.Aligning;
                _compassAlignementCameraStartingRotation = Camera.main.transform.rotation.eulerAngles.y;
                _compassAlignementTimer.Restart();
                return;

            case CompassAlignmentStatusEnum.Aligning:

                compassOrientation = Quaternion.Euler(Vector3.up * (float)pose.Orient.True);
                cameraOrientation = Camera.main.transform.rotation;

                //if not enough still time has passed, add readings, show progress
                if (_compassAlignementTimeSpanMs > _compassAlignementTimer.ElapsedMilliseconds)
                {
                    _infoTextLarge.text = $"Aligning Orientation. Hold Still\nMag: {Math.Round(pose.Orient.True, 2)}\nCam: {Math.Round(cameraOrientation.eulerAngles.y, 2)}";

                    //if not still, start over
                    if(_compassAlignementCameraMaxAllowedRotation < 
                        Math.Abs(cameraOrientation.eulerAngles.y - 
                        _compassAlignementCameraStartingRotation))
                    {
                        _compassAlignementCompassReadingSum = 0;
                        _compassAlignementCompassReadingCount = 0;
                        _compassAlignementCameraStartingRotation = 
                            Camera.main.transform.rotation.eulerAngles.y;
                        _compassAlignementTimer.Restart();
                        return;
                    }

                    _compassAlignementCompassReadingSum += pose.Orient.True;
                    _compassAlignementCompassReadingCount++;
                    return;
                }

                //Enough still time has passed

                //find average of mag readings taken during cal time
                var MagAngleAvg = (float)(_compassAlignementCompassReadingSum / 
                    (float)_compassAlignementCompassReadingCount);

                //convert to quat
                compassOrientation = Quaternion.Euler(Vector3.up * MagAngleAvg);

                //find diff between compass and camera rotation
                var DiffRotation = compassOrientation * Quaternion.Inverse(cameraOrientation);

                //rotate playspace by diff angle
                MixedRealityPlayspace.Rotation = DiffRotation;

                _infoTextLarge.text = $"Aligned : {Math.Round(MagAngleAvg, 2)} deg.";

                _compassAlignmentStatus = CompassAlignmentStatusEnum.Showing;
                _compassAlignementTimer.Restart();

                return;

            case CompassAlignmentStatusEnum.Broken:
                return;
        }


        //filter out noise, only adjust when necessary

        //set the rotation of the play space to true compass

        //temporary disable, need to reenable with more context
        MixedRealityPlayspace.Rotation = Quaternion.Euler(Vector3.up * (float)pose.Orient.True);
    }

    //*************************************************************************
    /// <summary>
    /// Set values of self object, once per frame update
    /// </summary>
    /// <param name="thing"></param>
    //*************************************************************************
    void SetSelf(Thing thing)
    {
        if (_terrainState == TerrainStateEnum.placedLatLon)
        {
            _gotMainCameraAltitudeOverTerrain =
                HeightAboveTerrain(Camera.main.transform.position, 
                out _mainCameraAltitudeOverTerrain);

            if (_gotMainCameraAltitudeOverTerrain)
            {
                //move terrain by offset
                _terrain.transform.position +=
                    new Vector3(0, _mainCameraAltitudeOverTerrain - 
                    _mainCameraAltitudeOverTerrainOffset, 0);
            }

            _terrainState = TerrainStateEnum.placed;
        }

        // set the origin to the first reported location of self
        if (null == _origin)
            if (thing.Pose.PointGeoUsable == ThingPose.PointGeoUsableEnum.Yes)
            {
                SetOrigin(thing.Pose);

                if (_terrainState == TerrainStateEnum.uninit)
                {
                    _terrainState = TerrainStateEnum.placing;

                    //place the terrain, centered around my position
                    PlaceTerrain(new PointLatLonAlt(thing.Pose.PointGeo.Lat, 
                        thing.Pose.PointGeo.Lon, thing.Pose.PointGeo.Alt + 1000f),
                        zoomLevel, tilesPerSide);

                    _terrainState = TerrainStateEnum.placedLatLon;
                }
            }

        if (null == thing.GameObjectObject)
            thing.GameObjectObject = ThingGameObject.CreateGameObject(thing);

        // set the rotation of world space to true compass of self
        AlignToCompass(thing.Pose);

        _self = thing;
    }

    //*************************************************************************
    /// <summary>
    /// Set values of non self objects, once per frame update
    /// </summary>
    /// <param name="thing"></param>
    //*************************************************************************
    void SetOther(Thing thing)
    {
        //dont do anything if we don't yet have a terrain
        if (_terrainState != TerrainStateEnum.placed)
            return;

        if (null == thing.GameObjectObject)
            thing.GameObjectObject = ThingGameObject.CreateGameObject(thing);
        else
            ThingGameObject.SetThing(thing);
    }

    //*************************************************************************
    /// <summary>
    /// Make sure frustum contains all objects, adjust accordingly
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
        //Don't move farClipPlane closer than terrain set in PlaceTerrain()
    }

    //*************************************************************************
    /// <summary>
    /// Fetch and dispaly terrain
    /// </summary>
    /// <param name="trueToShow">Lat lon coords at center of tiles to fetch</param>
    /// <param name="zoomLevel">Zoom levelof tiles to fetch</param>
    /// <param name="tilesPerSide">Number of tiles per edge, total number of
    /// tiles to fetch and display = (tilesPerSide+1)^2</param>
    //*************************************************************************
    public static void PlaceTerrain(PointLatLonAlt coords,
        int zoomLevel, int tilesPerSide)
    {
        var terrains = Utils.FindObjectsInScene("Terrain");

        if (terrains.Count == 0)
            return;

        var mb = terrains[0]?.GetComponent<MapBuilder>();

        if (null == mb)
            return;

        var zoom = mb.ZoomLevel; //* TODO * make UI adjustable
        var size = mb.MapSize;   //* TODO * make UI adjustable

        //fetch and place the map
        mb.ShowMap((float)coords.Lat, (float)coords.Lon, zoomLevel, (int)tilesPerSide);

        Camera.main.farClipPlane = Math.Max(
            Camera.main.farClipPlane, mb.MapTotal3DDiagonalLength/2);

        //find the top left coords of the center tile
        var CenterTileTopLeft = new PointLatLonAlt(
            mb.CenterTileTopLeftLatitude, mb.CenterTileTopLeftLongitude, 0);

        //find the x and z offset from my origin to tile top left
        var offset = GeoLib.GpsUtils.GeodeticToEnu(CenterTileTopLeft, coords);

        //move the map by the offset
        terrains[0].transform.position += PointENU.ToVector3(offset);
    }

    //*************************************************************************
    /// <summary>
    /// Display or hide terrain
    /// </summary>
    /// <param name="trueToShow"></param>
    //*************************************************************************
    public static void ShowTerrain(bool trueToShow)
    {
        var dml = Utils.FindObjectsInScene("Terrain");

        if (dml.Count == 0)
            return;

        dml[0]?.SetActive(trueToShow);
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
    private static float DefaultLerpTimeSpan = (float)AppSettings.App.GameObjectSettings.DefaultLerpTimeSpan.Value;

    // If true, will use a flat plane, otherwise will use a GEO plane
    private bool _useFlatTerrain = (bool)AppSettings.App.GameObjectSettings.UseFlatTerrain.Value;

    private float _haloScaleFactor = (float)AppSettings.App.GameObjectSettings.HaloScaleFactor.Value;

    protected Thing _thing = null;
    protected GameObject _gameObject = null;
    protected HaloObject _haloObject = null;
    protected GameObject _gimbal = null;
    protected GameObject _gimbalCameraHousing = null;
    protected ThingGazeHitpointObject _gimbalCameraGazeHitpoint = null;
    //protected GameObject _gimbalCameraGazeHitpointHalo = null;
    protected Camera _gimbalCamera = null;
    protected Interactable _interactableObject = null;
    protected ThemeDefinition _newThemeType;
    protected BoxCollider _boxCollider;

    protected float _lerpStartTime;
    protected bool _gotLerpStart = false;
    protected Vector3 _lerpStartPosition;
    protected Vector3 _lerpEndPosition;
    protected float _lerpTimeSpan;

    protected bool _keepAboveTerrain = (bool)AppSettings.App.GameObjectSettings.PlaceObjectsAboveTerrain.Value;
    protected bool _keepAboveTerrainAltitudeAdjusted = false;
    //protected float _keepAboveTerrainAltitudeAdjustement = 0;
    protected Vector3 _displayPositionOffset = new Vector3(0,0,0);

    protected HoloToolkit.Unity.DirectionIndicator _directionIndicator;

    //The position of the gimbal camera's gaze intersection with the terrain
    Vector3 _gimbalCameraGazeTerrainIntersctionPoint;
    bool _haveGimbalCameraGazeTerrainIntersctionPoint = false;

    public GameObject gameObject { get { return _gameObject; } }

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

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

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
            landingPad._gameObject.transform.position = padPosition + new Vector3(0,.3f,0);
        }

        return landingPad;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="objectPosition"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    //*************************************************************************

    public bool HeightAboveTerrain(Vector3 objectPosition, out float height)
    {
        Vector3 terrainHitPosition;
        height = 0;

        //Find the spot on the terrain below the object.
        //The arguments are the position of the UAV, and the down vector
        if (FindRayTerrainIntersection(
            objectPosition,
            Vector3.down,
            out terrainHitPosition))
        {
            height = objectPosition.y - terrainHitPosition.y;
            return true;
        }

        //Find the spot on the terrain above the object.
        //The arguments are the position of the UAV, and the up vector
        if (FindRayTerrainIntersection(
            objectPosition,
            Vector3.up,
            out terrainHitPosition))
        {
            height = objectPosition.y - terrainHitPosition.y;
            return true;
        }

        return false;
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

        _gimbalCameraGazeHitpoint = new ThingGazeHitpointObject();

        //AddGimbalCameraGazeHitpoint();
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
        _interactableObject.OnClick.AddListener(() => UnityEngine.Debug.Log("Interactable clicked"));
    }

    private void AddFocusEvents()
    {
        var onFocusReceiver = _interactableObject.AddReceiver<InteractableOnFocusReceiver>();

        onFocusReceiver.OnFocusOn.AddListener(
            () => UnityEngine.Debug.Log("Focus on"));
        onFocusReceiver.OnFocusOff.AddListener(
            () => UnityEngine.Debug.Log("Focus off"));
    }

    private void AddToggleEvents()
    {
        var toggleReceiver = _interactableObject.AddReceiver<InteractableOnToggleReceiver>();

        // Make the interactable have toggle capability, from code.
        // In the gui editor it's much easier
        _interactableObject.NumOfDimensions = 2;
        _interactableObject.CanSelect = true;
        _interactableObject.CanDeselect = true;

        toggleReceiver.OnSelect.AddListener(() => UnityEngine.Debug.Log("Toggle selected"));
        toggleReceiver.OnDeselect.AddListener(() => UnityEngine.Debug.Log("Toggle un-selected"));
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
    /// <param name="quatIn"></param>
    /// <returns></returns>
    //*************************************************************************

    private static UnityEngine.Quaternion ConvertQuat(System.Numerics.Quaternion quatIn)
    {
        return new UnityEngine.Quaternion(quatIn.X, quatIn.Y, quatIn.Z, quatIn.W);
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
        thing.DistanceToObserver = Thing.GetDistance(ThingsManager.Self, _thing);

        //x = E, y = U, z = N
        _gameObject.transform.position = Vector3.Lerp(
            _lerpStartPosition, _lerpEndPosition, lerpVal) + _displayPositionOffset;

        _gameObject.transform.localRotation = ConvertQuat(_thing.Pose.Orient.Quat);

        SetScale(this, 50);
        _haloObject?.Set();

        if (null != _gimbalCameraGazeHitpoint)
        {
            //var dist = GetDistance(ThingsManager.Self, _gimbalCameraGazeHitpoint);
            SetScale(_gimbalCameraGazeHitpoint, 50);
            _gimbalCameraGazeHitpoint._haloObject?.Set();
        }

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

        if (_useFlatTerrain)
        {
            //Use a terrain which is just a flat plane with a point at 0,0,0
            if (ThingsManager.HorizontalPlane.Raycast(ray, out var distance))
            {
                intersectionPoint = ray.GetPoint(distance);
                return true;
            }
        }
        else
        {
            //Use the terrain which was built from GEO data
            int layerMask = 1 << ThingsManager.MainTerrain.layer;
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                intersectionPoint = hit.point;
                return true;
            }
        }

        intersectionPoint = new Vector3(0,0,0);
        return false;
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

        _gimbalCameraGazeHitpoint.Set(
            _gimbalCameraGazeTerrainIntersctionPoint,
            _haveGimbalCameraGazeTerrainIntersctionPoint);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="orient"></param>
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
    /// <param name="thingGameObject"></param>
    /// <param name="maxApparentDistance"></param>
    //*************************************************************************
    private void SetScale(ThingGameObject thingGameObject, float maxApparentDistance)
    {
        float haloScale = 1;

        if (null != thingGameObject)
            if (null != thingGameObject._gameObject)
            {
                if (thingGameObject._thing.DistanceToObserver > maxApparentDistance)
                    haloScale = (float)_thing.DistanceToObserver / maxApparentDistance;

                thingGameObject._gameObject.transform.localScale = new Vector3(
                    haloScale, haloScale, haloScale);
            }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="distanceToObserver"></param>
    /// <param name="maxApparentDistance"></param>
    //*************************************************************************
    private void SetScale(GameObject gameObject, float distanceToObserver, float maxApparentDistance)
    {
        float scale = 1;

        if (null != gameObject)
            {
                if (distanceToObserver > maxApparentDistance)
                    scale = distanceToObserver / maxApparentDistance;

                gameObject.transform.localScale = new Vector3(
                    scale, scale, scale);
            }
    }

    //*************************************************************************
    /// <summary>
    ///find offset of reported altitude from telemetry to altitude of terrain
    ///in display. Corrects for mismatch of GPS to terrain elevations.
    /// </summary>
    /// <param name="thing"></param>
    //*************************************************************************
    public void SetAltitudeTerrainOffset(Thing thing)
    {
        //app config, are we even doing this?
        if (!_keepAboveTerrain)
            return;

        //have we already done this?
        if (_keepAboveTerrainAltitudeAdjusted)
            return;

        //do we have telemetry data yet?
        if (!thing.Pose.IsEnuValid)
            return;

        if (HeightAboveTerrain(PointENU.ToVector3(thing.Pose.PointEnu), out var height))
        {
            //subtract height above displayed terrain from display offset
            //_displayPositionOffset += new Vector3(0, -height, 0);
            _displayPositionOffset += new Vector3(0, -height, 0);
        }

        //indicate that height has been adjusted
        _keepAboveTerrainAltitudeAdjusted = true;
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
        _displayPositionOffset += new
            Vector3(0, (float)AppSettings.App.UavObjectSettings.AltitudeOffset.Value, 0);

        //GameObject resourceObject = Resources.Load("ThingDrone") as GameObject;
        GameObject resourceObject = Resources.Load("UavObject") as GameObject;
        base._gameObject = UnityEngine.Object.Instantiate(resourceObject) as GameObject;
        base._gameObject.name = "UAV" + _count++.ToString();
        base._gameObject.AddComponent<MeshRenderer>();
        base._gameObject.transform.localScale = new Vector3(1, 1, 1);
        base._gameObject.transform.position = new Vector3(0, 0, 0);
        base._gameObject.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        base._gameObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;

        _haloObject = new HaloObject(_gameObject, 8f);
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
        SetAltitudeTerrainOffset(thing);

        base.Set(thing);

        if (thing.Pose?.PointEnu != null)
        {
            if (_autoLandingPad) if (null == _landingPad) if(thing.Pose.IsEnuValid)
                _landingPad = AddLandingPad(PointENU.ToVector3(thing.Pose.PointEnu) + _displayPositionOffset);
            if (_leaveBreadcrumbs) if (thing.Pose.IsEnuValid)
                _breadcrumbs.Add(AddBreadcrumb(PointENU.ToVector3(thing.Pose.PointEnu) + _displayPositionOffset));
        }
    }
}

#endregion //ThingUavObject

#region HaloObject

//*************************************************************************
/// <summary>
/// 
/// </summary>
//*************************************************************************

public class HaloObject 
{
    GameObject _gameObject;
    public GameObject gameObject { get { return _gameObject; } }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public HaloObject(GameObject haloParent, float localScale)
    {
        GameObject objPrefab = Resources.Load("ThingHaloRing") as GameObject;
        _gameObject = UnityEngine.Object.Instantiate(objPrefab) as GameObject;
        _gameObject.name = haloParent.name + "Halo";
        _gameObject.AddComponent<MeshRenderer>();
        _gameObject.transform.SetParent(haloParent.transform, true);
        _gameObject.transform.localScale = new Vector3(localScale, localScale, localScale);
        _gameObject.transform.eulerAngles = new Vector3(90, 0, 0);
        _gameObject.transform.position = new Vector3(0, 0, 0);
        _gameObject.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        _gameObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(255, 0, 0));
        _gameObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public void Set()
    {
        if (null == _gameObject)
            return;

        // https://docs.unity3d.com/ScriptReference/Transform.LookAt.html
        if (!(ThingsManager.Self.GameObjectObject is ThingGameObject selfThing))
            return;

        if (null == selfThing.gameObject)
            return;

        gameObject.transform.LookAt(selfThing.gameObject.transform);
        //add another 90
        gameObject.transform.eulerAngles += new Vector3(90, 0, 0);
    }
}

#endregion //HaloObject

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

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="thing"></param>
    //*************************************************************************
    /*public override void Set(Thing thing)
    {
        SetAltitudeTerrainOffset(thing);

        base.Set(thing);
    }*/

}

#endregion //ThingPersonObject

#region ThingGazeHitpointObject

//*************************************************************************
/// <summary>
/// 
/// </summary>
//*************************************************************************

public class ThingGazeHitpointObject : ThingGameObject
{
    private static int _count = 0;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    public ThingGazeHitpointObject()
    {
        base._gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        base._gameObject.name = "GazeHitpoint" + _count++.ToString();
        base._gameObject.transform.localScale = new Vector3(1, 1, 1);
        base._gameObject.transform.position = new Vector3(0, 0, 0);
        base._gameObject.AddComponent<MeshRenderer>();
        base._gameObject.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        base._gameObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;
        base._gameObject.SetActive(false);

        base._thing = new Thing(base._gameObject.name, Thing.TypeEnum.GazeHipoint, 
            Thing.SelfEnum.Other, Thing.RoleEnum.Observed);

        //TODO * Very temporary
        base._thing.DistanceToObserver = 100;

        _haloObject = new HaloObject(_gameObject, 6f);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="hitpoint"></param>
    /// <param name="active"></param>
    //*************************************************************************
    public void Set(Vector3 hitpoint, bool active)
    {
        if (null == base._gameObject)
            return;

        base._gameObject.SetActive(active);

        if (active)
            base._gameObject.transform.position = hitpoint;
    }

}

#endregion //ThingGazeHitpointObject

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
