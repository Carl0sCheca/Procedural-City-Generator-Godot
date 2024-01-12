using System;
using System.Globalization;
using Godot;
using ProceduralCityGenerator.Scripts.DataStructure;

namespace ProceduralCityGenerator.Scripts.GUI;

public partial class PropertiesPointsEdit : VBoxContainer
{
    private bool _angleEdit;
    private bool _decayEdit;

    private Field _field;
    [Export] private StyleBoxFlat _selectedStyle;
    private bool _sizeEdit;
    [Export] private StyleBoxFlat _unselectedStyle;
    [Export] public VBoxContainer angleEditContainer;
    [Export] public Button pointButton;
    [Export] public Button removeButton;
    [Export] public HSlider sliderAngle;
    [Export] public HSlider sliderDecay;
    [Export] public HSlider sliderSize;
    [Export] public VBoxContainer tensorPropertiesContainer;
    [Export] public LineEdit textEditAngle;
    [Export] public LineEdit textEditDecay;
    [Export] public LineEdit textEditSize;

    public override void _Ready()
    {
        removeButton.Pressed += () =>
        {
            MainScript.tensorField.Fields.Remove(_field);
            MainScript.Redraw();
            QueueFree();
        };

        pointButton.Pressed += () =>
        {
            tensorPropertiesContainer.Visible = !tensorPropertiesContainer.Visible;
            removeButton.Visible = !removeButton.Visible;
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

            if (_field != null)
            {
                _field.decay = (float)sliderDecay.Value;
                MainScript.Redraw();
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

            if (_field is Grid grid)
            {
                grid.theta = (float)Mathf.DegToRad(sliderAngle.Value);
                MainScript.Redraw();
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

            if (_field != null)
            {
                _field.size = (float)sliderSize.Value;
                MainScript.Redraw();
            }
        };
        sliderSize.DragEnded += _ => { _sizeEdit = false; };
        sliderSize.DragStarted += () => { _sizeEdit = true; };
    }

    public override void _Process(double delta)
    {
        if (_sizeEdit)
        {
            textEditSize.Text = sliderSize.Value.ToString(CultureInfo.InvariantCulture);

            if (_field != null)
            {
                _field.size = (float)sliderSize.Value;
                MainScript.Redraw();
            }
        }

        if (_angleEdit)
        {
            textEditAngle.Text = sliderAngle.Value.ToString(CultureInfo.InvariantCulture);

            if (_field is Grid grid)
            {
                grid.theta = (float)Mathf.DegToRad(sliderAngle.Value);
                MainScript.Redraw();
            }
        }

        if (_decayEdit)
        {
            textEditDecay.Text = sliderDecay.Value.ToString(CultureInfo.InvariantCulture);

            if (_field != null)
            {
                _field.decay = (float)sliderDecay.Value;
                MainScript.Redraw();
            }
        }

        if (_field != null && MainScript.selectedField != null && MainScript.selectedField.Equals(_field))
        {
            tensorPropertiesContainer.Visible = true;
            removeButton.Visible = true;
            pointButton.ButtonPressed = true;

            pointButton.Set("theme_override_styles/pressed", _selectedStyle);
        }
        else
        {
            pointButton.Set("theme_override_styles/pressed", _unselectedStyle);
        }
    }

    public void AddField(Field tensorField)
    {
        _field = tensorField;
        SetValuesInit();
    }

    private void SetValuesInit()
    {
        if (_field is Grid grid)
        {
            textEditAngle.Text = Mathf.RadToDeg(grid.theta).ToString(CultureInfo.InvariantCulture);
            sliderAngle.Value = Mathf.RadToDeg(grid.theta);
            pointButton.Text = "Grid point";
        }
        else
        {
            pointButton.Text = "Radial point";
            angleEditContainer.Visible = false;
        }

        textEditDecay.Text = _field.decay.ToString(CultureInfo.InvariantCulture);
        sliderDecay.Value = _field.decay;
        textEditSize.Text = _field.size.ToString(CultureInfo.InvariantCulture);
        sliderSize.Value = _field.size;
    }
}