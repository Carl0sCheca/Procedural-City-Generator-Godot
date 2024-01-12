using Godot;

namespace ProceduralCityGenerator.Scripts;

public partial class RayCast3D : Godot.RayCast3D
{
    public RayCast3D(Vector3 position)
    {
        Enabled = true;
        Position = position;
        CollideWithBodies = true;
        TargetPosition = new Vector3(0, -13, 0);
    }
}