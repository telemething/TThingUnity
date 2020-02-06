public class WorldCoordinate
{
    public float Lon { get; set; }
    public float Lat { get; set; }

    public WorldCoordinate()
    {
    }

    public WorldCoordinate(WorldCoordinate wcIn)
    {
        Lon = wcIn.Lon;
        Lat = wcIn.Lat;
    }

    public override string ToString()
    {
        return string.Format("lat={0},lon={1}", Lat, Lon);
    }
}
