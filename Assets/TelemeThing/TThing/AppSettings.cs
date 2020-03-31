using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppSettings 
{
    private string _appName;
    private static AppSettings _appSettings = new AppSettings("TThingUnity");

    public ThingsManagerSettings ThingsManagerSettings =
        new ThingsManagerSettings()
        {
            TelemetryPort = new AppSetting("TelemetryPort", 45679, "The UDP port on which to monitor telemetry messages"),
            MyThingId = new AppSetting("MyThingId", "self", "The ID of the self object in telemetry messages"),
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
            AltitudeOffset = new AppSetting("AltitudeOffset", 1.0f, "AltitudeOffset"),
        };

    /// <summary>
    /// Fetch the singleton
    /// </summary>
    public static AppSettings App => _appSettings;

    public AppSettings(string appName)
    {
        _appName = appName;
    }

    void Update()
    {
        
    }
}

public class AppSetting
{
    private string _name;
    private string _description;
    private object _value;
    private System.Type _type;

    public string name => _name;
    //public object Value => _value;
    public object Description => _description;
    public System.Type Type => _type;
    public object Value { set => _value = value; get => _value; }

    public AppSetting(string name, object value, string description)
    {
        _name = name;
        _value = value;
        _description = description;
        _type = value.GetType();
    }
}

public class ThingsManagerSettings
{
    public AppSetting TelemetryPort { get; set; }

    public AppSetting MyThingId { get; set; }
}

public class GameObjectSettings
{
    public AppSetting PlaceObjectsAboveTerrain { get; set; }

    public AppSetting DefaultLerpTimeSpan { get; set; }

    // If true, will use a flat plane, otherwise will use a GEO plane
    public AppSetting UseFlatTerrain { get; set; }

    public AppSetting HaloScaleFactor { get; set; }
}

public class UavObjectSettings
{
    public AppSetting AltitudeOffset { get; set; }
}


