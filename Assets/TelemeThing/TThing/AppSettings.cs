using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#region AppSettings

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
    private bool _haveConfigServer = false;
    private bool _haveGeoTileServer = false;
    WebApiLib.WebApiClient _webApiClient = null;

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
            TerrainTilesPerSide = new AppSetting("TerrainTilesPerSide", 7, "The number of tiles per edge (-1 because center tile)"),
        };

    List<AppSettingsBase> _settingCollections = new List<AppSettingsBase>();

    //*************************************************************************
    /// <summary>
    /// Fetch the singleton
    /// </summary>
    //*************************************************************************
    public static AppSettings App
    {
        get
        {
            if (null == _appSettings)
                _appSettings = new AppSettings("TThingUnity");
            return _appSettings;
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="appName"></param>
    //*************************************************************************
    public AppSettings(string appName)
    {
        _appName = appName;
        _settingCollections = new List<AppSettingsBase>
        { 
            ThingsManagerSettings, 
            GameObjectSettings, 
            UavObjectSettings, 
            SelfSettings, 
            TerrainSettings
        };
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
        /*var pas = new PortableAppSettings(_appName,
            _appDescription, new List<AppSettingCollection>
        {
            ThingsManagerSettings.AppSettings,
            GameObjectSettings.AppSettings,
            UavObjectSettings.AppSettings,
            SelfSettings.AppSettings,
            TerrainSettings.AppSettings
        }
        );*/

        var pas = new PortableAppSettings(_appName,
            _appDescription, new List<AppSettingCollection>());

        foreach (var settingCollection in _settingCollections)
            pas.AppSettingCollections.Add(settingCollection.AppSettings);

       return Newtonsoft.Json.JsonConvert.SerializeObject(pas);
    }

    //*************************************************************************
    /// <summary>
    /// Called when we receive an advertisement for an available config server,
    /// we set an event callback and start up WebApi connection to that server.
    /// </summary>
    /// <param name="configService"></param>
    //*************************************************************************
    private void ProcessConfigServerAdvertisement(
        TThingComLib.Messages.NetworkService configService)
    {
        //leave if we are already connected
        if (_haveConfigServer)
            return;

        _haveConfigServer = true;

        _webApiClient = WebApiLib.WebApiClient.Singleton;

        try
        {
            //set up a method to be called by the WebApi on any state change
            _webApiClient.AddEventCallback(WebApiEventCallback);

            //set up a method to be called when the WebApi server wants to
            //change an app setting
            _webApiClient.AddApiMethod(
                WebApiLib.WebApiMethodNames.Settings_ChangeSettings,
                ChangeSettingsApiMethod);

            //connect to the remote WebApi server
            var connected = _webApiClient.Connect(configService.URL);
        }
        catch (Exception ex)
        {
            LogThis(ex);
            throw ex;
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="geoTileService"></param>
    //*************************************************************************
    private void ProcessGeoTileServerAdvertisement(
        TThingComLib.Messages.NetworkService geoTileService)
    {
        //leave if we are already connected
        if (_haveGeoTileServer)
            return;

        _haveGeoTileServer = true;

        //indicate that we have a GeoTileServer available
        WebApiLib.WebApiClient.Singleton.IsGeoTileServer = true;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    //*************************************************************************
    public void ProcessMessage(TThingComLib.Messages.Message message)
    {
        foreach (var networkService in message.NetworkServices)
        {
            switch (networkService.ServiceType)
            {
                case TThingComLib.Messages.ServiceTypeEnum.Config:
                    ProcessConfigServerAdvertisement(networkService);
                    break;
                case TThingComLib.Messages.ServiceTypeEnum.GeoTile:
                    ProcessGeoTileServerAdvertisement(networkService);
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

    //*************************************************************************
    /// <summary>
    /// Invoked by _webApiClient whenever an event (i.e. connect) occurs
    /// </summary>
    /// <param name="apiEvent"></param>
    //*************************************************************************
    private async void WebApiEventCallback(WebApiLib.ApiEvent apiEvent)
    {
        switch(apiEvent.EventType)
        {
            case WebApiLib.ApiEvent.EventTypeEnum.connect:
                //send a copy of our settings to the server
                await SendSettingsToWebApi();
                break;
            case WebApiLib.ApiEvent.EventTypeEnum.disconnect:
                break;
            case WebApiLib.ApiEvent.EventTypeEnum.failure:
                break;
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    //*************************************************************************
    private async Task SendSettingsToWebApi()
    {
        var resp = await _webApiClient.Invoke(
            new WebApiLib.Request(
                WebApiLib.WebApiMethodNames.Settings_RegisterRemoteSettings,
                new List<WebApiLib.Argument> 
                { new WebApiLib.Argument("Settings", AppSettings.App.Serialize()) }));
    }

    //*************************************************************************
    /// <summary>
    /// Called when the WebApi server wants to change an app setting
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    //*************************************************************************
    private List<WebApiLib.Argument> ChangeSettingsApiMethod(
        List<WebApiLib.Argument> args)
    {
        if (!(args[0].Value is String appSettingsString))
            throw new ArgumentException("args[0].Value not encoded as string");

        var changedSettings = PortableAppSettings.Deserialize(appSettingsString);

        foreach(var appSettingCollection in changedSettings.AppSettingCollections)
            foreach(var appSetting in appSettingCollection.AppSettings)
                Update(appSettingCollection, appSetting);

        return new List<WebApiLib.Argument>();
    }

    //*************************************************************************
    /// <summary>
    /// Update an appSetting value
    /// </summary>
    /// <param name="newApSettingCollection"></param>
    /// <param name="newAppSetting"></param>
    //*************************************************************************
    private void Update(
        AppSettingCollection newApSettingCollection, AppSetting newAppSetting)
    {
        var currentAppSetting = FindAppSetting(newApSettingCollection, newAppSetting);

        if(null == currentAppSetting)
            throw new ArgumentException(
                "Setting '" + newAppSetting.name + "' not found");

        currentAppSetting.Value = newAppSetting.Value;
    }

    //*************************************************************************
    /// <summary>
    /// Find an appSetting entry
    /// </summary>
    /// <param name="searchApSettingCollection"></param>
    /// <param name="searchAppSetting"></param>
    /// <returns></returns>
    //*************************************************************************
    private AppSetting FindAppSetting(
        AppSettingCollection searchApSettingCollection, AppSetting searchAppSetting)
    {
        foreach( var appSettingCollection in _settingCollections)
        {
            if(searchApSettingCollection.name.Equals(appSettingCollection.AppSettings.name))
                foreach ( var appSetting in appSettingCollection.AppSettings.AppSettings )
                {
                    if (searchAppSetting.name.Equals(appSetting.name))
                        return appSetting;
                }
        }

        return null;
    }

    private void LogThis(Exception ex)
    {

    }
}

#endregion

#region Messages

//*****************************************************************************
/// <summary>
/// App setting
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
/// Container of app settings
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
/// Container of portable app settings
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
    public List<AppSettingCollection> 
        AppSettingCollections => _appSettingCollections;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="appSettingCollections"></param>
    //*************************************************************************
    public PortableAppSettings(string name, string description,
        List<AppSettingCollection> appSettingCollections)
    {
        _name = name;
        _description = description;
        _appSettingCollections = appSettingCollections;
    }

    //*********************************************************************
    /// <summary>
    /// Hydrate an instance of this class from serialized data
    /// </summary>
    /// <param name="serializedData"></param>
    /// <returns></returns>
    //*********************************************************************
    public static PortableAppSettings Deserialize(string serializedData)
    {
        return Newtonsoft.Json.JsonConvert.
            DeserializeObject<PortableAppSettings>(serializedData);
    }

    //*********************************************************************
    /// <summary>
    /// Create serialized data from this instance
    /// </summary>
    /// <param name="serializedData">Include only changed settings if true
    /// </param>
    /// <returns></returns>
    //*********************************************************************
    public string Serialize(bool onlyChanged)
    {
        if (!onlyChanged)
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);

        bool collectionAdded;
        var settingCollectionsOut = new List<AppSettingCollection>();
        var sc = new AppSettingCollection("", "", new List<AppSetting>());

        foreach (var settingCollection in AppSettingCollections)
        {
            collectionAdded = false;

            foreach (var appSetting in settingCollection.AppSettings)
            {
                if (appSetting.Changed)
                {
                    if (!collectionAdded)
                    {
                        sc = new AppSettingCollection(settingCollection.name,
                            settingCollection.Description as string,
                            new List<AppSetting>());
                        settingCollectionsOut.Add(sc);
                        collectionAdded = true;
                    }

                    sc.AppSettings.Add(appSetting);
                }
            }
        }

        PortableAppSettings returnVal = new PortableAppSettings(
            this.name, this._description, settingCollectionsOut);

        return Newtonsoft.Json.JsonConvert.SerializeObject(returnVal);
    }

    //*********************************************************************
    /// <summary>
    /// Find the appsetting of the given longname
    /// </summary>
    /// <param name="automationId"></param>
    /// <returns></returns>
    //*********************************************************************
    public AppSetting FindAppSetting(string longname)
    {
        if (null == longname)
            return null;

        foreach (var settingsCollection in _appSettingCollections)
        {
            if (longname.Contains(settingsCollection.name))
                foreach (var setting in settingsCollection.AppSettings)
                {
                    if ((settingsCollection.name + setting.name).Equals(longname))
                        return setting;
                }
        }

        return null;
    }

    //*********************************************************************
    /// <summary>
    /// Update the value of the app setting of the given longname
    /// </summary>
    /// <param name="longName"></param>
    /// <param name="settingValue"></param>
    //*********************************************************************

    public void UpdateValue(string longName, object settingValue)
    {
        var appSetting = FindAppSetting(longName);

        if (null == appSetting)
            return;

        switch (appSetting.Type)
        {
            case Type tipe when tipe == typeof(int):
                appSetting.Value = Convert.ToInt32(settingValue);
                break;
            case Type tipe when tipe == typeof(Int64):
                appSetting.Value = Convert.ToInt64(settingValue);
                break;
            case Type tipe when tipe == typeof(bool):
                appSetting.Value = Convert.ToBoolean(settingValue);
                break;
            case Type tipe when tipe == typeof(float):
                appSetting.Value = Convert.ToDouble(settingValue);
                break;
            case Type tipe when tipe == typeof(double):
                appSetting.Value = Convert.ToDouble(settingValue);
                break;
            case Type tipe when tipe == typeof(string):
                appSetting.Value = Convert.ToString(settingValue);
                break;
            default:
                break;
        }
    }
}

#endregion

#region Settings

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class AppSettingsBase
{
    protected AppSettingCollection _appSettings { set;  get; }
    public AppSettingCollection AppSettings => _appSettings;
}

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class ThingsManagerSettings : AppSettingsBase
{
    public new AppSettingCollection AppSettings
    {
        get
        {
            base._appSettings = new AppSettingCollection("Things Manager", "Things Manager Settings",
            new List<AppSetting>() { TelemetryPort });
            return base._appSettings;
        }
    }

    public AppSetting TelemetryPort { get; set; }
}

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class GameObjectSettings : AppSettingsBase
{
    public new AppSettingCollection AppSettings 
    {
        get
        {
            base._appSettings = new AppSettingCollection("Game", "Game Settings",
            new List<AppSetting>() { PlaceObjectsAboveTerrain, DefaultLerpTimeSpan, UseFlatTerrain, HaloScaleFactor });
            return base._appSettings;
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
public class UavObjectSettings : AppSettingsBase
{
    public new AppSettingCollection AppSettings
    {
        get
        {
            base._appSettings = new AppSettingCollection("UAV", "UAV Settings",
            new List<AppSetting>() { AltitudeOffset });
            return base._appSettings;
        }
    }

    public AppSetting AltitudeOffset { get; set; }
}

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class SelfSettings : AppSettingsBase
{
    public new AppSettingCollection AppSettings
    {
        get
        {
            base._appSettings = new AppSettingCollection("Self", "Self Settings",
            new List<AppSetting>() { MyThingId, MainCameraAltitudeOverTerrainOffset });
            return base._appSettings;
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
public class TerrainSettings : AppSettingsBase
{
    public new AppSettingCollection AppSettings
    {
        get
        {
            base._appSettings = new AppSettingCollection("Terrain", "Terrain Settings",
            new List<AppSetting>() { TerrainZoomLevel, TerrainTilesPerSide });
            return base._appSettings;
        }
    }

    public AppSetting TerrainZoomLevel { get; set; }

    public AppSetting TerrainTilesPerSide { get; set; }
}

#endregion






