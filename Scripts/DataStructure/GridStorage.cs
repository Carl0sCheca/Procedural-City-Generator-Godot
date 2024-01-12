using System.Collections.Generic;
using Godot;

namespace ProceduralCityGenerator.Scripts.DataStructure;

public class GridStorage
{
    private readonly List<List<List<Vector2>>> _grid;
    private readonly Vector2 _gridDimensions;
    private readonly Vector2 _origin;
    private readonly float _sep;
    private readonly Vector2 _worldDimensions;

    public GridStorage(Vector2 worldDimensions, Vector2 origin, float sep)
    {
        _grid = new List<List<List<Vector2>>>();
        _origin = origin;
        _sep = sep;
        _worldDimensions = worldDimensions;
        _gridDimensions = _worldDimensions / _sep;

        for (var i = 0; i < _gridDimensions.X; i++)
        {
            _grid.Add(new List<List<Vector2>>());
            for (var j = 0; j < _gridDimensions.Y; j++) _grid[i].Add(new List<Vector2>());
        }
    }

    public void AddAll(GridStorage gridStorage)
    {
        foreach (var row in gridStorage._grid)
        foreach (var cell in row)
        foreach (var sample in cell)
            AddSample(sample);
    }

    public List<Vector2> GetNearbyPoints(Vector2 v, float distance)
    {
        var radius = (int)Mathf.Ceil(distance / _sep - 0.5f);
        var coords = GetSampleCoords(v);
        var outVectors = new List<Vector2>();

        for (var i = -1 * radius; i <= 1 * radius; i++)
        for (var j = -1 * radius; j <= 1 * radius; j++)
        {
            var cell = coords + new Vector2(i, j);
            if (!VectorOutOfBounds(cell, _gridDimensions))
                foreach (var v2 in _grid[(int)cell.X][(int)cell.Y])
                    outVectors.Add(v2);
        }

        return outVectors;
    }

    public void AddSample(Vector2 v)
    {
        var coords = GetSampleCoords(v);

        AddSample(v, coords);
    }

    private void AddSample(Vector2 v, Vector2 coords)
    {
        _grid[(int)coords.X][(int)coords.Y].Add(v);
    }

    private Vector2 GetSampleCoords(Vector2 worldV)
    {
        var v = worldV - _origin;

        if (VectorOutOfBounds(v, _worldDimensions)) return Vector2.Zero;

        return new Vector2(Mathf.Floor(v.X / _sep), Mathf.Floor(v.Y / _sep));
    }

    public bool IsValidSample(Vector2 v, float dSq)
    {
        var coords = GetSampleCoords(v);

        for (var i = -1; i <= 1; i++)
        for (var j = -1; j <= 1; j++)
        {
            var cell = coords + new Vector2(i, j);

            if (!VectorOutOfBounds(cell, _gridDimensions))
                if (!VectorFarFromVectors(v, _grid[(int)cell.X][(int)cell.Y], dSq))
                    return false;
        }

        return true;
    }

    private static bool VectorFarFromVectors(Vector2 v, List<Vector2> vectors, float dSq)
    {
        foreach (var sample in vectors)
            if (!sample.Equals(v))
            {
                var distanceSq = sample.DistanceSquaredTo(v);

                if (distanceSq < dSq) return false;
            }

        return true;
    }

    private static bool VectorOutOfBounds(Vector2 gridV, Vector2 bounds)
    {
        return gridV.X < 0 || gridV.Y < 0 ||
               gridV.X >= bounds.X || gridV.Y >= bounds.Y;
    }

    public void AddPolyline(List<Vector2> line)
    {
        foreach (var v in line) AddSample(v);
    }
}