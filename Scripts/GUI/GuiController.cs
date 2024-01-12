using System;
using System.Globalization;
using Godot;

namespace ProceduralCityGenerator.Scripts.GUI;

public partial class GuiController : Control
{
    private bool _angleEdit;
    private bool _decayEdit;

    [Export] private MainScript _mainScript;
    private bool _sizeEdit;
    [Export] public Button addAllRoads;
    [Export] public Button addFieldButton;

    [Export] public Button addMainRoads;
    [Export] public Button addMajorRoads;
    [Export] public Button addMinorRoads;
    [Export] public VBoxContainer angleEditContainer;
    [Export] public Button generateBuildings;
    [Export] public Button generateCity;
    [Export] public Button hideMenuButton;

    [Export] public VBoxContainer listTensorPointsContainer;

    [Export] public PanelContainer mainMenuPanel;
    [Export] public Button regenerateBuildings;
    [Export] public Button regenerateTrees;
    [Export] public Button saveModelButton;
    [Export] public Button selectGridButton;

    [Export] public Button selectRadialButton;
    public bool showMenu;

    [Export] public Button showMenuButton;
    [Export] public HSlider sliderAngle;
    [Export] public HSlider sliderDecay;
    [Export] public HSlider sliderSize;

    [Export] public TabBar tabBar;
    [Export] public VBoxContainer tabContainer1;
    [Export] public VBoxContainer tabContainer2;
    [Export] public PackedScene tensorPointPrefab;
    [Export] public VBoxContainer tensorPropertiesContainer;
    [Export] public LineEdit textEditAngle;
    [Export] public LineEdit textEditDecay;
    [Export] public LineEdit textEditSize;

    public override void _Ready()
    {
        regenerateTrees.Pressed += () => _mainScript.GenerateTrees();

        regenerateBuildings.Pressed += async () =>
        {
            _mainScript.GenerateBuildings3D();

            await ToSignal(GetTree(), "process_frame");
            _mainScript.GenerateTrees();
        };

        generateCity.Pressed += async () =>
        {
            _mainScript.GenerateBuildings3D();
            _mainScript.GenerateRoads3D();

            await ToSignal(GetTree(), "process_frame");
            _mainScript.GenerateTrees();
        };

        generateBuildings.Pressed += () =>
        {
            _mainScript.GenerateIntersections();
            _mainScript.GeneratePolygons();
        };

        textEditDecay.TextChanged += text =>
        {
            try
            {
                sliderDecay.Value = float.Parse(text);
            }
            catch (FormatException)
            {
                // ignored
            }
        };
        sliderDecay.DragEnded += _ => { _decayEdit = false; };
        sliderDecay.DragStarted += () => { _decayEdit = true; };

        textEditAngle.TextChanged += text =>
        {
            try
            {
                sliderAngle.Value = float.Parse(text);
            }
            catch (FormatException)
            {
                // ignored
            }
        };
        sliderAngle.DragEnded += _ => { _angleEdit = false; };
        sliderAngle.DragStarted += () => { _angleEdit = true; };

        textEditSize.TextChanged += text =>
        {
            try
            {
                sliderSize.Value = float.Parse(text);
            }
            catch (FormatException)
            {
                // ignored
            }
        };
        sliderSize.DragEnded += _ => { _sizeEdit = false; };
        sliderSize.DragStarted += () => { _sizeEdit = true; };

        tabBar.TabChanged += index =>
        {
            switch (index)
            {
                case 0:
                    tabContainer1.Visible = true;
                    tabContainer2.Visible = false;
                    _mainScript.state = MainScript.DrawState.Roads;
                    break;
                case 1:
                    tabContainer1.Visible = false;
                    tabContainer2.Visible = true;
                    _mainScript.SetTensorFieldState();
                    break;
            }

            MainScript.Redraw();
        };

        addAllRoads.Pressed += () =>
        {
            _mainScript.GenerateRoads(_mainScript.mainRoads);
            _mainScript.GenerateRoads(_mainScript.majorRoads);
            _mainScript.GenerateRoads(_mainScript.minorRoads);
        };

        addMainRoads.Pressed += () =>
        {
            if (_mainScript.majorRoads.Streamlines != null) _mainScript.majorRoads.Streamlines = null;

            if (_mainScript.minorRoads.Streamlines != null) _mainScript.minorRoads.Streamlines = null;

            _mainScript.GenerateRoads(_mainScript.mainRoads);
        };

        addMajorRoads.Pressed += () =>
        {
            if (_mainScript.minorRoads.Streamlines != null) _mainScript.minorRoads.Streamlines = null;

            _mainScript.GenerateRoads(_mainScript.majorRoads);
        };

        addMinorRoads.Pressed += () => { _mainScript.GenerateRoads(_mainScript.minorRoads); };

        addFieldButton.Pressed += () =>
        {
            var position = _mainScript.cameraController.centerScreen;

            if (selectGridButton.ButtonPressed)
                MainScript.tensorField.AddGrid(new Vector2(position.X, -position.Y), (float)sliderSize.Value,
                    (float)sliderDecay.Value, Mathf.DegToRad((float)sliderAngle.Value));
            else if (selectRadialButton.ButtonPressed)
                MainScript.tensorField.AddRadial(new Vector2(position.X, -position.Y), (float)sliderSize.Value,
                    (float)sliderDecay.Value);

            var tensor = tensorPointPrefab.Instantiate<PropertiesPointsEdit>();
            tensor.AddField(MainScript.tensorField.Fields[^1]);
            listTensorPointsContainer.AddChild(tensor);

            _mainScript.DrawReload();
        };

        selectGridButton.Pressed += () =>
        {
            selectRadialButton.ButtonPressed = false;
            angleEditContainer.Visible = true;

            tensorPropertiesContainer.Visible = selectGridButton.ButtonPressed;
        };

        selectRadialButton.Pressed += () =>
        {
            selectGridButton.ButtonPressed = false;
            angleEditContainer.Visible = false;

            tensorPropertiesContainer.Visible = selectRadialButton.ButtonPressed;
        };

        hideMenuButton.Pressed += HideMenu;

        showMenuButton.Pressed += ShowMenu;

        saveModelButton.Pressed += () =>
        {
            var gl = new GltfDocument();
            var state = new GltfState();
            var fileDialog = new FileDialog();
            fileDialog.Access = FileDialog.AccessEnum.Filesystem;
            fileDialog.InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen;
            fileDialog.Size = new Vector2I(930, 440);
            fileDialog.AddFilter("*.gltf");
            GetNode("/root/Root/GUI").AddChild(fileDialog);
            fileDialog.Visible = true;

            fileDialog.FileSelected += path =>
            {
                gl.AppendFromScene(GetTree().CurrentScene, state);

                var err = gl.WriteToFilesystem(state, path);

                if (err == Error.Failed) GD.PrintErr("Ha ocurrido un error");
            };
        };
    }

    public async void ShowMenu()
    {
        if (showMenu) return;

        showMenu = true;

        showMenuButton.Visible = false;

        var tween = GetTree().CreateTween();
        tween.SetTrans(Tween.TransitionType.Expo);
        tween.SetEase(Tween.EaseType.Out);

        mainMenuPanel.Visible = true;
        tween.TweenProperty(mainMenuPanel, "position", -mainMenuPanel.Position, 0.5f).AsRelative();
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public async void HideMenu()
    {
        if (!showMenu) return;

        var tween = GetTree().CreateTween();
        tween.SetTrans(Tween.TransitionType.Expo);
        tween.SetEase(Tween.EaseType.Out);

        tween.TweenProperty(mainMenuPanel, "position", new Vector2(-mainMenuPanel.Size.X, mainMenuPanel.Position.Y),
            0.5f).AsRelative();
        showMenuButton.Visible = true;
        await ToSignal(tween, Tween.SignalName.Finished);
        mainMenuPanel.Visible = false;
        showMenu = false;
    }

    public override void _Process(double delta)
    {
        if (_sizeEdit) textEditSize.Text = sliderSize.Value.ToString(CultureInfo.InvariantCulture);

        if (_angleEdit) textEditAngle.Text = sliderAngle.Value.ToString(CultureInfo.InvariantCulture);

        if (_decayEdit) textEditDecay.Text = sliderDecay.Value.ToString(CultureInfo.InvariantCulture);
    }
}