using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
class SpiralOut
{
    int layer = 1;
    int leg  = 0;
   
    private int x = 0, y = 0;

    public int X => x;
    public int Y => y;

    public SpiralOut()
    { }

    public void goNext()
    {
        switch (leg)
        {
            case 0: ++x; if (x == layer) ++leg; break;
            case 1: ++y; if (y == layer) ++leg; break;
            case 2: --x; if (-x == layer) ++leg; break;
            case 3: --y; if (-y == layer) { leg = 0; ++layer; } break;
        }
    }
};

//*****************************************************************************
/// <summary>
/// 
/// </summary>
//*****************************************************************************
public class MapBuilder : MonoBehaviour
{
    public int ZoomLevel = 12;
    public float MapTileSize = 0.5f;
    public float Latitude = 47.642567f;
    public float Longitude = -122.136919f;
    public bool BuildOnStart = false;

    public float CenterTileTopLeftLatitude { get; private set; }
    public float CenterTileTopLeftLongitude { get; private set; }
    public int MaxElevation { get; private set; }
    public int MinElevation { get; private set; }

    public float MapTotal2DEdgeLength { get { 
            return MapTileSize * (MapSize + 1); } }
    public float MapTotal2DDiagonalLength { get { 
            return (float)Math.Sqrt(MapTotal2DEdgeLength * MapTotal2DEdgeLength * 2); } }
    public float MapTotal3DDiagonalLength { get { 
            return (float)Math.Sqrt((MapTotal2DDiagonalLength * MapTotal2DDiagonalLength) + (MaxElevation * MaxElevation)); } }

    public GameObject MapTilePrefab;

    // the number of tiles per edge (-1 because center tile)
    public float MapSize = 12;

    private TileInfo _centerTile;
    private List<MapTile> _mapTiles;

    private UnityEngine.UI.Text _infoTextLarge = null;
    private IEnumerator _loadTilesCoroutine = null;

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    void Start()
    {
        _infoTextLarge = Utils.FindObjectComponentInScene<UnityEngine.UI.Text>("InfoTextLarge");

        if (BuildOnStart)
        {
            _mapTiles = new List<MapTile>();
            ShowMap();
        }
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="lat"></param>
    /// <param name="lon"></param>
    /// <param name="zoom"></param>
    /// <param name="size"></param>
    //*************************************************************************
    public void ShowMap(float lat, float lon, int zoom, int size)
    {
        _infoTextLarge = Utils.FindObjectComponentInScene<UnityEngine.UI.Text>("InfoTextLarge");

        Latitude = lat;
        Longitude = lon;
        ZoomLevel = zoom;
        MapSize = size;

        _mapTiles = new List<MapTile>();
        ShowMap();
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*************************************************************************
    public void ShowMap()
    {
        if(0 == MapTileSize)
            MapTileSize = (float)TileInfo.TileSizeMeters(Latitude, ZoomLevel, 256);

        _centerTile = new TileInfo(new WorldCoordinate { Lat = Latitude, Lon = Longitude }, 
            ZoomLevel, MapTileSize);

        var LL = _centerTile.TopLeftLatLon();

        CenterTileTopLeftLatitude = LL.Lat;
        CenterTileTopLeftLongitude = LL.Lon;

        //_centerTile.X -= (int)(MapTileSize / 2.0f);
        //_centerTile.Y += (int)(MapTileSize / 2.0f);

        //LoadTiles();
        _loadTilesCoroutine = LoadTiles();
        StartCoroutine(_loadTilesCoroutine);

        //Camera.main.farClipPlane = 10000;

        if (BuildOnStart)
        {
            //assuming camera is in center, set clip plane to encompas entire map
            Camera.main.farClipPlane = Math.Max(
                Camera.main.farClipPlane, MapTotal2DDiagonalLength);
        }
    }

    //*************************************************************************
    /// <summary>
    /// Build map tiles, spiral from center out
    /// </summary>
    /// <param name="forceReload"></param>
    //*************************************************************************
    private IEnumerator LoadTiles(bool forceReload = false)
    {
        var countFetched = 0;
        var tileIndex = 0;
        var tileCount = (MapSize + 1) * (MapSize + 1);

        SpiralOut so = new SpiralOut();

        for (int layer = 0; layer < tileCount; layer++)
        {
            _infoTextLarge.text = $"Fetching Tile: {countFetched++} of: {(MapSize + 1) * (MapSize + 1) - 1}";

            var tile = GetOrCreateTile(so.X, so.Y, tileIndex++);

            tile.SetTileData(new TileInfo(
                _centerTile.X - so.X, _centerTile.Y + so.Y,
                ZoomLevel, MapTileSize, _centerTile.CenterLocation),
                forceReload);

            tile.gameObject.name = string.Format("({0},{1}) - {2},{3}", so.X, so.Y, tile.TileData.X,
                tile.TileData.Y);

            MaxElevation = Math.Max(MaxElevation, tile.MaxElevation);
            MinElevation = Math.Min(MinElevation, tile.MinElevation);

            so.goNext();

            if(0 == tileIndex % 4)
                yield return null;
        }

        _infoTextLarge.text = $"";
    }

    private IEnumerator LoadTilesOld(bool forceReload = false)
    {
        var size = (int)(MapSize / 2);
        var countFetched = 0;

        var tileIndex = 0;
        for (var x = -size; x <= size; x++)
        {
            for (var y = -size; y <= size; y++)
            {
                _infoTextLarge.text = $"Fetching Tile: {countFetched++} of: {(MapSize + 1) * (MapSize + 1) - 1}";

                var tile = GetOrCreateTile(x, y, tileIndex++);

                tile.SetTileData(new TileInfo(
                    _centerTile.X - x, _centerTile.Y + y,
                    ZoomLevel, MapTileSize, _centerTile.CenterLocation),
                    forceReload);

                tile.gameObject.name = string.Format("({0},{1}) - {2},{3}", x, y, tile.TileData.X,
                    tile.TileData.Y);

                MaxElevation = Math.Max(MaxElevation, tile.MaxElevation);
                MinElevation = Math.Min(MinElevation, tile.MinElevation);

                // temporary
                //System.Threading.Thread.Sleep(200);

                yield return null;
            }
        }

        _infoTextLarge.text = $"";
    }

    //*************************************************************************
    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    //*************************************************************************
    private MapTile GetOrCreateTile(int x, int y, int i)
    {
        if (_mapTiles.Any() && _mapTiles.Count > i)
        {
            return _mapTiles[i];
        }

        var mapTile = Instantiate(MapTilePrefab, transform);

        var oo = MapTilePrefab.transform.localScale;
        var ff = mapTile.transform.localScale;

        //mapTile.transform.localScale = new Vector3(MapTileSize / 10.0f, 20 * MapTileSize / 10.0f, MapTileSize / 10.0f);
        //mapTile.transform.localScale = new Vector3(MapTileSize / 10.0f, MapTileSize / 10.0f, MapTileSize / 10.0f);
        mapTile.transform.localScale = new Vector3(MapTileSize / 10.0f, 1, MapTileSize / 10.0f);

        mapTile.transform.localPosition = new Vector3(MapTileSize * x - MapTileSize / 2, 0, MapTileSize * y + MapTileSize / 2);
        mapTile.transform.localRotation = Quaternion.identity;
        var tile = mapTile.GetComponent<MapTile>();
        _mapTiles.Add(tile);
        return tile;
    }
}