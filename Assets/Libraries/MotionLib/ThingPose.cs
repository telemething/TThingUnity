using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
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

    private bool _isNewData = false;
    private bool _isEnuValid = false;
    private bool calculateEnu = false;
    private PointENU _pointEnu = new PointENU();
    private PointLatLonAlt _pointGeo = new PointLatLonAlt();
    private Orientation _orient = new Orientation();
    private Orientation _gimbalOrient = new Orientation();
    private PointLatLonAlt _origin = new PointLatLonAlt();
    private PointGeoStatusEnum _pointGeoStatus = PointGeoStatusEnum.UnInit;
    private PointGeoUsableEnum _pointGeoUsable = PointGeoUsableEnum.UnInit;

    private double _declination = 15.5;
    bool _gotOrientTrue = false;
    bool _gotOrientMag = false;

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
    public Orientation Orient => _orient;
    public Orientation GimbalOrient => _gimbalOrient;

    public bool IsNewData
    {
        get { return _isNewData; }
    }

    public bool IsEnuValid
    {
        get { return _isEnuValid; }
    }

    //*************************************************************************
    /// <summary>
    /// Check if new data has arrived from some channel since we last checked
    /// it. Mark as not new data. (to indicate that we have read it).
    /// </summary>
    /// <returns></returns>
    //*************************************************************************

    public bool CheckForNewData()
    {
        if (_isNewData)
        {
            _isNewData = false;
            return true;
        }

        return false;
    }

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
    /// <param name="pointGeo"></param>
    /// <param name="pointOrigin"></param>
    ///
    //*************************************************************************

    private void CalculateEnu(PointLatLonAlt pointGeo, PointLatLonAlt pointOrigin)
    {
        if (calculateEnu)
        {
            _pointEnu = GeoLib.GpsUtils.GeodeticToEnu(_pointGeo, _origin);
            _isEnuValid = true;
        }
        else
        {
            _pointEnu.E = 0;
            _pointEnu.N = 0;
            _pointEnu.U = 0;
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    //*************************************************************************
    public void SetVals(TThingComLib.Messages.Message message)
    {
        type = message.Type.ToString(); //*** TODO * useless?
        id = message.From;
        tow = 2; //*** TODO * handle time string correctly

        if (null != message.Coord)
        {
            _pointGeo.Lat = message.Coord.Lat;
            _pointGeo.Lon = message.Coord.Lon;
            _pointGeo.Alt = message.Coord.Alt;

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

        if (null != message.Orient)
        {
            _orient.True = message.Orient.True;
            _orient.Magnetic = message.Orient.Mag;

            if (_orient.True != 0)
                _gotOrientTrue = true;

            if (_orient.Magnetic != 0)
                _gotOrientMag = true;

            if(_gotOrientTrue |  !_gotOrientMag)
            {
                _orient.Magnetic = (_orient.True + _declination) % 360;
            }
            else
            {
                if(_gotOrientMag)
                {
                    _orient.True = _orient.Magnetic - _declination;
                    if (_orient.True < 0)
                        _orient.True += 360;
                }
            }

            _orient.Quat = new System.Numerics.Quaternion(
                Convert.ToSingle(message.Orient.X),
                Convert.ToSingle(message.Orient.Y),
                Convert.ToSingle(message.Orient.Z),
                Convert.ToSingle(message.Orient.W));
        }

        if (null != message.Gimbal)
        {
            _gimbalOrient.Quat = new System.Numerics.Quaternion(
                Convert.ToSingle(message.Gimbal.X),
                Convert.ToSingle(message.Gimbal.Y),
                Convert.ToSingle(message.Gimbal.Z),
                Convert.ToSingle(message.Gimbal.W));
        }

        _isNewData = true;
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
        return string.Format($"Lat:{(_pointGeo.Lat):F6} Lon:{(_pointGeo.Lon):F6} Alt:{(_pointGeo.Alt):F2} N: {_pointEnu.N:F1} E: {_pointEnu.E:F1} U: {_pointEnu.U:F1} True:{_orient.True:F2} ");
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
