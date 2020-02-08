using System;
using UnityEngine;

public class TileInfo : IEquatable<TileInfo>
{
    public float MapTileSize { get; private set; }

    public int MapPixelSize = 256;

    public WorldCoordinate CenterLocation { get; private set; }

    public TileInfo(WorldCoordinate centerLocation, int zoom, float mapTileSize)
    {
        SetStandardValues(mapTileSize);
        CenterLocation = new WorldCoordinate( centerLocation );

        //http://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#Tile_numbers_to_lon..2Flat._2
        var latrad = centerLocation.Lat * Mathf.Deg2Rad;
        var n = Math.Pow(2, zoom);
        X = (int)((centerLocation.Lon + 180.0) / 360.0 * n);
        Y = (int)((1.0 - Mathf.Log(Mathf.Tan(latrad) + 1 / Mathf.Cos(latrad)) / Mathf.PI) / 2.0 * n);
        ZoomLevel = zoom;
    }

    public TileInfo(int x, int y, int zoom, float mapTileSize, WorldCoordinate centerTileCenterLocation)
    {
        SetStandardValues(mapTileSize);
        X = x;
        Y = y;
        ZoomLevel = zoom;
        CenterLocation = centerTileCenterLocation;
    }

    private void SetStandardValues(float mapTileSize)
    {
        MapTileSize = mapTileSize;
    }

    public int X { get; set; }
    public int Y { get; set; }

    public int ZoomLevel { get; private set; }

    public override bool Equals(object obj)
    {
        return Equals(obj as TileInfo);
    }

    public override string ToString()
    {
        return string.Format("X={0},Y={1},zoom={2}", X, Y, ZoomLevel);
    }

    public virtual bool Equals(TileInfo other)
    {
        if (other != null)
        {
            return X == other.X && Y == other.Y && ZoomLevel == other.ZoomLevel;
        }

        return base.Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = X;
            hashCode = (hashCode * 397) ^ Y;
            hashCode = (hashCode * 397) ^ ZoomLevel;
            return hashCode;
        }
    }

    //http://wiki.openstreetmap.org/wiki/Zoom_levels
    private static readonly float[] _zoomScales =
    {
        156412f, 78206f, 39103f, 19551f, 9776f, 4888f, 2444f,
        1222f, 610.984f, 305.492f, 152.746f, 76.373f, 38.187f,
        19.093f, 9.547f, 4.773f, 2.387f, 1.193f, 0.596f, 0.298f, 0.149f
    };

    public static double PixelSizeMeters(float latitudeDegrees, int zoomLevel)
    {
        return (156543.03 * Math.Cos(latitudeDegrees * Mathf.Deg2Rad)) / Math.Pow(2.0, zoomLevel); 
    }

    public static double TileSizeMeters(float latitudeDegrees, int zoomLevel, int pixelsPerTile)
    {
        return PixelSizeMeters(latitudeDegrees, zoomLevel) * pixelsPerTile;
    }

    public double MetersPerPixel
    {
        get { return (156543.03 * Math.Cos(CenterLocation.Lat * Mathf.Deg2Rad)) / Math.Pow(2.0, ZoomLevel); }
    }

    public double ScaleFactor
    {
        get { return MetersPerPixel * MapPixelSize; }
    }

    public float ScaleFactorold
    {
        get 
        {
            var v1 = ScaleFactor;
            var v2 = _zoomScales[ZoomLevel] * MapPixelSize;
            return _zoomScales[ZoomLevel] * MapPixelSize;
        }
    }

    public WorldCoordinate GetNorthEast()
    {
        return GetNorthWestLocation(X+1, Y, ZoomLevel);
    }

    public WorldCoordinate GetSouthWest()
    {
        return GetNorthWestLocation(X, Y+1, ZoomLevel);
    }

//http://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#C.23
private WorldCoordinate GetNorthWestLocation(int tileX, int tileY, int zoomLevel)
{
    var p = new WorldCoordinate();
    var n = Math.Pow(2.0, zoomLevel);
    p.Lon = (float)(tileX / n * 360.0 - 180.0);
    var latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileY / n)));
    p.Lat = (float) (latRad * 180.0 / Math.PI);
    return p;
}
}
