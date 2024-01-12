using Godot;

namespace ProceduralCityGenerator.Scripts.DataStructure;

public class Radial : Field
{
    public Radial(Vector2 center, float size, float decay) : base(center, size, decay)
    {
    }

    protected override Tensor GetTensor(Vector2 point)
    {
        var t = point - center;
        var t1 = Mathf.Pow(t.Y, 2) - Mathf.Pow(t.X, 2);
        var t2 = -2 * t.X * t.Y;

        return new Tensor(1, new[] { t1, t2 });
    }
}