using Godot;

namespace ProceduralCityGenerator.Scripts.DataStructure;

public abstract class Field
{
    public Vector2 center;
    public float decay;
    public float size;

    protected Field(Vector2 center, float size, float decay)
    {
        this.center = center;
        this.size = size;
        this.decay = decay;
    }

    public Tensor GetWeightedTensor(Vector2 point)
    {
        var tensor = GetTensor(point);
        tensor.r *= GetTensorWeight(point);
        tensor.oldth = true;

        return tensor;
    }

    private float GetTensorWeight(Vector2 point)
    {
        var normDistanceToCenter = (point - center).Length() / size;

        if (decay == 0 && normDistanceToCenter >= 1) return 0;

        return Mathf.Pow(Mathf.Max(0, 1 - normDistanceToCenter), decay);
    }

    protected abstract Tensor GetTensor(Vector2 point);
}