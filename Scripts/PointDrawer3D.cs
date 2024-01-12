using System.Collections.Generic;
using Godot;
using ProceduralCityGenerator.Scripts.DataStructure;

public partial class PointDrawer3D : MeshInstance3D, IPointDrawImmediate
{
    private readonly List<IPointDrawImmediate.Point> _points = new();
    private ImmediateMesh _mesh = new();

    public void AddPoint(Vector3 p)
    {
        AddPoint(p, Colors.Green);
    }

    public void AddPoint(Vector3 p, Color color)
    {
        _points.Add(new IPointDrawImmediate.Point
        {
            point = p,
            color = color
        });
    }

    public void Clear()
    {
        _mesh.ClearSurfaces();
        _points.Clear();
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

    public override void _Process(double delta)
    {
        if (_points.Count == 0) return;

        _mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

        var width = 0.1f;
        foreach (var t in _points)
        {
            _mesh.SurfaceSetColor(t.color);
            _mesh.SurfaceAddVertex(t.point + new Vector3(-1 * width, 1 * width, 0));
            _mesh.SurfaceAddVertex(t.point + new Vector3(1 * width, -1 * width, 0));
            _mesh.SurfaceAddVertex(t.point + new Vector3(-1 * width, -1 * width, 0));

            _mesh.SurfaceAddVertex(t.point + new Vector3(-1 * width, 1 * width, 0));
            _mesh.SurfaceAddVertex(t.point + new Vector3(1 * width, 1 * width, 0));
            _mesh.SurfaceAddVertex(t.point + new Vector3(1 * width, -1 * width, 0));
        }

        _mesh.SurfaceEnd();

        Mesh = _mesh;
    }
}