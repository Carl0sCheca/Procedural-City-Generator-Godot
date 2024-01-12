using Godot;

namespace ProceduralCityGenerator.Scripts.DataStructure;

public class Tensor
{
    private readonly float[] _m;
    private float _theta;
    public bool oldth;

    public float r;

    public Tensor(float r, float[] m)
    {
        this.r = r;
        _m = m;
        oldth = false;
        Theta = CalculateTheta();
    }

    private float Theta
    {
        get
        {
            if (oldth)
            {
                _theta = CalculateTheta();
                oldth = false;
            }

            return _theta;
        }
        init => _theta = value;
    }

    public static Tensor Zero
    {
        get { return new Tensor(0, new float[] { 0, 0 }); }
    }

    public Vector2 Major
    {
        get
        {
            var vector = Vector2.Zero;
            if (r != 0) vector = new Vector2(Mathf.Cos(Theta), Mathf.Sin(Theta));

            return vector;
        }
    }

    public Vector2 Minor
    {
        get
        {
            var vector = Vector2.Zero;
            if (r != 0)
            {
                var angle = Theta + Mathf.Pi / 2;
                vector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            return vector;
        }
    }

    private float CalculateTheta()
    {
        var theta = 0.0f;
        if (r != 0) theta = Mathf.Atan2(_m[1] / r, _m[0] / r) / 2;

        return theta;
    }

    public void Add(Tensor tensor)
    {
        for (var i = 0; i < _m.Length; i++) _m[i] = _m[i] * r + tensor._m[i] * tensor.r;

        r = 2;
        oldth = true;
    }
}