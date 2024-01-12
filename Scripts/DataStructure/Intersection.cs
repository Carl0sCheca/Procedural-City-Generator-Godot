using System.Collections.Generic;
using Godot;

namespace ProceduralCityGenerator.Scripts.DataStructure;

public struct Intersection
{
    public Vector2 point;
    public HashSet<Intersection> neighbours;
    public List<Segment> segments;
}