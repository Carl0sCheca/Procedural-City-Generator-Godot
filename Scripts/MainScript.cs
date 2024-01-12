using System;
using System.Collections.Generic;
using System.Linq;
using Advanced.Algorithms.Geometry;
using Godot;
using ProceduralCityGenerator.Scripts.DataStructure;
using ProceduralCityGenerator.Scripts.GUI;
using Environment = Godot.Environment;
using Noise = SimplexNoise.Noise;

namespace ProceduralCityGenerator.Scripts;

public partial class MainScript : Node
{
    public enum DrawState
    {
        TensorFields,
        Roads,
        Intersections,
        Polygons,
        Roads3D
    }

    public static TensorField tensorField;

    private static bool _redraw;

    public static Field selectedField;
    private readonly Vector2 _origin = Vector2.Zero;

    private readonly Vector2 _worldDimension = Vector2.One * 40;
    [Export] private Environment _cameraEnvironment;
    [Export] private Environment _defaultEnvironment;

    [Export] private GuiController _guiController;

    private List<Intersection> _intersections;
    private LineDrawer3D _line;
    private PointDrawer3D _point;
    private List<List<Intersection>> _polygons;
    private List<Vector2> _polygonsCentroid;
    private List<List<Vector2>> _polygonsShrunk;
    private List<List<Vector2>> _polygonsSidewalk;
    private List<List<Vector2>> _polygonsSidewalkRounded;
    private PointDrawer3D _tensorPoints;

    public CameraController cameraController;

    public Roads mainRoads;
    public Roads majorRoads;
    public Roads minorRoads;

    public DrawState state;

    public override void _Ready()
    {
        tensorField = new TensorField(_worldDimension * 0.5f, _origin);
        var integrator = new Integrator(tensorField, 1f);

        mainRoads = new Roads(StreamLinesParams.Default, integrator, _worldDimension, _origin);
        majorRoads = new Roads(StreamLinesParams.Default.SetSep(5f).SetLookahead(500f), integrator,
            _worldDimension,
            _origin);
        minorRoads = new Roads(StreamLinesParams.Default.SetSep(2.5f).SetLookahead(500f), integrator,
            _worldDimension,
            _origin);

        minorRoads.SetExistingStreamlines(new List<Roads> { mainRoads, majorRoads });
        majorRoads.SetExistingStreamlines(new List<Roads> { mainRoads });
        state = DrawState.TensorFields;


        cameraController = GetTree().Root.GetNode("Root").GetNode<CameraController>("Camera3D");
        _line = GetTree().Root.GetNode("Root").GetNode<LineDrawer3D>("Lines");
        _point = GetTree().Root.GetNode("Root").GetNode<PointDrawer3D>("Points");
        _tensorPoints = GetTree().Root.GetNode("Root").GetNode<PointDrawer3D>("TensorFieldPoints");

        DrawReload();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey inputEventKey)
        {
            // Ver las intersecciones y las uniones (DEBUG)
            if (inputEventKey.IsPressed() && inputEventKey.Keycode == Key.I && minorRoads.Streamlines != null &&
                majorRoads.Streamlines != null && mainRoads.Streamlines != null)
                GenerateIntersections(true);

            if (inputEventKey.IsPressed() && inputEventKey.Keycode == Key.Escape)
            {
                if (_guiController.showMenu)
                {
                    _guiController.HideMenu();
                    cameraController.mouseCapture = true;
                }
                else
                {
                    _guiController.ShowMenu();
                    cameraController.mouseCapture = false;
                }
            }
        }


        if (@event is InputEventMouseButton eventMouseButton)
        {
            var position = cameraController.ProjectPosition(eventMouseButton.Position, 0);

            if (eventMouseButton.Pressed && eventMouseButton.ButtonIndex == MouseButton.Left)
            {
                foreach (var field in tensorField.Fields)
                {
                    var v1 = field.center;
                    var v2 = new Vector2(position.X, -position.Z);
                    const float width = 0.1f;

                    if (v2.X >= v1.X - width && v2.X <= v1.X + width && v2.Y >= v1.Y - width && v2.Y <= v1.Y + width)
                    {
                        selectedField = field;
                        break;
                    }

                    selectedField = null;
                }

                DrawReload();
            }
        }

        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            var position = cameraController.ProjectPosition(eventMouseMotion.Position, 0);

            if (Input.IsActionPressed("mouseLeft") && selectedField != null)
            {
                selectedField.center = new Vector2(position.X, -position.Z);
                DrawReload();
            }
        }
    }

    public void GeneratePolygons(bool redraw = true)
    {
        state = DrawState.Polygons;
        while (true)
        {
            var polygonsGenerated = PolygonGenerator.GeneratePolygons(_intersections);

            if (polygonsGenerated.Count == 0) break;

            _polygons.AddRange(polygonsGenerated);
        }

        (_polygonsShrunk, _polygonsSidewalk, _polygonsSidewalkRounded, _polygonsCentroid) =
            PolygonGenerator.GeneratePlots(_polygons);

        if (redraw) DrawReload();
    }

    public void GenerateIntersections(bool redraw = false)
    {
        state = DrawState.Intersections;

        var allStreamlines = new List<List<Vector2>>();

        if (minorRoads.Streamlines != null) allStreamlines.AddRange(minorRoads.Streamlines.allStreamlinesSimple);

        if (mainRoads.Streamlines != null) allStreamlines.AddRange(mainRoads.Streamlines.allStreamlinesSimple);

        if (majorRoads.Streamlines != null) allStreamlines.AddRange(majorRoads.Streamlines.allStreamlinesSimple);

        _intersections = new List<Intersection>();
        _polygons = new List<List<Intersection>>();

        // Intersección entre todas las streamlines
        for (var m = 0; m < allStreamlines.Count - 1; m++)
        for (var i = 0; i < allStreamlines[m].Count - 1; i++)
        for (var j = i + 1; j < allStreamlines[m].Count; j++)
        {
            for (var k = m + 1; k < allStreamlines.Count; k++)
            for (var l = 0; l < allStreamlines[k].Count - 1; l++)
            {
                var line1 = new Line(
                    new Point(
                        _worldDimension.X + allStreamlines[m][i].X,
                        _worldDimension.Y + allStreamlines[m][i].Y
                    ), new Point(
                        _worldDimension.X + allStreamlines[m][j].X,
                        _worldDimension.Y + allStreamlines[m][j].Y)
                );

                var line2 = new Line(
                    new Point(
                        _worldDimension.X + allStreamlines[k][l].X,
                        _worldDimension.Y + allStreamlines[k][l].Y
                    ), new Point(
                        _worldDimension.X + allStreamlines[k][l + 1].X,
                        _worldDimension.Y + allStreamlines[k][l + 1].Y)
                );

                var actualIntersections = LineIntersection.Find(line1, line2);
                if (actualIntersections != null)
                    _intersections.Add(new Intersection
                    {
                        point = new Vector2((float)actualIntersections.X,
                                    (float)actualIntersections.Y) -
                                _worldDimension,
                        neighbours = new HashSet<Intersection>(),
                        segments = new List<Segment>
                        {
                            new()
                            {
                                indexA = i,
                                indexB = j,
                                indexStreamline = m
                            },
                            new()
                            {
                                indexA = l,
                                indexB = l + 1,
                                indexStreamline = k
                            }
                        }
                    });
            }

            i++;
        }

        for (var i = 0; i < allStreamlines.Count; i++)
        for (var j = 0; j < allStreamlines[i].Count - 2; j++)
        {
            var line1 = new Line(
                new Point(
                    _worldDimension.X + allStreamlines[i][j].X,
                    _worldDimension.Y + allStreamlines[i][j].Y
                ), new Point(
                    _worldDimension.X + allStreamlines[i][j + 1].X,
                    _worldDimension.Y + allStreamlines[i][j + 1].Y)
            );

            var line2 = new Line(
                new Point(
                    _worldDimension.X + allStreamlines[i][j + 1].X,
                    _worldDimension.Y + allStreamlines[i][j + 1].Y
                ), new Point(
                    _worldDimension.X + allStreamlines[i][j + 2].X,
                    _worldDimension.Y + allStreamlines[i][j + 2].Y)
            );

            var actualIntersections = LineIntersection.Find(line1, line2);
            if (actualIntersections != null)
                _intersections.Add(new Intersection
                {
                    point = new Vector2((float)actualIntersections.X,
                                (float)actualIntersections.Y) -
                            _worldDimension,
                    neighbours = new HashSet<Intersection>(),
                    segments = new List<Segment>
                    {
                        new()
                        {
                            indexA = j,
                            indexB = j + 1,
                            indexStreamline = i
                        },
                        new()
                        {
                            indexA = j + 1,
                            indexB = j + 2,
                            indexStreamline = i
                        }
                    }
                });
        }

        // Eliminar líneas que acaban sin ninguna intersección
        var usedStreamlines = new HashSet<int>();
        foreach (var intersection in _intersections)
        foreach (var segment in intersection.segments)
        {
            var selectedStreamlineIndex = segment.indexStreamline;
            if (usedStreamlines.Contains(selectedStreamlineIndex)) continue;
            usedStreamlines.Add(selectedStreamlineIndex);

            // Buscar todos las intersecciones que se encuentren en esa streamline
            var selectedIntersectionsFromStreamline = new List<Intersection>();
            for (var i = 0; i < _intersections.Count; i++)
            for (var j = 0; j < _intersections[i].segments.Count; j++)
                if (_intersections[i].segments[j].indexStreamline == selectedStreamlineIndex)
                    selectedIntersectionsFromStreamline.Add(_intersections[i]);

            // Si hay más de 2 intersecciones en esa streamline, buscar cuál es el la más cercana al primer
            // punto de la streamline y al último [0] y [^1]
            if (selectedIntersectionsFromStreamline.Count >= 2)
            {
                var distanceA = float.MaxValue;
                var distanceB = float.MaxValue;

                var indexPointA = -1;
                var indexPointB = -1;

                for (var i = 0; i < selectedIntersectionsFromStreamline.Count; i++)
                {
                    var p = selectedIntersectionsFromStreamline[i].point;

                    if (allStreamlines[selectedStreamlineIndex][0].DistanceSquaredTo(p) <= distanceA)
                    {
                        indexPointA = i;
                        distanceA = allStreamlines[selectedStreamlineIndex][0].DistanceSquaredTo(p);
                    }

                    if (allStreamlines[selectedStreamlineIndex][^1].DistanceSquaredTo(p) <= distanceB)
                    {
                        indexPointB = i;
                        distanceB = allStreamlines[selectedStreamlineIndex][^1].DistanceSquaredTo(p);
                    }
                }

                if (indexPointA == indexPointB) continue;
                if (indexPointA != -1)
                    allStreamlines[selectedStreamlineIndex][0] = new Vector2(
                        selectedIntersectionsFromStreamline[indexPointA].point.X,
                        selectedIntersectionsFromStreamline[indexPointA].point.Y);

                if (indexPointB != -1)
                    allStreamlines[selectedStreamlineIndex][^1] = new Vector2(
                        selectedIntersectionsFromStreamline[indexPointB].point.X,
                        selectedIntersectionsFromStreamline[indexPointB].point.Y);
            }
        }

        // Añadir vecinos a cada intersección
        foreach (var intersection in _intersections)
        foreach (var segment in intersection.segments)
        {
            var selectedStreamlineIndex = segment.indexStreamline;
            var selectedIntersectionsFromStreamline = new List<Intersection>();
            for (var i = 0; i < _intersections.Count; i++)
            for (var j = 0; j < _intersections[i].segments.Count; j++)
                if (_intersections[i].segments[j].indexStreamline == selectedStreamlineIndex)
                    selectedIntersectionsFromStreamline.Add(_intersections[i]);
            var streamlinesSegments = new Dictionary<(int, int), List<Intersection>>();
            for (var i = 0; i < selectedIntersectionsFromStreamline.Count; i++)
                foreach (var s in selectedIntersectionsFromStreamline[i].segments
                             .Where(s => s.indexStreamline == selectedStreamlineIndex))
                    if (streamlinesSegments.ContainsKey((s.indexA, s.indexB)))
                        streamlinesSegments[(s.indexA, s.indexB)]
                            .Add(selectedIntersectionsFromStreamline[i]);
                    else
                        streamlinesSegments.Add((s.indexA, s.indexB),
                            new List<Intersection> { selectedIntersectionsFromStreamline[i] });

            foreach (var kv in streamlinesSegments)
            {
                var firstPoint = allStreamlines[selectedStreamlineIndex][kv.Key.Item1];
                streamlinesSegments[kv.Key] =
                    kv.Value.OrderBy(item => item.point.DistanceSquaredTo(firstPoint)).ToList();
            }

            var sortedDict = streamlinesSegments.Keys.OrderBy(item => item.Item1).ToList();

            var finish = false;
            for (var i = 0; i < sortedDict.Count && !finish; i++)
            for (var j = 0; j < streamlinesSegments[sortedDict[i]].Count && !finish; j++)
                if (streamlinesSegments[sortedDict[i]][j].point == intersection.point)
                {
                    if (j - 1 >= 0)
                        intersection.neighbours.Add(streamlinesSegments[sortedDict[i]][j - 1]);
                    else if (i - 1 >= 0) intersection.neighbours.Add(streamlinesSegments[sortedDict[i - 1]][^1]);

                    if (j + 1 < streamlinesSegments[sortedDict[i]].Count)
                        intersection.neighbours.Add(streamlinesSegments[sortedDict[i]][j + 1]);
                    else if (i + 1 < sortedDict.Count)
                        intersection.neighbours.Add(streamlinesSegments[sortedDict[i + 1]][0]);
                    finish = true;
                }
        }

        foreach (var intersection in _intersections)
        foreach (var neighbour in intersection.neighbours)
            neighbour.neighbours.Add(intersection);

        if (redraw)
            DrawReload();
    }

    public void GenerateRoads(Roads roads, bool redraw = true)
    {
        roads.GenerateRoads();
        state = DrawState.Roads;
        if (redraw)
            DrawReload();
    }

    public void DrawReload()
    {
        _line.Clear();
        _point.Clear();
        _tensorPoints.Clear();

        switch (state)
        {
            case DrawState.TensorFields:
                foreach (var p in tensorField.points)
                {
                    var tensor = tensorField.GetPoint(p);
                    _line.AddLines(tensorField.GetTensorLine(p, tensor.Major));
                    _line.AddLines(tensorField.GetTensorLine(p, tensor.Minor));
                }

                foreach (var field in tensorField.Fields)
                    if (selectedField != null && selectedField.Equals(field))
                        _tensorPoints.AddPoint(new Vector3(field.center.X, field.center.Y, 0), Colors.GreenYellow);
                    else
                        _tensorPoints.AddPoint(new Vector3(field.center.X, field.center.Y, 0), Colors.Yellow);

                break;
            case DrawState.Roads:
                // Orden de dibujado: MINOR -> MAJOR -> MAIN
                // Orden de creación: MAIN -> MAJOR -> MINOR

                minorRoads.DrawLineRoads(_line, Colors.Red);
                majorRoads.DrawLineRoads(_line, Colors.Green);
                mainRoads.DrawLineRoads(_line, Colors.Blue);

                break;
            case DrawState.Intersections:
                if (_intersections != null)
                    foreach (var p in _intersections)
                    {
                        _point.AddPoint(new Vector3(p.point.X, p.point.Y, 0), Colors.Red);

                        foreach (var neighbour in p.neighbours)
                        {
                            var vector1 = new Vector3(p.point.X, p.point.Y, 0);
                            var vector2 = new Vector3(neighbour.point.X, neighbour.point.Y, 0);
                            _line.AddLine(vector1, vector2, Colors.Black);
                        }
                    }

                break;
            case DrawState.Polygons:
                if (_intersections != null)
                    foreach (var p in _intersections)
                        _point.AddPoint(new Vector3(p.point.X, p.point.Y, 0));

                if (_polygonsShrunk != null)
                {
                    minorRoads.DrawLineRoads(_line, Colors.Red);
                    majorRoads.DrawLineRoads(_line, Colors.Green);
                    mainRoads.DrawLineRoads(_line, Colors.Blue);

                    foreach (var plist in _polygonsShrunk)
                        for (var i = 0; i < plist.Count - 1; i++)
                        {
                            var vector1 = new Vector3(plist[i].X, plist[i].Y, 0);
                            var vector2 = new Vector3(plist[i + 1].X, plist[i + 1].Y, 0);

                            _line.AddLine(vector1, vector2, Colors.Chocolate);
                        }
                }

                break;
            case DrawState.Roads3D:
                var floorNode = GetNode<CsgMesh3D>("/root/Root/Floor");
                if (!floorNode.IsVisibleInTree()) floorNode.Visible = true;

                break;
        }
    }

    public override void _Process(double delta)
    {
        if (!_redraw) return;

        DrawReload();
        _redraw = false;
    }

    public static void Redraw()
    {
        _redraw = true;
    }

    public void GenerateRoads3D()
    {
        var roadsNode = GetNode("/root/Root/RoadModel/Roads");
        foreach (var child in roadsNode.GetChildren()) child.Free();

        if (minorRoads.Streamlines != null)
            foreach (var plist in minorRoads.Streamlines.allStreamlinesSimple)
            {
                var roadPolygon = new MainRoads3D();
                roadsNode.AddChild(roadPolygon);
                roadPolygon.AddPath(plist
                    .Select(p =>
                        new Vector3(p.X - _worldDimension.X, 0, p.Y - _worldDimension.Y).Rotated(Vector3.Right,
                            Mathf.Pi))
                    .ToList());
            }

        if (majorRoads.Streamlines != null)
            foreach (var plist in majorRoads.Streamlines.allStreamlinesSimple)
            {
                var roadPolygon = new MainRoads3D();
                roadsNode.AddChild(roadPolygon);
                roadPolygon.AddPath(plist
                    .Select(p =>
                        new Vector3(p.X - _worldDimension.X, 0, p.Y - _worldDimension.Y).Rotated(Vector3.Right,
                            Mathf.Pi))
                    .ToList());
            }

        if (mainRoads.Streamlines != null)
            foreach (var plist in mainRoads.Streamlines.allStreamlinesSimple)
            {
                var roadPolygon = new MainRoads3D();
                roadsNode.AddChild(roadPolygon);
                roadPolygon.AddPath(plist
                    .Select(p =>
                        new Vector3(p.X - _worldDimension.X, 0, p.Y - _worldDimension.Y).Rotated(Vector3.Right,
                            Mathf.Pi))
                    .ToList());
            }

        ChangeCameraToPerspective();
        DrawReload();
    }

    private void ChangeCameraToPerspective()
    {
        state = DrawState.Roads3D;
        cameraController.Projection = Camera3D.ProjectionType.Perspective;
        cameraController.Environment = _cameraEnvironment;
    }

    public void GenerateBuildings3D()
    {
        var buildingsNode = GetNode("/root/Root/BuildingsModel/Buildings");
        var roadsNode = GetNode("/root/Root/BuildingsModel/RoadsBuildings");
        foreach (var child in buildingsNode.GetChildren()) child.Free();
        foreach (var child in roadsNode.GetChildren()) child.Free();

        Noise.Seed = GD.RandRange(0, 100000);
        var noiseValues = Noise.Calc2D((int)_worldDimension.X, (int)_worldDimension.Y, 0.01f);

        for (var i = 0; i < _polygonsShrunk.Count; i++)
        {
            var buildingsPolygon = new MainBuildings3D(_polygonsShrunk[i],
                noiseValues[(int)_polygonsCentroid[i].X, (int)_polygonsCentroid[i].Y] / 255 *
                (float)GD.RandRange(1f, 10f));

            var index = GD.RandRange(1, 11);
            buildingsPolygon.Material =
                (Material)ResourceLoader
                    .Load($"res://Materials/BuildingMaterial{index}.tres");

            buildingsNode.AddChild(buildingsPolygon);
            buildingsNode.AddChild(MainBuildings3D.SetTop(buildingsPolygon));
        }

        foreach (var plist in _polygonsSidewalkRounded)
        {
            var sidewalkPolygon = new MainSidewalk3D(plist);

            buildingsNode.AddChild(sidewalkPolygon);
        }

        foreach (var plist in _polygonsSidewalk)
        {
            var sidewalkPolygon = MainSidewalk3D.Bottom(plist);

            roadsNode.AddChild(sidewalkPolygon);
        }

        ChangeCameraToPerspective();
        DrawReload();
    }

    public void GenerateTrees()
    {
        var raycasts = new List<Vector3>();
        foreach (var child in GetTree().Root.GetNode("Root/Trees").GetChildren()) child.Free();

        for (var i = 0; i < _worldDimension.X; i++)
        for (var j = 0; j < _worldDimension.Y; j++)
        {
            var raycast = new RayCast3D(new Vector3(i, 12, -j));
            GetTree().Root.AddChild(raycast);
            raycast.ForceRaycastUpdate();
            if (((Node)raycast.GetCollider()).Name.Equals("Floor")) raycasts.Add(raycast.GetCollisionPoint());
            raycast.Free();
        }

        var random = new Random();
        foreach (var raycast in raycasts)
            if (random.Next(0, 101) - 1 <= 30)
            {
                var tree = new Sprite3D();
                tree.Texture = (Texture2D)ResourceLoader.Load("res://Assets/Image_49.png");
                tree.Position = new Vector3(raycast.X, 0.635f, raycast.Z);
                tree.Scale = Vector3.One * 0.25f;
                tree.Shaded = true;
                tree.RotateY((float)GD.RandRange(0, Mathf.Tau));
                var tree2 = tree.Duplicate();
                ((Node3D)tree2).RotateY(Mathf.Pi * 0.5f);
                GetTree().Root.GetNode("Root/Trees").AddChild(tree);
                GetTree().Root.GetNode("Root/Trees").AddChild(tree2);
            }
    }

    public void SetTensorFieldState()
    {
        state = DrawState.TensorFields;
        cameraController.Projection = Camera3D.ProjectionType.Orthogonal;

        cameraController.Position = new Vector3(cameraController.Position.X, 10, cameraController.Position.Z);
        cameraController.RotationDegrees = new Vector3(-90, 0, 0);

        cameraController.Environment = _defaultEnvironment;

        var buildingsNode = GetNode("/root/Root/BuildingsModel/Buildings");
        foreach (var child in buildingsNode.GetChildren()) child.Free();

        var sidewalks = GetNode("/root/Root/BuildingsModel/RoadsBuildings");
        foreach (var child in sidewalks.GetChildren()) child.Free();

        var roadsNode = GetNode("/root/Root/RoadModel/Roads");
        foreach (var child in roadsNode.GetChildren()) child.Free();

        var treesNode = GetNode("/root/Root/Trees");
        foreach (var child in treesNode.GetChildren()) child.Free();

        DrawReload();
        var floorNode = GetNode<CsgMesh3D>("/root/Root/Floor");
        floorNode.Visible = false;
    }
}