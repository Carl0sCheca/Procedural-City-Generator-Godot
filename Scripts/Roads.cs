using System.Collections.Generic;
using Godot;
using ProceduralCityGenerator.Scripts.DataStructure;

namespace ProceduralCityGenerator.Scripts;

public class Roads
{
    private readonly Integrator _integrator;
    private readonly Vector2 _origin;
    private readonly Vector2 _worldDimension;

    private List<Roads> _existingStreamlines;

    public Roads(StreamLinesParams streamLinesParams, Integrator integrator, Vector2 worldDimension, Vector2 origin)
    {
        StreamLinesParams = streamLinesParams;
        _integrator = integrator;
        _worldDimension = worldDimension;
        _origin = origin;
        _existingStreamlines = new List<Roads>();
    }

    private StreamLinesParams StreamLinesParams { get; }
    public StreamLines Streamlines { get; set; }

    public void GenerateRoads()
    {
        Streamlines = new StreamLines(_integrator, _worldDimension, _origin, StreamLinesParams);

        foreach (var road in _existingStreamlines)
            if (road.Streamlines != null)
                Streamlines.AddExistingStreamlines(road.Streamlines);

        Streamlines.CreateAllStreamLines();
    }

    public void SetExistingStreamlines(List<Roads> roadsList)
    {
        _existingStreamlines = roadsList;
    }

    public void DrawLineRoads(LineDrawer3D line, Color color)
    {
        if (Streamlines == null)
            return;

        foreach (var plist in Streamlines.allStreamlinesSimple)
            for (var i = 0; i < plist.Count - 1; i++)
            {
                var vector1 = new Vector3(plist[i].X, plist[i].Y, 0);
                var vector2 = new Vector3(plist[i + 1].X, plist[i + 1].Y, 0);
                line.AddLine(vector1, vector2, color);
            }
    }
}