using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace ProceduralCityGenerator.Scripts.DataStructure;

public static class PolygonGenerator
{
    private const int MaxLength = 20;
    private static readonly GeometryFactory Gf = NtsGeometryServices.Instance.CreateGeometryFactory();

    public static List<List<Intersection>> GeneratePolygons(List<Intersection> intersections)
    {
        var polygons = new List<List<Intersection>>();

        foreach (var p in intersections)
            if (p.neighbours.Count >= 2)
                foreach (var nextPoint in p.neighbours.ToList())
                {
                    var polygon = GetPolygons(new List<Intersection> { p, nextPoint });

                    if (polygon is { Count: < MaxLength })
                    {
                        RemovePolygonAdjacency(polygon);
                        polygons.Add(polygon);
                        polygon.Add(polygon[0]);
                    }
                }

        return polygons;
    }

    private static void RemovePolygonAdjacency(IReadOnlyList<Intersection> polygon)
    {
        for (var i = 0; i < polygon.Count; i++)
        {
            var current = polygon[i];
            var next = polygon[(i + 1) % polygon.Count];

            if (current.neighbours.Contains(next)) current.neighbours.Remove(next);
        }
    }

    private static List<Intersection> GetPolygons(List<Intersection> visited, int count = 0)
    {
        List<Intersection> output = null;
        var continueLoop = true;
        while (continueLoop)
            if (count >= MaxLength)
            {
                continueLoop = false;
            }
            else
            {
                var nextIntersection = GetRightMostIntersection(visited[^2], visited[^1]);
                if (!nextIntersection.HasValue)
                {
                    continueLoop = false;
                }
                else
                {
                    var visitedIndex = visited.IndexOf(nextIntersection.Value);
                    if (visitedIndex >= 0)
                    {
                        output = visited.GetRange(visitedIndex, visited.Count - visitedIndex);
                        continueLoop = false;
                    }
                    else
                    {
                        visited.Add(nextIntersection.Value);
                        count++;
                    }
                }
            }

        return output;
    }

    private static Intersection? GetRightMostIntersection(Intersection intersectionFrom, Intersection intersectionTo)
    {
        if (intersectionTo.neighbours.Count == 0) return null;

        var backwardsDifferenceVector = intersectionFrom.point - intersectionTo.point;
        var transformAngle = Mathf.Atan2(backwardsDifferenceVector.Y, backwardsDifferenceVector.X);

        Intersection? rightMostIntersection = null;
        var smallestTheta = Mathf.Tau;

        foreach (var nextIntersection in intersectionTo.neighbours)
        {
            if (nextIntersection.Equals(intersectionFrom))
                continue;

            var nextVector = nextIntersection.point - intersectionTo.point;
            var nextAngle = Mathf.Atan2(nextVector.Y, nextVector.X) - transformAngle;

            if (nextAngle < 0) nextAngle += Mathf.Tau;

            if (nextAngle < smallestTheta)
            {
                smallestTheta = nextAngle;
                rightMostIntersection = nextIntersection;
            }
        }

        return rightMostIntersection;
    }

    public static (List<List<Vector2>>, List<List<Vector2>>, List<List<Vector2>>, List<Vector2>) GeneratePlots(
        List<List<Intersection>> polygons)
    {
        var shrunkPolygons = new List<List<Vector2>>();
        var sidewalkPolygons = new List<List<Vector2>>();
        var sidewalkFloorPolygons = new List<List<Vector2>>();
        var centroidPolygon = new List<Vector2>();

        var random = new Random();

        foreach (var polygon in polygons)
        {
            if (random.Next(0, 1001) - 1 < 5) continue;

            if (polygon.Count <= 2) continue;

            var poly = Gf.CreatePolygon(
                Gf.CreateLinearRing(polygon.Select(x => new Coordinate(x.point.X, x.point.Y))
                    .ToArray())
            );

            var plotBuilding = poly.Buffer(GD.RandRange(-0.4d, -0.8d));


            if (!plotBuilding.IsSimple || plotBuilding.Area < 1d || plotBuilding.Area > 100d) continue;

            var plotSidewalk = poly.Buffer(-0.25d);
            var plotSidewalkRounded = plotSidewalk.Buffer(-0.1d).Buffer(0.1d);

            shrunkPolygons.Add(
                plotBuilding.Coordinates.Select(c => new Vector2((float)c.X, (float)c.Y)).ToList()
            );
            sidewalkPolygons.Add(
                plotSidewalk.Coordinates.Select(c => new Vector2((float)c.X, (float)c.Y)).ToList()
            );
            sidewalkFloorPolygons.Add(
                plotSidewalkRounded.Coordinates.Select(c => new Vector2((float)c.X, (float)c.Y)).ToList()
            );
            centroidPolygon.Add(new Vector2((float)plotBuilding.Centroid.X, (float)plotBuilding.Centroid.Y));
        }

        return (shrunkPolygons, sidewalkPolygons, sidewalkFloorPolygons, centroidPolygon);
    }
}