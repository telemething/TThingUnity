using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileCache : MonoBehaviour
{
    static string DataPath 
    { 
        get 
        { 
            // https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html
            return Application.persistentDataPath + "/TileCache"; 
        } 
    }

    public enum DataTypeEnum { None, StreetMap, Topo, Elevation }
    public enum DataProviderEnum { None, OSM, VirtualEarth }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*********************************************************************
    void Start()
    {
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*********************************************************************
    void Update()
    {
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="zoomLevel"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="dataType"></param>
    /// <param name="dataProvider"></param>
    /// <param name="fileExt"></param>
    /// <param name="createDir"></param>
    /// <returns></returns>
    //*********************************************************************
    public static string GetFileName(int zoomLevel, int x, int y, 
        DataTypeEnum dataType, DataProviderEnum dataProvider, string fileExt, bool createDir)
    {
        var dir = string.Format("{0}/z{1}/x{2}/y{3}",
            DataPath, zoomLevel, x, y);

        if (createDir)
            System.IO.Directory.CreateDirectory(dir);

        return string.Format("{0}/{1}_{2}.{3}",
            dir, dataType.ToString(), dataProvider.ToString(), fileExt);
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="zoomLevel"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="dataType"></param>
    /// <param name="dataProvider"></param>
    /// <param name="fileExt"></param>
    //*********************************************************************
    public static void Store(byte[] data, int zoomLevel, int x, int y,
        DataTypeEnum dataType, DataProviderEnum dataProvider, string fileExt)
    {
        WriteFileData(GetFileName(zoomLevel, x, y, dataType, 
            dataProvider, fileExt, true), data);
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="zoomLevel"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="dataType"></param>
    /// <param name="dataProvider"></param>
    /// <param name="fileExt"></param>
    //*********************************************************************
    public static void Store(string data, int zoomLevel, int x, int y,
        DataTypeEnum dataType, DataProviderEnum dataProvider, string fileExt)
    {
        WriteFileData(GetFileName(zoomLevel, x, y, dataType, 
            dataProvider, fileExt, true), data);
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="zoomLevel"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="dataType"></param>
    /// <param name="dataProvider"></param>
    /// <param name="fileExt"></param>
    /// <returns></returns>
    //*********************************************************************
    public static byte[] FetchBytes(int zoomLevel, int x, int y,
        DataTypeEnum dataType, DataProviderEnum dataProvider, string fileExt)
    {
        return GetFileData(GetFileName(zoomLevel, x, y, dataType,
            dataProvider, fileExt, false));
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="zoomLevel"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="dataType"></param>
    /// <param name="dataProvider"></param>
    /// <param name="fileExt"></param>
    /// <returns></returns>
    //*********************************************************************
    public static string FetchText(int zoomLevel, int x, int y,
        DataTypeEnum dataType, DataProviderEnum dataProvider, string fileExt)
    {
        return GetFileText(GetFileName(zoomLevel, x, y, dataType,
            dataProvider, fileExt, false));
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    //*********************************************************************
    public static byte[] GetFileData(string fileName)
    {
        try
        {
            System.IO.FileStream fs = System.IO.File.Open(
                fileName, System.IO.FileMode.Open);

            long fileSize = fs.Length;
            byte[] fileData = new byte[fileSize];
            fs.Read(fileData, 0, (int)fileSize);
            fs.Close();

            return fileData;
        }
        catch (Exception ex)
        {
            throw new Exception("GetFileData() : " + ex.Message);
        }
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    //*********************************************************************
    public static string GetFileText(string fileName)
    {
        try
        {
            using (var sr = new System.IO.StreamReader(fileName))
            {
                return sr.ReadToEnd();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("GetFileText() : " + ex.Message);
        }
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="data"></param>
    //*********************************************************************
    public static void WriteFileData(string fileName, byte[] data)
    {
        try
        {
            System.IO.FileStream fs = System.IO.File.Open(
                fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            fs.Write(data, 0, data.Length);
            fs.Flush();
            fs.Close();
        }
        catch (Exception ex)
        {
            throw new Exception("WriteFileData() : " + ex.Message);
        }
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="data"></param>
    //*********************************************************************
    public static void WriteFileData(string fileName, string data)
    {
        try
        {
            using (var sw = new System.IO.StreamWriter(fileName))
            {
                sw.WriteLine(data);
                sw.Flush();
                sw.Close();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("WriteFileData() : " + ex.Message);
        }
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    //*********************************************************************
    static bool DoesExist(string fileName)
    {
        var fi = new System.IO.FileInfo(fileName);
        return fi.Exists;
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="zoomLevel"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="dataType"></param>
    /// <param name="dataProvider"></param>
    /// <param name="fileExt"></param>
    /// <returns></returns>
    //*********************************************************************
    public static bool DoesExist(int zoomLevel, int x, int y, 
        DataTypeEnum dataType, DataProviderEnum dataProvider, string fileExt)
    {
        return DoesExist(GetFileName(zoomLevel, x, y, dataType, dataProvider, fileExt, false));
    }
}
