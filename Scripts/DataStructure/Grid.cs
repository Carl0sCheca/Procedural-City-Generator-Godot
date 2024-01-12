using Godot;

namespace ProceduralCityGenerator.Scripts.DataStructure;

public class Grid : Field
{
    public float theta;

    public Grid(Vector2 center, float size, float decay, float theta) : base(center, size, decay)
    {
        this.theta = theta;
    }

    protected override Tensor GetTensor(Vector2 point)
    {
        return new Tensor(1, new[] { Mathf.Cos(2 * theta), Mathf.Sin(2 * theta) });
    }
}