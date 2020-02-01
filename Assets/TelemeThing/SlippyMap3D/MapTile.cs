
//using HoloToolkitExtensions.RemoteAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// https://github.com/LocalJoost/UnityTextureDownload/blob/master/Assets/App/DynamicTextureDownloader.cs
// https://github.com/LocalJoost/TextureLoaderError/blob/master/Assets/Assets/HoloToolkitExtensions/Scripts/RemoteAssets/DynamicTextureDownloader.cs
// http://dotnetbyexample.blogspot.com/2017/02/a-behaviour-for-dynamically-loading-and.html
// http://dotnetbyexample.blogspot.com/2018/10/workaround-remote-texture-loading-does.html

public class DynamicTextureDownloader : MonoBehaviour
{
    public string ImageUrl;
    public bool ResizePlane;

    private WWW _imageLoader = null;
    private string _previousImageUrl = null;
    private bool _appliedToTexture = false;

    private Vector3 _originalScale;

    void Start()
    {
        _originalScale = transform.localScale;
    }

    void Update()
    {
        CheckLoadImage();
        OnUpdate();
    }

    private void CheckLoadImage()
    {
        // No image requested
        if (string.IsNullOrEmpty(ImageUrl))
        {
            return;
        }

        // New image set - reset status vars and start loading new image
        if (_previousImageUrl != ImageUrl)
        {
            _previousImageUrl = ImageUrl;

            OnStartLoad();
        }

        if (_imageLoader != null && _imageLoader.isDone && !_appliedToTexture)
        {
            // Apparently an image was loading and is now done. Get the texture and apply
            _appliedToTexture = true;

            //Destroy(GetComponent<Renderer>().material.mainTexture);
            GetComponent<Renderer>().material.mainTexture = _imageLoader.texture;
            Destroy(_imageLoader.texture);
            _imageLoader = null;

            if (ResizePlane)
            {
                DoResizePlane();
            }
            OnEndLoad();
        }
    }

    private void DoResizePlane()
    {
        // Keep the longest edge at the same length
        if (_imageLoader.texture.width < _imageLoader.texture.height)
        {
            transform.localScale = new Vector3(
                _originalScale.z * _imageLoader.texture.width / _imageLoader.texture.height,
                _originalScale.y, _originalScale.z);
        }
        else
        {
            transform.localScale = new Vector3(
                _originalScale.x, _originalScale.y,
                _originalScale.x * _imageLoader.texture.height / _imageLoader.texture.width);
        }
    }

    protected virtual void OnStartLoad()
    {
        _appliedToTexture = false;
        _imageLoader = new WWW(ImageUrl);
    }

    protected virtual void OnEndLoad()
    {

    }

    protected virtual void OnUpdate()
    {

    }
}


public class MapTile : DynamicTextureDownloader
{
    public IMapUrlBuilder MapBuilder { get; set; }

    private TileInfo _tileData;

    public MapTile()
    {
        MapBuilder = MapBuilder != null ? MapBuilder : new OpenStreetMapTileBuilder();
    }

    public void SetTileData(TileInfo tiledata, bool forceReload = false)
    {
        if (_tileData == null || !_tileData.Equals(tiledata) || forceReload)
        {
            TileData = tiledata;
            StartLoadElevationDataFromWeb();
        }
    }

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

    private WWW _downloader;

    private void StartLoadElevationDataFromWeb()
    {
        if (_tileData == null)
        {
            return;
        }
        var northEast = _tileData.GetNorthEast();
        var southWest = _tileData.GetSouthWest();

        var urlData = string.Format(
        "http://dev.virtualearth.net/REST/v1/Elevation/Bounds?bounds={0},{1},{2},{3}&rows=11&cols=11&key={4}",
         southWest.Lat, southWest.Lon, northEast.Lat, northEast.Lon, _mapToken);

        // this URL can only be called 5 times per second
        // https://social.msdn.microsoft.com/Forums/en-US/3e8b767d-36ee-44bf-92f1-ccb94e20779c/too-many-requests-error-started-on-21617

        _downloader = new WWW(urlData);
        IsDownloading = true;
    }

    protected override void OnUpdate()
    {
        ProcessElevationDataFromWeb();
    }

    private void ProcessElevationDataFromWeb()
    {
        if (TileData == null || _downloader == null)
        {
            return;
        }

        if (IsDownloading && _downloader.isDone)
        {
            IsDownloading = false;
            var elevationData = JsonUtility.FromJson<ElevationResult>(_downloader.text);
            if (elevationData == null)
            {
                return;
            }

            ApplyElevationData(elevationData);
        }
    }

    private void ApplyElevationData(ElevationResult elevationData)
    {
        try
        {
            var threeDScale = TileData.ScaleFactor;

            //temp
            threeDScale = 300;

            if (0 == elevationData.resourceSets.Count)
            {
                return;
            }
            var resource = elevationData.resourceSets[0].resources[0];

            var verts = new List<Vector3>();
            var mesh = GetComponent<MeshFilter>().mesh;
            for (var i = 0; i < mesh.vertexCount; i++)
            {
                var newPos = mesh.vertices[i];
                newPos.y = resource.elevations[i] / threeDScale;
                verts.Add(newPos);
            }
            RebuildMesh(mesh, verts);
        }
        catch(Exception ex)
        {
            var tt = ex.Message;
        }
    }

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
