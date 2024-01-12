using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ProceduralCityGenerator.Scripts;

public partial class MainSidewalk3D : CsgPolygon3D
{
    public MainSidewalk3D(IEnumerable<Vector2> polygon)
    {
        Mode = ModeEnum.Depth;
        Name = "Sidewalk";
        Polygon = polygon.Select(v => new Vector2(v.X, -v.Y)).ToArray();
        Scale = new Vector3(Scale.X, Scale.Y, 0.015f);
        Material = (Material)ResourceLoader.Load("res://Materials/SidewalkMaterial1.tres");
    }

    public static MainSidewalk3D Bottom(List<Vector2> polygons)
    {
        var sidewalk = new MainSidewalk3D(polygons);
        sidewalk.Scale = new Vector3(sidewalk.Scale.X, sidewalk.Scale.Y, 0.005f);
        sidewalk.Material = (Material)ResourceLoader.Load("res://Materials/RoadMaterial1.tres");
        return sidewalk;
    }
}