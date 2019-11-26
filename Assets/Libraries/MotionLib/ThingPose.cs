﻿using System;
using System.Collections;
using System.Collections.Generic;
using GeoLib;
using UnityEngine;

//*************************************************************************
///
/// <summary>
/// 
/// </summary>
/// <returns></returns>
/// 
//*************************************************************************
public class ThingPose 
{
    public enum PointGeoStatusEnum { UnInit, UnKnown, Waiting, Bad, Poor, Ok, Good }
    public enum PointGeoUsableEnum { UnInit, UnKnown, No, Yes }

    public string type;
    public string id;
    public uint tow;

    private bool calculateEnu = false;
    private PointENU _pointEnu = new PointENU();
    private PointLatLonAlt _pointGeo = new PointLatLonAlt();
    private PointLatLonAlt _origin = new PointLatLonAlt();
    private PointGeoStatusEnum _pointGeoStatus = PointGeoStatusEnum.UnInit;
    private PointGeoUsableEnum _pointGeoUsable = PointGeoUsableEnum.UnInit;

    public PointLatLonAlt Origin
    {
        set
        {
            _origin = value;
            calculateEnu = (null != _origin);
        }
    }

    public PointGeoUsableEnum PointGeoUsable => _pointGeoUsable;
    public PointGeoStatusEnum PointGeoStatus => _pointGeoStatus;
    public PointENU PointEnu => _pointEnu;
    public PointLatLonAlt PointGeo => _pointGeo;

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// 
    //*************************************************************************
    public ThingPose()
    {
        type = "";
        id = "";
        tow = 0;
        _pointGeo.Lat = 0;
        _pointGeo.Lon = 0;
        _pointGeo.Alt = 0;
        _pointEnu.E = 0;
        _pointEnu.N = 0;
        _pointEnu.U = 0;
    }

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// 
    //*************************************************************************
    public ThingPose(string jsonString)
    {
        SetVals(jsonString);
    }

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pointGeo"></param>
    /// <param name="pointOrigin"></param>
    ///
    //*************************************************************************

    private void CalculateEnu(PointLatLonAlt pointGeo, PointLatLonAlt pointOrigin)
    {
        if (calculateEnu)
            _pointEnu = GeoLib.GpsUtils.GeodeticToEnu(_pointGeo, _origin);
        else
        {
            _pointEnu.E = 0;
            _pointEnu.N = 0;
            _pointEnu.U = 0;
        }
    }

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// 
    //*************************************************************************
    public void SetVals(Newtonsoft.Json.Linq.JObject jsonObj)
    {
        try
        {
            type = jsonObj["type"].ToString();
            id = jsonObj["id"].ToString();
            tow = Convert.ToUInt32(jsonObj["tow"]);
            var coord = jsonObj["coord"];
            _pointGeo.Lat = Convert.ToDouble(coord["lat"]);
            _pointGeo.Lon = Convert.ToDouble(coord["lon"]);
            _pointGeo.Alt = Convert.ToDouble(coord["alt"]);

            // If coords are > 1000 then they are ints, and need to be scaled down
            if (_pointGeo.Lat > 1000)
            {
                _pointGeo.Lat /= 10e6;
                _pointGeo.Lon /= 10e6;
                _pointGeo.Alt /= 10e2;
            }

            //TODO : just say ok for now
            _pointGeoStatus = PointGeoStatusEnum.Ok;
            _pointGeoUsable = PointGeoUsableEnum.Yes;

            CalculateEnu(_pointGeo, _origin);
        }
        catch (Exception e)
        {
            throw new Exception(
                $"ThingPose::SetVals() : unable to parse json obj : '{e.Message}'",
                e.InnerException);
        }
    }

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// 
    //*************************************************************************
    public void SetVals(string jsonString)
    {
        try
        {
            SetVals(Newtonsoft.Json.Linq.JObject.Parse(jsonString));
        }
        catch (Exception e)
        {
            throw new Exception(
                $"ThingPose::SetVals() : unable to parse json string : '{e.Message}'",
                e.InnerException);
        }
    }

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// 
    //*************************************************************************
    public void SetVals(ThingPose thing)
    { 
        type = thing.type;
        id = thing.id;
        tow = thing.tow;
        _pointGeo.Lat = thing._pointGeo.Lat;
        _pointGeo.Lon = thing._pointGeo.Lon;
        _pointGeo.Alt = thing._pointGeo.Alt;

        //TODO : just say ok for now
        _pointGeoStatus = PointGeoStatusEnum.Ok;
        _pointGeoUsable = PointGeoUsableEnum.Yes;

        CalculateEnu(_pointGeo, _origin);
    }

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// 
    //*************************************************************************
    public override string ToString()
    {
        //return string.Format($"Lat:{_pointGeo.Lat / 10e6} Lon:{_pointGeo.Lon / 10e6} Alt:{_pointGeo.Alt / 10e2}");
        return string.Format($"Lat:{(_pointGeo.Lat):F6} Lon:{(_pointGeo.Lon):F6} Alt:{(_pointGeo.Alt):F2} N: {_pointEnu.N:F1}  E: {_pointEnu.E:F1}  U: {_pointEnu.U:F1} ");
    }

    //*************************************************************************
    ///
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// 
    //*************************************************************************
    public string ToStringExt()
    {
        return string.Format($"Lat:{_pointGeo.Lat / 10e6} Lon:{_pointGeo.Lon / 10e6} Alt:{_pointGeo.Alt / 10e2} N: {_pointEnu.N:F1}  E: {_pointEnu.E:F1}  U: {_pointEnu.U:F1} ");
    }
}