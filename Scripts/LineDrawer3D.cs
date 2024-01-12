using System.Collections.Generic;
using Godot;
using ProceduralCityGenerator.Scripts.DataStructure;

public partial class LineDrawer3D : MeshInstance3D, IPointDrawImmediate
{
    private readonly List<IPointDrawImmediate.Point> _points = new();
    private ImmediateMesh _mesh = new();

    public void AddLine(Vector3 p1, Vector3 p2, Color color)
    {
        _points.Add(new IPointDrawImmediate.Point { point = p1, color = color });
        _points.Add(new IPointDrawImmediate.Point { point = p2, color = color });
    }

    public void AddLine(Vector3 p1, Vector3 p2)
    {
        AddLine(p1, p2, Colors.Red);
    }

    public void AddLines(List<Vector2> line)
    {
        AddLines(line, Colors.Red);
    }

    public void AddLines(List<Vector2> line, Color color)
    {
        if (line.Count < 2) return;

        foreach (var point in line)
            _points.Add(new IPointDrawImmediate.Point { point = new Vector3(point.X, point.Y, 0), color = color });
    }

    public override void _Ready()
    {
        var m = new StandardMaterial3D();
        m.NoDepthTest = true;
        m.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        m.VertexColorUseAsAlbedo = true;
        m.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        MaterialOverride = m;
    }

    public void Clear()
    {
        _mesh.ClearSurfaces();
        _points.Clear();
    }

    public override void _Process(double delta)
    {
        if (_points.Count == 0) return;

        _mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        foreach (var t in _points)
        {
            _mesh.SurfaceSetColor(t.color);
            _mesh.SurfaceAddVertex(t.point);
        }

        _mesh.SurfaceEnd();

        Mesh = _mesh;
    }
}