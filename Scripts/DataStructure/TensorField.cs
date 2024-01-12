using System.Collections.Generic;
using Godot;

namespace ProceduralCityGenerator.Scripts.DataStructure;

public class TensorField
{
    private readonly float _diameter;
    public readonly List<Vector2> points;

    public TensorField(Vector2 worldDimensions, Vector2 origin)
    {
        _diameter = 0.1f;
        var n = new Vector2(Mathf.Ceil(worldDimensions.X / _diameter) + 1,
            Mathf.Ceil(worldDimensions.Y / _diameter) + 1);

        Fields = new List<Field>();

        // Puntos en la rejilla
        points = new List<Vector2>();
        for (var i = 1; i < n.X - 1; i++)
        for (var j = 1; j < n.Y - 1; j++)
            points.Add(new Vector2(origin.X + i * _diameter * 2, origin.Y + j * _diameter * 2));
    }

    public List<Field> Fields { get; }

    public Tensor GetPoint(Vector2 p)
    {
        if (Fields.Count == 0) return new Tensor(1, new float[] { 0, 0 });

        var tensorAcc = Tensor.Zero;

        foreach (var f in Fields) tensorAcc.Add(f.GetWeightedTensor(p));

        return tensorAcc;
    }

    public List<Vector2> GetTensorLine(Vector2 point, Vector2 tensor)
    {
        var diff = tensor * _diameter;
        var start = point - diff;
        var end = point + diff;

        return new List<Vector2> { start, end };
    }

    public void AddRadial(Vector2 center, float size, float decay)
    {
        AddField(new Radial(center, size, decay));
    }

    public void AddGrid(Vector2 center, float size, float decay, float theta)
    {
        AddField(new Grid(center, size, decay, theta));
    }

    private void AddField(Field field)
    {
        Fields.Add(field);
    }
}