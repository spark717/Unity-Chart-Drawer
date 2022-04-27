using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class ChartDrawer : VisualElement
{
    public const string PositiveClass = "chart-positive";
    public const string NegativeClass = "chart-negative";
    public const string ValueLabelClass = "value-label";
    
    private readonly Vector2[] _points;

    private readonly IResolvedStyle _positiveStyle;
    private readonly IResolvedStyle _negativeStyle;

    private int _pointsCount;
    private float _horizontalScale = 1f;
    private bool _isPillarMode = false;
    private bool _showValueLabels = false;

    private float _yRatio;
    
    public bool IsPillarMode => _isPillarMode;
    public float HorizontalScale => _horizontalScale;
    public bool ShowValueLabels => _showValueLabels;

    public string LabelFormat { get; set; } = "x: {0:F1}\ny: {1:F1}";
    
    public ChartDrawer(int maxPointsCount)
    {
        _points = new Vector2[maxPointsCount];
        
        generateVisualContent = OnGenerateVisualContent;

        CreateStyle(out _positiveStyle, PositiveClass);
        CreateStyle(out _negativeStyle, NegativeClass);
        
        style.height = new StyleLength(Length.Percent(100f));
        style.width = new StyleLength(0f);
    }

    private void CreateStyle(out IResolvedStyle newStyle, string className)
    {
        var styleElement = new VisualElement();
        styleElement.AddToClassList(className);
        styleElement.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

        Add(styleElement);
        
        newStyle = styleElement;
    }

    public void SetHorizontalScale(float value)
    {
        _horizontalScale = value;

        if (_pointsCount >= 2)
        {
            AdjustWidth();
            CreateLabels();
            MarkDirtyRepaint();
        }
    }

    public void SetPillarMode(bool value)
    {
        _isPillarMode = value;
        
        MarkDirtyRepaint();
    }
    
    public void SwitchMode()
    {
        SetPillarMode(!_isPillarMode);
    }

    public void SetShowValueLebels(bool value)
    {
        _showValueLabels = value;
        
        CreateLabels();
    }
    
    public void AddPoint(Vector2 point)
    {
        AddPoint(point.x, point.y);
    }
    
    public void AddPoint(float x, float y)
    {
        if (_pointsCount > 0)
        {
            float lastX = _points[_pointsCount - 1].x;

            if (x < lastX)
            {
                throw new Exception("New x must be greater than the previous");
            }
        }

        // if array full shift points left on 1 cell
        if (_pointsCount == _points.Length)
        {
            for (int i = 0; i < _pointsCount - 1; i++)
            {
                _points[i] = _points[i + 1];
            }
        }

        // increment total points count
        _pointsCount++;
        _pointsCount = Mathf.Clamp(_pointsCount, 0, _points.Length);
        
        // write new point as last array element
        _points[_pointsCount - 1] = new Vector2(x, y);
        
        if (_pointsCount >= 2)
        {
            CalculateRatioY();
            AdjustWidth();
            CreateLabels();
            MarkDirtyRepaint();
        }
    }

    public void ClearPoints()
    {
        _pointsCount = 0;

        for (int i = 0; i < _points.Length; i++)
        {
            _points[i] = default;
        }
        
        AdjustWidth();
        CreateLabels();
        MarkDirtyRepaint();
    }
    
    private void OnGenerateVisualContent(MeshGenerationContext context)
    {
        if (_isPillarMode)
        {
            CreatePillarChartMesh(context);
        }
        else
        {
            CreateNormalChartMesh(context);
        }
    }

    private void CreateNormalChartMesh(MeshGenerationContext context)
    {
        if (_pointsCount < 2)
        {
            return;
        }
        
        int trianglesCount = (_pointsCount - 1) * 2;
        int verticesCount = trianglesCount * 3;
        int indicesCount = trianglesCount * 3;
        
        var mesh = context.Allocate(verticesCount, indicesCount);

        Color positiveColor = GetPositiveColor();
        Color negativeColor = GetNegativeColor();

        int indexCounter = 0;
        
        for (int i = 0; i < _pointsCount - 1; i++)
        {
            float x1 = _points[i].x - _points[0].x;
            float y1 = _points[i].y * _yRatio;
            float x2 = _points[i + 1].x - _points[0].x;
            float y2 = _points[i + 1].y * _yRatio;
            float x3 = x1;
            float y3 = 0f;
            float x4 = x2;
            float y4 = 0f;
            
            if (y1 * y2 >= 0) // points with same Y sign
            {
                Color color = y1 > 0 ? positiveColor : negativeColor;

                AddTriangle(mesh, color, ref indexCounter, x1, y1, x2, y2, x3, y3);
                AddTriangle(mesh, color, ref indexCounter, x2, y2, x3, y3, x4, y4);
            }
            else // points with different Y sign
            {
                float x5 = x1 - y1 * (x2 - x1) / (y2 - y1);
                float y5 = 0f;
                
                Color color1 = y1 > 0 ? positiveColor : negativeColor;
                Color color2 = y2 > 0 ? positiveColor : negativeColor;

                AddTriangle(mesh, color1, ref indexCounter, x1, y1, x3, y3, x5, y5);
                AddTriangle(mesh, color2, ref indexCounter, x2, y2, x4, y4, x5, y5);
            }
        }
    }

    private void CreatePillarChartMesh(MeshGenerationContext context)
    {
        if (_pointsCount < 2)
        {
            return;
        }
        
        int trianglesCount = _pointsCount * 2;
        int verticesCount = trianglesCount * 3;
        int indicesCount = trianglesCount * 3;
        
        var mesh = context.Allocate(verticesCount, indicesCount);
        
        Color positiveColor = GetPositiveColor();
        Color negativeColor = GetNegativeColor();

        int indexCounter = 0;
        
        for (int i = 0; i < _pointsCount - 1; i++)
        {
            float x1 = _points[i].x - _points[0].x;
            float width = _points[i + 1].x - _points[i].x;
            float height = _points[i].y * _yRatio;

            DrawPillar(mesh, ref indexCounter, x1, width, height, positiveColor, negativeColor);
        }
        
        // additional fake pillar for last point
        {
            float x1 = _points[_pointsCount - 1].x - _points[0].x;
            float width = 10f;
            float height = _points[_pointsCount - 1].y * _yRatio;
            
            DrawPillar(mesh, ref indexCounter, x1, width, height, positiveColor, negativeColor);
        }
    }

    private void DrawPillar(MeshWriteData mesh, ref int indexCounter, float x, float width, float height, Color positiveColor, Color negativeColor)
    {
        float x1 = x;
        float y1 = height;
        float x2 = x + width;
        float y2 = height;
        float x3 = x;
        float y3 = 0f;
        float x4 = x + width;
        float y4 = 0f;
            
        Color color = y1 > 0 ? positiveColor : negativeColor;
            
        AddTriangle(mesh, color, ref indexCounter, x1, y1, x2, y2, x3, y3);
        AddTriangle(mesh, color, ref indexCounter, x2, y2, x3, y3, x4, y4);
    }

    private void AddTriangle(MeshWriteData mesh, Color color, ref int indexCounter, float x1, float y1, float x2, float y2, float x3, float y3)
    {
        Vector3 p1 = new Vector3(x1, y1, 0f);
        Vector3 p2 = new Vector3(x2, y2, 0f);
        Vector3 p3 = new Vector3(x3, y3, 0f);
        
        Vector3 crossProduct = Vector3.Cross(p3 - p1, p2 - p1);

        // means normal vector inverted. For proper rendering need positive normal
        if (crossProduct.z < 0)
        {
            (p3, p2) = (p2, p3);
        }
        
        AddVertex(mesh, color, p1.x, p1.y);
        AddVertex(mesh, color, p2.x, p2.y);
        AddVertex(mesh, color, p3.x, p3.y);

        for (int i = 0; i < 3; i++)
        {
            mesh.SetNextIndex((ushort) indexCounter);
            indexCounter++;
        }
    }
    
    private void AddVertex(MeshWriteData mesh, Color color, float x, float y)
    {
        Vertex vertex = new Vertex()
        {
            position = GetVertexPosition(x, y),
            tint = color,
        };
        
        mesh.SetNextVertex(vertex);
    }

    private Vector3 GetVertexPosition(float x, float y)
    {
        var result = new Vector3(x, y, Vertex.nearZ);

        var inverted = FixPosition(result);
        
        return inverted;
    }
    
    private Vector3 FixPosition(Vector3 origin)
    {
        Vector3 result = origin;

        // make y == 0 at half content
        result.y = GetHalfHeight() - result.y;
        
        result.x *= _horizontalScale;

        return result;
    }

    private void AdjustWidth()
    {
        if (_pointsCount < 1)
        {
            style.width = new StyleLength(0f);
            return;
        }
        
        float width = _points[_pointsCount - 1].x - _points[0].x;

        width *= _horizontalScale;

        // for text
        width += 50f;
        
        style.width = new StyleLength(width);
    }

    private void CalculateRatioY()
    {
        float biggestY = _points.Max(v => Mathf.Abs(v.y));
        float maxY = GetHalfHeight();

        if (biggestY != 0f)
        {
            _yRatio = maxY / biggestY;
        }
    }

    private void CreateLabels()
    {
        Clear();

        if (_showValueLabels == false)
        {
            return;
        }
        
        for (int i = 0; i < _pointsCount; i++)
        {
            float x = _points[i].x - _points[0].x;
            float y = _points[i].y * _yRatio;
            
            var pos = new Vector3(x, y, 0f);
            
            pos = FixPosition(pos);
            
            var label = new Label(string.Format(LabelFormat, _points[i].x, _points[i].y));
            
            label.AddToClassList(ValueLabelClass);
            label.style.position = new StyleEnum<Position>(Position.Absolute);
            label.style.left = new StyleLength(pos.x);
            label.style.top = new StyleLength(pos.y);

            Add(label);
        }
    }
    
    private float GetHalfHeight() => contentRect.height / 2f;
    
    private Color GetPositiveColor() => _positiveStyle.backgroundColor;
    private Color GetPositiveBorderColor() => _positiveStyle.borderTopColor;
    private float GetPositiveBorderSize() => _positiveStyle.borderTopWidth;
    
    private Color GetNegativeColor() => _negativeStyle.backgroundColor;
    private Color GetNegativeBorderColor() => _negativeStyle.borderTopColor;
    private float GetNegativeBorderSize() => _negativeStyle.borderTopWidth;
}