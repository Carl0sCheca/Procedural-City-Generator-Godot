using System;
using System.Collections.Generic;
using Godot;
using Simplify.NET;

namespace ProceduralCityGenerator.Scripts.DataStructure;

public class StreamLines
{
    private readonly List<List<Vector2>> _allStreamlines = new();

    private readonly Integrator _integrator;

    private readonly GridStorage _major;
    private readonly GridStorage _minor;
    private readonly Vector2 _origin;
    private readonly StreamLinesParams _parameters;
    private readonly List<List<Vector2>> _streamlinesMajor = new();
    private readonly List<List<Vector2>> _streamlinesMinor = new();
    private readonly Vector2 _worldDimensions;
    private StreamLinesParams _parametersSq;
    public List<List<Vector2>> allStreamlinesSimple = new();

    public StreamLines(Integrator integrator, Vector2 worldDimensions, Vector2 origin, StreamLinesParams parameters)
    {
        _integrator = integrator;
        _worldDimensions = worldDimensions;
        _origin = origin;
        _parameters = parameters;

        if (_parameters.step > _parameters.sep)
            GD.PrintErr("Distancia de la muestra de linea de flujo más grande que dsep");

        _parameters.test = Mathf.Min(_parameters.test, _parameters.sep);

        _major = new GridStorage(_worldDimensions, _origin, _parameters.sep);
        _minor = new GridStorage(_worldDimensions, _origin, _parameters.sep);

        SetParametersSq();
    }

    private void SetParametersSq()
    {
        _parametersSq = new StreamLinesParams
        {
            sep = _parameters.sep * _parameters.sep,
            test = _parameters.test * _parameters.test,
            step = _parameters.step * _parameters.step,
            lookahead = _parameters.lookahead * _parameters.lookahead,
            circleJoin = _parameters.circleJoin * _parameters.circleJoin,
            joinAngle = _parameters.joinAngle * _parameters.joinAngle,
            pathIterations = _parameters.pathIterations * _parameters.pathIterations,
            seedTries = _parameters.seedTries * _parameters.seedTries,
            simplifyTolerance = _parameters.simplifyTolerance * _parameters.simplifyTolerance,
            colliderEarly = _parameters.colliderEarly * _parameters.colliderEarly
        };
    }

    public void AddExistingStreamlines(StreamLines streamLines)
    {
        _major.AddAll(streamLines._major);
        _minor.AddAll(streamLines._minor);
    }

    public void CreateAllStreamLines()
    {
        var major = true;
        while (CreateStreamLine(major)) major = !major;

        JoinDanglingStreamlines();
    }

    private Vector2? GetBestNextPoint(Vector2 point, Vector2 previousPoint)
    {
        var nearbyPoints = _major.GetNearbyPoints(point, _parameters.lookahead);
        nearbyPoints.AddRange(_minor.GetNearbyPoints(point, _parameters.lookahead));
        var direction = point - previousPoint;

        Vector2? closestSample = null;
        var closestDistance = float.PositiveInfinity;

        foreach (var sample in nearbyPoints)
            if (!sample.Equals(point) && !sample.Equals(previousPoint))
            {
                var differenceVector = sample - point;
                var dotDifferenceVector = differenceVector.Dot(direction);
                if (dotDifferenceVector < 0)
                    continue;

                var distanceToSample = point.DistanceSquaredTo(sample);
                if (distanceToSample < closestDistance && distanceToSample < 2 * 0.01f * 0.01f)
                {
                    closestDistance = distanceToSample;
                    closestSample = sample;
                    continue;
                }

                var angleBetween = MathF.Abs(direction.AngleTo(differenceVector));

                if (angleBetween < _parameters.joinAngle && distanceToSample < closestDistance)
                {
                    closestDistance = distanceToSample;
                    closestSample = sample;
                }
            }

        if (closestSample.HasValue) closestSample += direction.Normalized() * _parameters.simplifyTolerance * 4;

        return closestSample;
    }

    private void JoinDanglingStreamlines()
    {
        var dstep = 0.01f;
        foreach (var major in new[] { true, false })
        foreach (var streamline in Streamlines(major))
        {
            if (streamline[0].Equals(streamline[^1])) continue;

            var newStart = GetBestNextPoint(streamline[0], streamline[4]);

            if (newStart.HasValue)
                foreach (var p in PointsBetween(streamline[0], newStart.Value, dstep))
                {
                    streamline.Insert(0, p);
                    Grid(major).AddSample(p);
                }

            var newEnd = GetBestNextPoint(streamline[^1], streamline[^4]);

            if (newEnd.HasValue)
                foreach (var p in PointsBetween(streamline[^1], newEnd.Value, dstep))
                {
                    streamline.Add(p);
                    Grid(major).AddSample(p);
                }
        }

        allStreamlinesSimple = new List<List<Vector2>>();
        foreach (var s in _allStreamlines) allStreamlinesSimple.Add(SimplifyStreamline(s));
    }

    private List<Vector2> PointsBetween(Vector2 v1, Vector2 v2, float step)
    {
        var d = v1.DistanceTo(v2);
        var nPoints = Mathf.Floor(d / step);

        if (nPoints == 0) return new List<Vector2>();

        var stepVector = v2 - v1;

        var outVectors = new List<Vector2>();

        var i = 1;
        var next = v1 + stepVector * (i / nPoints);

        for (i = 1; i <= nPoints; i++)
        {
            if (_integrator.Integrate(next, true).LengthSquared() > 0.001f)
                outVectors.Add(next);
            else
                return outVectors;

            next = v1 + stepVector * (i / nPoints);
        }

        return outVectors;
    }

    private List<List<Vector2>> Streamlines(bool major)
    {
        return major ? _streamlinesMajor : _streamlinesMinor;
    }

    private GridStorage Grid(bool major)
    {
        return major ? _major : _minor;
    }

    private Vector2 SamplePoint()
    {
        return new Vector2(GD.Randf() * _worldDimensions.X, GD.Randf() * _worldDimensions.Y) + _origin;
    }

    private Vector2? GetSeed(bool major)
    {
        var seed = SamplePoint();
        var i = 0;

        while (!IsValidSample(major, seed, _parametersSq.sep))
        {
            if (i >= _parameters.seedTries) return null;

            seed = SamplePoint();
            i++;
        }

        return seed;
    }

    private bool IsValidSample(bool major, Vector2 point, float dSq, bool bothGrids = false)
    {
        var gridValid = Grid(major).IsValidSample(point, dSq);

        if (bothGrids) gridValid = gridValid && Grid(!major).IsValidSample(point, dSq);

        return gridValid;
    }

    private bool CreateStreamLine(bool major)
    {
        var seed = GetSeed(major);
        var isValid = false;
        if (seed.HasValue)
        {
            var streamline = _IntegrateStreamline(seed.Value, major);
            if (ValidStreamline(streamline))
            {
                Grid(major).AddPolyline(streamline);
                Streamlines(major).Add(streamline);
                _allStreamlines.Add(streamline);
                allStreamlinesSimple.Add(SimplifyStreamline(streamline));
            }

            isValid = true;
        }

        return isValid;
    }

    private List<Vector2> SimplifyStreamline(List<Vector2> streamline)
    {
        var simplified = new List<Vector2>();

        foreach (var point in SimplifyNet.Simplify(Vector2ToPoint(streamline), _parameters.simplifyTolerance))
            simplified.Add(new Vector2((float)point.X, (float)point.Y));

        return simplified;
    }

    private static List<Point> Vector2ToPoint(List<Vector2> vector)
    {
        var points = new List<Point>();
        foreach (var point in vector) points.Add(new Point(point.X, point.Y));

        return points;
    }

    private bool ValidStreamline(List<Vector2> streamline)
    {
        return streamline.Count > 5;
    }

    private List<Vector2> _IntegrateStreamline(Vector2 seed, bool major)
    {
        var count = 0;
        var pointsEscaped = false;

        var collideBoth = GD.Randf() < _parameters.colliderEarly;

        var d = _integrator.Integrate(seed, major);

        var forwardParameters = new StreamLineIntegration
        {
            seed = seed,
            originalDir = d,
            streamline = new List<Vector2> { seed },
            previousDirection = d,
            previousPoint = seed + d,
            valid = true
        };

        forwardParameters.valid = PointInBounds(forwardParameters.previousPoint);

        var negD = -d;
        var backwardParameters = new StreamLineIntegration
        {
            seed = seed,
            originalDir = negD,
            streamline = new List<Vector2>(),
            previousDirection = negD,
            previousPoint = seed + negD,
            valid = true
        };

        backwardParameters.valid = PointInBounds(backwardParameters.previousPoint);

        var finished = false;
        while (!finished && count < _parameters.pathIterations && (forwardParameters.valid || backwardParameters.valid))
        {
            StreamLineIntegrationStep(ref forwardParameters, major, collideBoth);
            StreamLineIntegrationStep(ref backwardParameters, major, collideBoth);

            var sqDistanceBetweenPoints =
                forwardParameters.previousPoint
                    .DistanceSquaredTo(backwardParameters.previousPoint);

            if (!pointsEscaped && sqDistanceBetweenPoints > _parametersSq.circleJoin) pointsEscaped = true;

            if (pointsEscaped && sqDistanceBetweenPoints <= _parametersSq.circleJoin)
            {
                forwardParameters.streamline.Add(forwardParameters.previousPoint);
                forwardParameters.streamline.Add(backwardParameters.previousPoint);
                backwardParameters.streamline.Add(backwardParameters.previousPoint);
                finished = true;
            }

            count++;
        }

        backwardParameters.streamline.Reverse();
        backwardParameters.streamline.AddRange(forwardParameters.streamline);
        return backwardParameters.streamline;
    }

    private void StreamLineIntegrationStep(ref StreamLineIntegration parameters, bool major, bool collideBoth)
    {
        if (!parameters.valid) return;

        parameters.streamline.Add(parameters.previousPoint);
        var nextDirection = _integrator.Integrate(parameters.previousPoint, major);

        if (nextDirection.LengthSquared() < 0.01f)
        {
            parameters.valid = false;
            return;
        }

        if (nextDirection.Dot(parameters.previousDirection) < 0) nextDirection *= -1;

        var nextPoint = parameters.previousPoint + nextDirection;

        if (PointInBounds(nextPoint)
            && IsValidSample(major, nextPoint, _parametersSq.test, collideBoth)
            && !StreamlineTurned(parameters.seed, parameters.originalDir, nextPoint, nextDirection))
        {
            parameters.previousPoint = nextPoint;
            parameters.previousDirection = nextDirection;
        }
        else
        {
            parameters.streamline.Add(nextPoint);
            parameters.valid = false;
        }
    }

    private static bool StreamlineTurned(Vector2 seed, Vector2 originalDir, Vector2 point, Vector2 direction)
    {
        if (!(originalDir.Dot(direction) < 0))
            return false;

        var perpendicularVector = new Vector2(originalDir.Y, -originalDir.X);
        var isLeft = (point - seed).Dot(perpendicularVector) < 0;
        var directionUp = direction.Dot(perpendicularVector) > 0;

        return isLeft == directionUp;
    }

    private bool PointInBounds(Vector2 v)
    {
        return v.X >= _origin.X
               && v.Y >= _origin.Y
               && v.X < _worldDimensions.X + _origin.X
               && v.Y < _worldDimensions.Y + _origin.Y;
    }
}