using System.Collections.Generic;
using Godot;

public partial class MainRoads3D : CsgPolygon3D
{
    public override void _Ready()
    {
        const float width = 0.5f;
        const float height = 0.0125f;
        const float halfWidth = width * 0.5f;
        const float halfHeight = height * 0.5f;
        Polygon = new Vector2[]
        {
            new(-halfWidth, -halfHeight),
            new(-halfWidth, height - halfHeight),
            new(width - halfWidth, height - halfHeight),
            new(width - halfWidth, -halfHeight)
        };
        Mode = ModeEnum.Path;
        Name = "Road";
        Material = (Material)ResourceLoader.Load("res://Materials/RoadMaterial1.tres");
        PathIntervalType = PathIntervalTypeEnum.Subdivide;
        PathInterval = 0.1f;
    }

    public void AddPath(List<Vector3> points)
    {
        var path3d = new Path3D();
        path3d.Name = "Path";
        path3d.Curve = new Curve3D();

        foreach (var point in points) path3d.Curve.AddPoint(point);

        AddChild(path3d);
        PathNode = path3d.GetPath();
    }
}