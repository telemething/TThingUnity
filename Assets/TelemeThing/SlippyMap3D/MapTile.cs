
//using HoloToolkitExtensions.RemoteAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// https://github.com/LocalJoost/UnityTextureDownload/blob/master/Assets/App/DynamicTextureDownloader.cs
// https://github.com/LocalJoost/TextureLoaderError/blob/master/Assets/Assets/HoloToolkitExtensions/Scripts/RemoteAssets/DynamicTextureDownloader.cs
// http://dotnetbyexample.blogspot.com/2017/02/a-behaviour-for-dynamically-loading-and.html
// http://dotnetbyexample.blogspot.com/2018/10/workaround-remote-texture-loading-does.html

public class DynamicTextureDownloader : MonoBehaviour
{
    public string ImageUrl;
    public bool ResizePlane;
    public bool UseCache = true;
    protected TileInfo _tileData;
    private string _pngExtenstion = "png";

    bool _useWebApiClient = false;

    private UnityEngine.Networking.UnityWebRequest _imageLoader = null;
    protected WebApiLib.WebApiClient _webApiClient = null;

    private string _previousImageUrl = null;
    private bool _appliedToTexture = false;

    private Vector3 _originalScale;
    bool IsDownloading = false;
    byte[] _downloadedData = null;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Start()
    {
        _originalScale = transform.localScale;
        _webApiClient = WebApiLib.WebApiClient.Singleton;
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Update()
    {
        CheckLoadImage();
        OnUpdate();
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************

    private void CheckLoadImage()
    {
        // No image requested
        if (string.IsNullOrEmpty(ImageUrl))
            return;

        // New image set - reset status vars and start loading new image
        if (_previousImageUrl != ImageUrl)
        {
            _previousImageUrl = ImageUrl;

            //Look in cache, if there fetch from cache and load into texture
            if(UseCache & TileCache.DoesExist(_tileData.ZoomLevel, _tileData.X, _tileData.Y, 
                TileCache.DataTypeEnum.StreetMap, TileCache.DataProviderEnum.OSM, _pngExtenstion) )
            {
                var rawData = TileCache.FetchBytes(_tileData.ZoomLevel, _tileData.X, _tileData.Y,
                TileCache.DataTypeEnum.StreetMap, TileCache.DataProviderEnum.OSM, _pngExtenstion);

                //get texture from image
                var tex = new Texture2D(_tileData.MapPixelSize, _tileData.MapPixelSize);
                tex.LoadImage(rawData);
                GetComponent<Renderer>().material.mainTexture = tex;

                _appliedToTexture = true;

                if (ResizePlane)
                    DoResizePlane(tex);
            }
            else
                //not found in cache, fetch from external service
                OnStartLoad();
        }

        //if fetching from WebApi service
        if (IsDownloading)
        //check if done downloading from WebApi service
        if (null != _downloadedData)
        {
            IsDownloading = false;
            //ProcessElevationData(_downloadedData);

            //store to cache
            if (UseCache)
                TileCache.Store(_downloadedData, _tileData.ZoomLevel,
                    _tileData.X, _tileData.Y, TileCache.DataTypeEnum.StreetMap,
                    TileCache.DataProviderEnum.OSM, _pngExtenstion);

            //get texture from image
            var tex = new Texture2D(_tileData.MapPixelSize, _tileData.MapPixelSize);
            tex.LoadImage(_downloadedData);
            GetComponent<Renderer>().material.mainTexture = tex;

            _appliedToTexture = true;

            if (ResizePlane)
                DoResizePlane(tex);
            
            return;
        }

        //if fetching from a third party tile server
        if (_imageLoader != null && _imageLoader.isDone && !_appliedToTexture)
        {
            _appliedToTexture = true;

            //get texture from image
            var tex = ((UnityEngine.Networking.DownloadHandlerTexture)_imageLoader.downloadHandler).texture;  
            GetComponent<Renderer>().material.mainTexture = tex;

            //store to cache
            if (UseCache)
                TileCache.Store(ImageConversion.EncodeToPNG(tex), _tileData.ZoomLevel,
                    _tileData.X, _tileData.Y, TileCache.DataTypeEnum.StreetMap,
                    TileCache.DataProviderEnum.OSM, _pngExtenstion);

            if (ResizePlane)
                DoResizePlane(tex);

            //Destroy(tex);
            //_imageLoader = null;

            OnEndLoad();
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="tex"></param>
    //*************************************************************************
    private void DoResizePlane(Texture tex)
    {
        // Keep the longest edge at the same length
        if (tex.width < tex.height)
            transform.localScale = new Vector3(
                _originalScale.z * tex.width / tex.height,
                _originalScale.y, _originalScale.z);
        else
            transform.localScale = new Vector3(
                _originalScale.x, _originalScale.y,
                _originalScale.x * tex.height / tex.width);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    protected virtual void OnStartLoad()
    {
        _appliedToTexture = false;

        if (null == _webApiClient)
            _webApiClient = WebApiLib.WebApiClient.Singleton;

        //can we fetch from paired ApiService?
        if(_useWebApiClient)
        if (_webApiClient.IsGeoTileServer)
        {
            //Call async, don't await.
            FetchImageTileFromWebApiAsync();
            return;
        }

        _imageLoader = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(ImageUrl); 
        _imageLoader.SendWebRequest();//T3
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    //*************************************************************************
    private async Task FetchImageTileFromWebApiAsync()
    {
        IsDownloading = true;

        var resp = await _webApiClient.Invoke(
            new WebApiLib.Request(
                WebApiLib.WebApiMethodNames.Geo_FetchImageTile,
                new List<WebApiLib.Argument>
                { new WebApiLib.Argument("zoomlevel", _tileData.ZoomLevel),
                  new WebApiLib.Argument("x", _tileData.X),
                  new WebApiLib.Argument("y", _tileData.Y) }));

        try
        {
            var imageB64 = resp.Arguments[0].Value as string;
            _downloadedData = Convert.FromBase64String(imageB64);
        }
        catch (Exception ex)
        {
            //TODO * React to this
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    protected virtual void OnEndLoad()
    {
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    protected virtual void OnUpdate()
    {
    }
}

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class MapTile : DynamicTextureDownloader
{
    bool _useWebApiClient = false;

    public IMapUrlBuilder MapBuilder { get; set; }
    private string _txtExtenstion = "txt";
    private static object _webReqLock = new object();
    public int MaxElevation { get; private set; }
    public int MinElevation { get; private set; }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public MapTile()
    {
        MapBuilder = MapBuilder != null ? MapBuilder : new OpenStreetMapTileBuilder();
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="tiledata"></param>
    /// <param name="forceReload"></param>
    //*************************************************************************
    public void SetTileData(TileInfo tiledata, bool forceReload = false)
    {
        if (_tileData == null || !_tileData.Equals(tiledata) || forceReload)
        {
            TileData = tiledata;

            if (UseCache & TileCache.DoesExist(_tileData.ZoomLevel, _tileData.X, _tileData.Y,
                TileCache.DataTypeEnum.Elevation, TileCache.DataProviderEnum.VirtualEarth, _txtExtenstion))
            {
                var textData = TileCache.FetchText(_tileData.ZoomLevel, _tileData.X, _tileData.Y,
                TileCache.DataTypeEnum.Elevation, TileCache.DataProviderEnum.VirtualEarth, _txtExtenstion);

                IsDownloading = false;
                var elevationData = JsonUtility.FromJson<ElevationResult>(textData);
                if (elevationData == null)
                    return;

                ApplyElevationData(elevationData);
            }
            else
                StartLoadElevationDataFromWeb();
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public TileInfo TileData
    {
        get { return _tileData; }
        private set
        {
            _tileData = value;
            ImageUrl = MapBuilder.GetTileUrl(_tileData);
        }
    }

    private string _mapToken = "1YbJbm5qzuyhOtIXL9nR~DTuaBfSuBZjNtROtKSqW6A~AoT9FlvKL7Zza_Xjc47s0VW8g99CujhQscZMUyboUTsoqgoGHJoB283DEK-qUFKu";

    public bool IsDownloading { get; private set; }

    private UnityEngine.Networking.UnityWebRequest _downloader = null;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    private void StartLoadElevationDataFromWeb()
    {
        if (_tileData == null)
        {
            return;
        }
            
        if (null == _webApiClient)
            _webApiClient = WebApiLib.WebApiClient.Singleton;

        //can we fetch from paired ApiService?
        if(_useWebApiClient)
        if(_webApiClient.IsGeoTileServer)
        {
            //Call async, don't await.
            FetchElevationTileFromWebApiAsync();
            return;
        }

        //fetch from third party tile server

        var northEast = _tileData.GetNorthEast();
        var southWest = _tileData.GetSouthWest();

        var urlData = string.Format(
        "http://dev.virtualearth.net/REST/v1/Elevation/Bounds?bounds={0},{1},{2},{3}&rows=11&cols=11&key={4}",
         southWest.Lat, southWest.Lon, northEast.Lat, northEast.Lon, _mapToken);

        // this URL can only be called 5 times per second
        // https://social.msdn.microsoft.com/Forums/en-US/3e8b767d-36ee-44bf-92f1-ccb94e20779c/too-many-requests-error-started-on-21617

        // critical section, force 200ms between web requests
        lock (_webReqLock)
        {
            _downloader = UnityEngine.Networking.UnityWebRequest.Get(urlData);
            _downloader.SendWebRequest();
            System.Threading.Thread.Sleep(200);
        }

        IsDownloading = true;
    }

    string _downloadedData = null;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    //*************************************************************************
    private async Task FetchElevationTileFromWebApiAsync()
    {
        IsDownloading = true;

        var resp = await _webApiClient.Invoke(
            new WebApiLib.Request(
                WebApiLib.WebApiMethodNames.Geo_FetchElevationTile,
                new List<WebApiLib.Argument>
                { new WebApiLib.Argument("zoomlevel", _tileData.ZoomLevel), 
                  new WebApiLib.Argument("x", _tileData.X),
                  new WebApiLib.Argument("y", _tileData.Y) }));

        _downloadedData = resp.Arguments[0].Value.ToString();
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    protected override void OnUpdate()
    {
        ProcessElevationDataFromWeb();
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    private void ProcessElevationDataFromWeby()
    {
        if (TileData == null || _downloader == null)
        {
            return;
        }

        if (IsDownloading && _downloader.isDone)
        {
            IsDownloading = false;
            var elevationData = JsonUtility.FromJson<ElevationResult>(_downloader.downloadHandler.text);
            if (elevationData == null)
                return;

            if (UseCache)
                TileCache.Store(_downloader.downloadHandler.text, _tileData.ZoomLevel,
                    _tileData.X, _tileData.Y, TileCache.DataTypeEnum.Elevation,
                    TileCache.DataProviderEnum.VirtualEarth, _txtExtenstion);

            ApplyElevationData(elevationData);
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    private void ProcessElevationDataFromWeb()
    {
        if (!IsDownloading)
            return;

        if (null != _downloadedData)
        {
            IsDownloading = false;
            ProcessElevationData(_downloadedData);
            return;
        }

        if (_downloader == null)
            return;

        if (_downloader.isDone)
        {
            IsDownloading = false;
            ProcessElevationData(_downloader.downloadHandler.text);
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    //*************************************************************************
    private void ProcessElevationData(string data)
    {
        var elevationData = JsonUtility.FromJson<ElevationResult>(data);
        if (elevationData == null)
            return;

        if (UseCache)
            TileCache.Store(data, _tileData.ZoomLevel, 
                _tileData.X, _tileData.Y, TileCache.DataTypeEnum.Elevation,
                TileCache.DataProviderEnum.VirtualEarth, _txtExtenstion);

        ApplyElevationData(elevationData);
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="elevationData"></param>
    //*************************************************************************
    private void ApplyElevationData(ElevationResult elevationData)
    {
        try
        {
            var threeDScale = TileData.ScaleFactor;
            threeDScale /= 20;

            if (0 == elevationData.resourceSets.Count)
            {
                return;
            }
            var resource = elevationData.resourceSets[0].resources[0];

            var verts = new List<Vector3>();
            var mesh = GetComponent<MeshFilter>().mesh;
            for (var i = 0; i < mesh.vertexCount; i++)
            {
                MaxElevation = Math.Max(MaxElevation, resource.elevations[i]);
                MinElevation = Math.Min(MinElevation, resource.elevations[i]);

                var newPos = mesh.vertices[i];
                newPos.y = resource.elevations[i];
                verts.Add(newPos);
            }
            RebuildMesh(mesh, verts);
        }
        catch(Exception ex)
        {
            var tt = ex.Message;
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="verts"></param>
    //*************************************************************************
    private void RebuildMesh(Mesh mesh, List<Vector3> verts)
    {
        mesh.SetVertices(verts);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        DestroyImmediate(gameObject.GetComponent<MeshCollider>());
        var meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }
}
