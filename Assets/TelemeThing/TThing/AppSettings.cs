using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class AppSettings 
{
    private string _appName = "TThingUnity";
    private string _appDescription = "TThing Unity";
    private static AppSettings _appSettings = new AppSettings("TThingUnity");

    public ThingsManagerSettings ThingsManagerSettings =
        new ThingsManagerSettings()
        {
            TelemetryPort = new AppSetting("TelemetryPort", 45679, "The UDP port on which to monitor telemetry messages")
        };

    public GameObjectSettings GameObjectSettings =
        new GameObjectSettings()
        {
            PlaceObjectsAboveTerrain = new AppSetting("PlaceObjectsAboveTerrain", true, "PlaceObjectsAboveTerrain"),
            DefaultLerpTimeSpan = new AppSetting("DefaultLerpTimeSpan", 1.0f, "DefaultLerpTimeSpan"),
            UseFlatTerrain = new AppSetting("UseFlatTerrain", false, "If true, will use a flat plane, otherwise will use a GEO plane"),
            HaloScaleFactor = new AppSetting("HaloScaleFactor", 16f, "HaloScaleFactor")
        };

    public UavObjectSettings UavObjectSettings =
        new UavObjectSettings()
        {
            AltitudeOffset = new AppSetting("AltitudeOffset", 2.0f, "AltitudeOffset"),
        };

    public SelfSettings SelfSettings =
        new SelfSettings()
        {
            MyThingId = new AppSetting("MyThingId", "self", "The ID of the self object in telemetry messages"),
            MainCameraAltitudeOverTerrainOffset = new AppSetting("MainCameraAltitudeOverTerrainOffset", 2.0f, "MainCameraAltitudeOverTerrainOffset"),
        };

    public TerrainSettings TerrainSettings =
        new TerrainSettings()
        {
            TerrainZoomLevel = new AppSetting("TerrainZoomLevel", 18, "The zoom level fetched from the tile server"),
            TerrainTilesPerSide = new AppSetting("TerrainTilesPerSide", 9, "The number of tiles per edge (-1 because center tile)"),
        };

    //*************************************************************************
    /// <summary>
    /// Fetch the singleton
    /// </summary>
    //*************************************************************************
    public static AppSettings App => _appSettings;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="appName"></param>
    //*************************************************************************
    public AppSettings(string appName)
    {
        _appName = appName;
    }

    void Update()
    {       
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    //*************************************************************************
    public string Serialize()
    {
        var pas = new PortableAppSettings(_appName, 
            _appDescription , new List<AppSettingCollection>
        {
            ThingsManagerSettings.AppSettings,
            GameObjectSettings.AppSettings,
            UavObjectSettings.AppSettings,
            SelfSettings.AppSettings,
            TerrainSettings.AppSettings
        }
        );

        string output = Newtonsoft.Json.JsonConvert.SerializeObject(pas);

        return output;
    }

    private bool _haveConfigServer = false;
    WebApiLib.WebApiClient _webApiClient = null;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configService"></param>
    //*************************************************************************
    private void ProcessConfigServerMessage(
        TThingComLib.Messages.NetworkService configService)
    {
        if (_haveConfigServer)
            return;

        _haveConfigServer = true;

        _webApiClient = new WebApiLib.WebApiClient();

        try
        {
            var connected = _webApiClient.Connect(configService.URL);
        }
        catch(Exception ex)
        {
            throw;
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    //*************************************************************************
    public void ProcessMessage(TThingComLib.Messages.Message message)
    {
        foreach( var networkService in message.NetworkServices )
        {
            switch(networkService.ServiceType)
            {
                case TThingComLib.Messages.ServiceTypeEnum.Config:
                    ProcessConfigServerMessage(networkService);
                    break;
                case TThingComLib.Messages.ServiceTypeEnum.GeoTile:
                    break;
                case TThingComLib.Messages.ServiceTypeEnum.GroundStation:
                    break;
                case TThingComLib.Messages.ServiceTypeEnum.SelfTelem:
                    break;
                case TThingComLib.Messages.ServiceTypeEnum.Unknown:
                    break;
            }
        }
    }
}

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class AppSetting
{
    private string _name;
    private string _description;
    private object _value;
    private System.Type _type;
    private bool _changed = false;

    public string name => _name;
    //public object Value => _value;
    public object Description => _description;
    public System.Type Type => _type;
    public object Value { set => _value = value; get => _value; }
    public bool Changed => _changed;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <param name="description"></param>
    //*************************************************************************
    public AppSetting(string name, object value, string description)
    {
        _name = name;
        _value = value;
        _description = description;
        _type = value.GetType();
    }
}

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class AppSettingCollection
{
    private string _name;
    private string _description;
    private List<AppSetting> _appSettings;

    public string name => _name;
    //public object Value => _value;
    public object Description => _description;
    public List<AppSetting> AppSettings => _appSettings;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="appSettings"></param>
    //*************************************************************************
    public AppSettingCollection(string name, string description, List<AppSetting> appSettings)
    {
        _name = name;
        _description = description;
        _appSettings = appSettings;
    }
}

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class PortableAppSettings
{
    private string _name;
    private string _description;
    private List<AppSettingCollection> _appSettingCollections;

    public string name => _name;
    //public object Value => _value;
    public object Description => _description;
    public List<AppSettingCollection> AppSettingCollections => _appSettingCollections;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="appSettingCollections"></param>
    //*************************************************************************
    public PortableAppSettings(string name, string description, List<AppSettingCollection> appSettingCollections)
    {
        _name = name;
        _description = description;
        _appSettingCollections = appSettingCollections;
    }
}

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class ThingsManagerSettings
{
    public AppSettingCollection AppSettings
    {
        get
        {
            return new AppSettingCollection("Things Manager", "Things Manager Settings",
            new List<AppSetting>() { TelemetryPort });
        }
    }

    public AppSetting TelemetryPort { get; set; }
}

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class GameObjectSettings
{
    public AppSettingCollection AppSettings 
    {
        get 
        { 
            return new AppSettingCollection("Game", "Game Settings", 
            new List<AppSetting>() { PlaceObjectsAboveTerrain, DefaultLerpTimeSpan, UseFlatTerrain, HaloScaleFactor }); 
        } 
    }

    public AppSetting PlaceObjectsAboveTerrain { get; set; }

    public AppSetting DefaultLerpTimeSpan { get; set; }

    // If true, will use a flat plane, otherwise will use a GEO plane
    public AppSetting UseFlatTerrain { get; set; }

    public AppSetting HaloScaleFactor { get; set; }
}

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class UavObjectSettings
{
    public AppSettingCollection AppSettings
    {
        get
        {
            return new AppSettingCollection("UAV", "UAV Settings",
            new List<AppSetting>() { AltitudeOffset });
        }
    }

    public AppSetting AltitudeOffset { get; set; }
}

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class SelfSettings
{
    public AppSettingCollection AppSettings
    {
        get
        {
            return new AppSettingCollection("Self", "Self Settings",
            new List<AppSetting>() { MyThingId, MainCameraAltitudeOverTerrainOffset });
        }
    }

    public AppSetting MyThingId { get; set; }

    public AppSetting MainCameraAltitudeOverTerrainOffset { get; set; }
}

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class TerrainSettings
{
    public AppSettingCollection AppSettings
    {
        get
        {
            return new AppSettingCollection("Terrain", "Terrain Settings",
            new List<AppSetting>() { TerrainZoomLevel, TerrainTilesPerSide });
        }
    }

    public AppSetting TerrainZoomLevel { get; set; }

    public AppSetting TerrainTilesPerSide { get; set; }
}







