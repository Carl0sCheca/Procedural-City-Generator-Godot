using Godot;

namespace ProceduralCityGenerator.Scripts.DataStructure;

public class Integrator
{
    private readonly float _step;
    private readonly TensorField _tensorField;

    public Integrator(TensorField tensorField, float step)
    {
        _tensorField = tensorField;
        _step = step;
    }

    private Vector2 SampleFieldVector(Vector2 point, bool major)
    {
        var tensor = _tensorField.GetPoint(point);

        return major ? tensor.Major : tensor.Minor;
    }

    public Vector2 Integrate(Vector2 point, bool major)
    {
        var k1 = SampleFieldVector(point, major);
        var k23 = SampleFieldVector(point + new Vector2(_step / 2f, _step / 2f), major);
        var k4 = SampleFieldVector(point + new Vector2(_step, _step), major);

        return (k1 + 4 * k23 + k4) * (_step / 6f);
    }
}