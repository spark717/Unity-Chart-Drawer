using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Random = Unity.Mathematics.Random;

public class ImguiTestWindow : EditorWindow
{
    [MenuItem("Test/ImguiTestWindow")]
    public static void ShowExample()
    {
        ImguiTestWindow wnd = GetWindow<ImguiTestWindow>();
        wnd.titleContent = new GUIContent("TestWindow");
    }

    private ChartDrawer _chartDrawer;
    private ScrollView _scrollView;
    private Slider _scaleSlider;
    private Toggle _pillarModeToggle;
    private Toggle _showLabelsToggle;
    private Button _clearButton;
    private Button _addPointButton;

    Random rnd;
    
    private float lastX = 0;
    
    public void CreateGUI()
    {
        try
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/TestWindow.uxml");
        
            VisualElement root = visualTree.Instantiate();
        
            root.style.flexBasis = 0f;
            root.style.flexGrow = 1f;
        
            rootVisualElement.Add(root);

            Q(out _scrollView);
            Q(out _scaleSlider);
            Q(out _pillarModeToggle, "pillar-mode");
            Q(out _showLabelsToggle, "show-labels");
            Q(out _clearButton, "clear-button");
            Q(out _addPointButton, "add-point-button");
            
            _chartDrawer = new ChartDrawer(100);

            _chartDrawer.SetHorizontalScale(500);
            
            _scrollView.contentContainer.Add(_chartDrawer);

            //_scaleSlider.value = 1f;
            //_scaleSlider.RegisterValueChangedCallback(OnScaleChanged);

            _pillarModeToggle.RegisterValueChangedCallback(OnPillarModeChanged);

            _showLabelsToggle.RegisterValueChangedCallback(OnShowLabelsChanged);

            _addPointButton.clicked += OnAddPointButtonClicked;
            
            _clearButton.clicked += OnClearClicked;

            rnd.InitState();
        
            _scrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            
            DebugData.VelocityChanged = OnVelocityChanged;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private void OnVelocityChanged()
    {
        _chartDrawer.AddPoint(DebugData.Time, DebugData.Velocity);
    }

    private void OnClearClicked()
    {
        _chartDrawer.ClearPoints();
    }

    private void OnShowLabelsChanged(ChangeEvent<bool> evt)
    {
        bool showLabels = evt.newValue;
        
        _chartDrawer.SetShowValueLebels(showLabels);
    }

    private void OnPillarModeChanged(ChangeEvent<bool> evt)
    {
        bool isPillarMode = evt.newValue;

        _chartDrawer.SetPillarMode(isPillarMode);
    }

    private void OnScaleChanged(ChangeEvent<float> evt)
    {
        float scale = evt.newValue;
        _chartDrawer.SetHorizontalScale(scale);
    }

    private void OnAddPointButtonClicked()
    {
        float y = rnd.NextFloat(-100, 100);
        float dx = rnd.NextFloat(1f, 50f);
        
        lastX += dx;
        
        _chartDrawer.AddPoint(lastX, y);
    }

    private void OnGeometryChanged(GeometryChangedEvent e)
    {
        if (_scrollView.contentContainer.contentRect.width > _scrollView.contentViewport.contentRect.width)
        {
            _scrollView.horizontalScroller.value = _scrollView.horizontalScroller.highValue;
        }
    }

    private void Q<T>(out T view, string viewName = null) where T : VisualElement
    {
        view = rootVisualElement.Q<T>(viewName);
    } 
}