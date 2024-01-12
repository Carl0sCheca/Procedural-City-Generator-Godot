using System.Collections.Generic;
using Godot;

namespace ProceduralCityGenerator.Scripts.DataStructure;

public struct StreamLineIntegration
{
    public Vector2 seed;
    public Vector2 originalDir;
    public List<Vector2> streamline;
    public Vector2 previousDirection;
    public Vector2 previousPoint;
    public bool valid;
}