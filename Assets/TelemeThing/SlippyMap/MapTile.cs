
//using HoloToolkitExtensions.RemoteAssets;
using System.Collections;
using UnityEngine;

// https://github.com/LocalJoost/UnityTextureDownload/blob/master/Assets/App/DynamicTextureDownloader.cs
// https://github.com/LocalJoost/TextureLoaderError/blob/master/Assets/Assets/HoloToolkitExtensions/Scripts/RemoteAssets/DynamicTextureDownloader.cs
// http://dotnetbyexample.blogspot.com/2017/02/a-behaviour-for-dynamically-loading-and.html
// http://dotnetbyexample.blogspot.com/2018/10/workaround-remote-texture-loading-does.html

public class DynamicTextureDownloadery : MonoBehaviour
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

public class MapTiley : DynamicTextureDownloadery
{
    public IMapUrlBuildery MapBuilder { get; set; }

    private TileInfoy _tileData;

    public MapTiley()
    {
        MapBuilder = MapBuilder != null ? MapBuilder : new OpenStreetMapTileBuildery();
    }

    public void SetTileData(TileInfoy tiledata, bool forceReload = false)
    {
        if (_tileData == null || !_tileData.Equals(tiledata) || forceReload)
        {
            TileData = tiledata;
        }
    }

    public TileInfoy TileData
    {
        get { return _tileData; }
        private set
        {
            _tileData = value;
            ImageUrl = MapBuilder.GetTileUrl(_tileData);
        }
    }
}
