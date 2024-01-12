using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ProceduralCityGenerator.Scripts;

public partial class MainBuildings3D : CsgPolygon3D
{
    private MainBuildings3D()
    {
    }

    public MainBuildings3D(IEnumerable<Vector2> polygon, float height)
    {
        Mode = ModeEnum.Depth;
        Name = "Building";
        Polygon = polygon.Select(v => new Vector2(v.X, -v.Y)).ToArray();
        Scale = new Vector3(Scale.X, Scale.Y, height);
    }

    public static MainBuildings3D SetTop(MainBuildings3D building)
    {
        var top = new MainBuildings3D();
        top.Mode = ModeEnum.Depth;
        top.Polygon = building.Polygon.ToArray();
        top.Name = "Building Top";
        top.Scale = new Vector3(building.Scale.X, building.Scale.Y, 0.005f);
        top.Position = new Vector3(building.Position.X, building.Position.Y, -building.Scale.Z);
        top.Material = (Material)ResourceLoader.Load("res://Materials/BuildingRoofMaterial1.tres");
        return top;
    }
}