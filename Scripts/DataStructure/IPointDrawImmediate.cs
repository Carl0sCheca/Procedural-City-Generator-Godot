using Godot;

namespace ProceduralCityGenerator.Scripts.DataStructure;

public interface IPointDrawImmediate
{
    internal struct Point
    {
        internal Vector3 point;
        internal Color color;
    }
}