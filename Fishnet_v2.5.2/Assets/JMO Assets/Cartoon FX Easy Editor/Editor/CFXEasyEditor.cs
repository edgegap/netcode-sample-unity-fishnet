using UnityEditor;
using UnityEngine;

// Cartoon FX Easy Editor
// (c) 2013-2017 - Jean Moreno

public class CFXEasyEditor : EditorWindow
{
    private static CFXEasyEditor SingleWindow;

    public static GUIStyle _LineStyle;

    //Change Start Color
    private bool AffectAlpha = true;

    //Foldouts
    private bool basicFoldout;
    private bool colorFoldout;
    private Color ColorValue = Color.white;
    private Color ColorValue2 = Color.white;
    private bool copyFoldout;

    //Delay
    private float DelayValue = 1.0f;

    //Duration
    private float DurationValue = 5.0f;
    private bool foldoutChanged;

    //Change Lightness
    private readonly int LightnessStep = 10;
    private float LTScalingValue = 1.0f;

    private readonly ParticleSystemModule[] modulesToCopy =
    {
        new("Initial", null),
        new("Emission", "EmissionModule"),
        new("Shape", "ShapeModule"),
        new("Velocity", "VelocityModule"),
        new("Limit Velocity", "ClampVelocityModule"),
        new("Force", "ForceModule"),
        new("Color over Lifetime", "ColorModule"),
        new("Color by Speed", "ColorBySpeedModule"),
        new("Size over Lifetime", "SizeModule"),
        new("Size by Speed", "SizeBySpeedModule"),
        new("Rotation over Lifetime", "RotationModule"),
        new("Rotation by Speed", "RotationBySpeedModule"),
        new("Collision", "CollisionModule"),
        new("Sub Emitters", "SubModule"),
        new("Texture Animation", "UVModule"),
        new("Renderer", null),
        new("Inherit Velocity", "InheritVelocityModule"),
        new("Triggers", "TriggerModule"),
        new("Lights", "LightsModule"),
        new("Noise", "NoiseModule"),
        new("Trails", "TrailModule")
    };

    private bool needSpace;
    private bool pref_HideDisabledModulesCopy;
    private bool pref_IncludeChildren;

    //Editor Prefs
    private bool pref_ShowAsToolbox;

    //Properties to scale, with per-version differences
    private readonly string[] PropertiesToScale =
    {
        //Initial
        "InitialModule.startSize.scalar",
        "InitialModule.startSpeed.scalar",
#if UNITY_2017_1_OR_NEWER
        "InitialModule.startSize.minScalar",
        "InitialModule.startSpeed.minScalar",
#endif
        //Size by Speed
        "SizeBySpeedModule.range.x",
        "SizeBySpeedModule.range.y",
        //Velocity over Lifetime
        "VelocityModule.x.scalar",
        "VelocityModule.y.scalar",
        "VelocityModule.z.scalar",
#if UNITY_2017_1_OR_NEWER
        "VelocityModule.x.minScalar",
        "VelocityModule.y.minScalar",
        "VelocityModule.z.minScalar",
#endif
        //Limit Velocity over Lifetime
        "ClampVelocityModule.x.scalar",
        "ClampVelocityModule.y.scalar",
        "ClampVelocityModule.z.scalar",
#if UNITY_2017_1_OR_NEWER
        "ClampVelocityModule.x.minScalar",
        "ClampVelocityModule.y.minScalar",
        "ClampVelocityModule.z.minScalar",
#endif
        "ClampVelocityModule.magnitude.scalar",
        //Force over Lifetime
        "ForceModule.x.scalar",
        "ForceModule.y.scalar",
        "ForceModule.z.scalar",
#if UNITY_2017_1_OR_NEWER
        "ForceModule.x.minScalar",
        "ForceModule.y.minScalar",
        "ForceModule.z.minScalar",
#endif
        //Special cases per Unity version
#if UNITY_2017_1_OR_NEWER
        "ShapeModule.m_Scale.x",
        "ShapeModule.m_Scale.y",
        "ShapeModule.m_Scale.z",
#else
		"ShapeModule.boxX",
		"ShapeModule.boxY",
		"ShapeModule.boxZ",
#if UNITY_5_6_OR_NEWER
		"ShapeModule.radius.value",
#else
		"ShapeModule.radius",
#endif
#endif
#if UNITY_5_5_OR_NEWER
        "EmissionModule.rateOverDistance.scalar",
        "InitialModule.gravityModifier.scalar",
#if UNITY_2017_1_OR_NEWER
        "EmissionModule.rateOverDistance.minScalar",
        "InitialModule.gravityModifier.minScalar",
#endif
#else
		"InitialModule.gravityModifier",
		"EmissionModule.rate.scalar",
#endif
    };

    //Scale
    private float ScalingValue = 2.0f;

    private int SelectedParticleSystemsCount;

    //Module copying system
    private ParticleSystem sourceObject;
    private bool TintColorModule = true;
    private bool TintColorSpeedModule = true;
    private Color TintColorValue = Color.white;
    private float TintHueShiftValue;

    //Tint
    private bool TintStartColor = true;

    public static GUIStyle LineStyle
    {
        get
        {
            if (_LineStyle == null)
            {
                _LineStyle = new GUIStyle();
                _LineStyle.normal.background = EditorGUIUtility.whiteTexture;
                _LineStyle.stretchWidth = true;
            }

            return _LineStyle;
        }
    }

    private void OnEnable()
    {
        //Load Settings
        pref_ShowAsToolbox = EditorPrefs.GetBool("CFX_ShowAsToolbox", true);
        pref_IncludeChildren = EditorPrefs.GetBool("CFX_IncludeChildren", true);
        pref_HideDisabledModulesCopy = EditorPrefs.GetBool("CFX_HideDisabledModulesCopy", true);
        basicFoldout = EditorPrefs.GetBool("CFX_BasicFoldout", false);
        colorFoldout = EditorPrefs.GetBool("CFX_ColorFoldout", false);
        copyFoldout = EditorPrefs.GetBool("CFX_CopyFoldout", false);

        RefreshCurrentlyEnabledModules();
    }

    private void OnDisable()
    {
        //Save Settings
        EditorPrefs.SetBool("CFX_BasicFoldout", basicFoldout);
        EditorPrefs.SetBool("CFX_ColorFoldout", colorFoldout);
        EditorPrefs.SetBool("CFX_CopyFoldout", copyFoldout);
    }

    private void OnGUI()
    {
        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.label);
        GUI.Label(rect, "Cartoon FX Easy Editor", EditorStyles.boldLabel);
        var guiColor = GUI.color;
        GUI.color *= new Color(.8f, .8f, .8f, 1f);
        pref_ShowAsToolbox = GUILayout.Toggle(pref_ShowAsToolbox,
            new GUIContent("Toolbox",
                "If enabled, the window will be displayed as an external toolbox.\nElse it will act as a dockable Unity window."),
            EditorStyles.miniButton, GUILayout.Width(65));
        GUI.color = guiColor;
        if (GUI.changed)
        {
            EditorPrefs.SetBool("CFX_ShowAsToolbox", pref_ShowAsToolbox);
            Close();
            ShowWindow();
        }

        GUILayout.EndHorizontal();
        GUILayout.Label("Easily change properties of any Particle System!", EditorStyles.miniLabel);

        //----------------------------------------------------------------

        pref_IncludeChildren = GUILayout.Toggle(pref_IncludeChildren,
            new GUIContent("Include Children",
                "If checked, changes will affect every Particle Systems from each child of the selected GameObject(s)"));
        if (GUI.changed)
        {
            EditorPrefs.SetBool("CFX_IncludeChildren", pref_IncludeChildren);
            UpdateSelectionCount();
        }

        GUILayout.Label(
            string.Format("{0} selected Particle System{1}", SelectedParticleSystemsCount,
                SelectedParticleSystemsCount > 1 ? "s" : ""), EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("Test effect(s):");
        using (new EditorGUI.DisabledScope(SelectedParticleSystemsCount <= 0))
        {
            if (GUILayout.Button("Play", EditorStyles.miniButtonLeft, GUILayout.Width(50f)))
                foreach (var go in Selection.gameObjects)
                {
                    var systems = go.GetComponents<ParticleSystem>();
                    if (systems.Length == 0) continue;
                    foreach (var system in systems)
                        system.Play(pref_IncludeChildren);
                }

            if (GUILayout.Button("Pause", EditorStyles.miniButtonMid, GUILayout.Width(50f)))
                foreach (var go in Selection.gameObjects)
                {
                    var systems = go.GetComponents<ParticleSystem>();
                    if (systems.Length == 0) continue;
                    foreach (var system in systems)
                        system.Pause(pref_IncludeChildren);
                }

            if (GUILayout.Button("Stop", EditorStyles.miniButtonMid, GUILayout.Width(50f)))
                foreach (var go in Selection.gameObjects)
                {
                    var systems = go.GetComponents<ParticleSystem>();
                    if (systems.Length == 0) continue;
                    foreach (var system in systems)
                        system.Stop(pref_IncludeChildren);
                }

            if (GUILayout.Button("Clear", EditorStyles.miniButtonRight, GUILayout.Width(50f)))
                foreach (var go in Selection.gameObjects)
                {
                    var systems = go.GetComponents<ParticleSystem>();
                    if (systems.Length == 0) continue;
                    foreach (var system in systems)
                    {
                        system.Stop(pref_IncludeChildren);
                        system.Clear(pref_IncludeChildren);
                    }
                }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        //----------------------------------------------------------------

        //Separator
        GUISeparator();
        //GUILayout.Box("",GUILayout.Width(this.position.width - 12), GUILayout.Height(3));

        EditorGUI.BeginChangeCheck();
        basicFoldout = EditorGUILayout.Foldout(basicFoldout, "QUICK EDIT");
        if (EditorGUI.EndChangeCheck()) foldoutChanged = true;
        if (basicFoldout)
        {
            //----------------------------------------------------------------

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(
                    new GUIContent("Scale Size",
                        "Changes the size of the Particle System(s) and other values accordingly (speed, gravity, etc.)"),
                    GUILayout.Width(120))) applyScale();
            GUILayout.Label("Multiplier:", GUILayout.Width(110));
            ScalingValue = EditorGUILayout.FloatField(ScalingValue, GUILayout.Width(50));
            if (ScalingValue <= 0) ScalingValue = 0.1f;
            GUILayout.EndHorizontal();

            //----------------------------------------------------------------

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(
                    new GUIContent("Set Speed",
                        "Changes the simulation speed of the Particle System(s)\n1 = default speed"),
                    GUILayout.Width(120))) applySpeed();
            GUILayout.Label("Speed:", GUILayout.Width(110));
            LTScalingValue = EditorGUILayout.FloatField(LTScalingValue, GUILayout.Width(50));
            if (LTScalingValue < 0.0f) LTScalingValue = 0.0f;
            else if (LTScalingValue > 9999) LTScalingValue = 9999;
            GUILayout.EndHorizontal();

            //----------------------------------------------------------------

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Set Duration", "Changes the duration of the Particle System(s)"),
                    GUILayout.Width(120))) applyDuration();
            GUILayout.Label("Duration (sec):", GUILayout.Width(110));
            DurationValue = EditorGUILayout.FloatField(DurationValue, GUILayout.Width(50));
            if (DurationValue < 0.1f) DurationValue = 0.1f;
            else if (DurationValue > 9999) DurationValue = 9999;
            GUILayout.EndHorizontal();

            //----------------------------------------------------------------

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Set Delay", "Changes the delay of the Particle System(s)"),
                    GUILayout.Width(120))) applyDelay();
            GUILayout.Label("Delay :", GUILayout.Width(110));
            DelayValue = EditorGUILayout.FloatField(DelayValue, GUILayout.Width(50));
            if (DelayValue < 0.0f) DelayValue = 0.0f;
            else if (DelayValue > 9999f) DelayValue = 9999f;
            GUILayout.EndHorizontal();

            //----------------------------------------------------------------

            GUILayout.Space(2);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(
                    new GUIContent("Loop",
                        "Loop the effect (might not work properly on some effects such as explosions)"),
                    EditorStyles.miniButtonLeft)) loopEffect(true);
            if (GUILayout.Button(new GUIContent("Unloop", "Remove looping from the effect"),
                    EditorStyles.miniButtonRight)) loopEffect(false);
            if (GUILayout.Button(new GUIContent("Prewarm On", "Prewarm the effect (if looped)"),
                    EditorStyles.miniButtonLeft)) prewarmEffect(true);
            if (GUILayout.Button(new GUIContent("Prewarm Off", "Don't prewarm the effect (if looped)"),
                    EditorStyles.miniButtonRight)) prewarmEffect(false);
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            //----------------------------------------------------------------
        }

        //Separator
        GUISeparator();
        //GUILayout.Box("",GUILayout.Width(this.position.width - 12), GUILayout.Height(3));

        EditorGUI.BeginChangeCheck();
        colorFoldout = EditorGUILayout.Foldout(colorFoldout, "COLOR EDIT");
        if (EditorGUI.EndChangeCheck()) foldoutChanged = true;
        if (colorFoldout)
        {
            //----------------------------------------------------------------

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(
                    new GUIContent("Set Start Color(s)",
                        "Changes the color(s) of the Particle System(s)\nSecond Color is used when Start Color is 'Random Between Two Colors'."),
                    GUILayout.Width(120))) applyColor();
            ColorValue = EditorGUILayout.ColorField(ColorValue);
            ColorValue2 = EditorGUILayout.ColorField(ColorValue2);
            AffectAlpha = GUILayout.Toggle(AffectAlpha,
                new GUIContent("Alpha", "If checked, the alpha value will also be changed"));
            GUILayout.EndHorizontal();

            //----------------------------------------------------------------

            GUILayout.Space(8);

            using (new EditorGUI.DisabledScope(!TintStartColor && !TintColorModule && !TintColorSpeedModule))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(
                        new GUIContent("Tint Colors",
                            "Colorize the Particle System(s) to a specific color, including gradients!\n(preserving their saturation and value)"),
                        GUILayout.Width(120))) tintColor();
                TintColorValue = EditorGUILayout.ColorField(TintColorValue);
                TintColorValue = HSLColor.FromRGBA(TintColorValue).VividColor();
                GUILayout.EndHorizontal();

                //----------------------------------------------------------------

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(
                        new GUIContent("Hue Shift",
                            "Tints the colors of the Particle System(s) by shifting the original hues by a value\n(preserving differences between colors)"),
                        GUILayout.Width(120))) hueShift();
                TintHueShiftValue = EditorGUILayout.Slider(TintHueShiftValue, -180f, 180f);
                GUILayout.EndHorizontal();
            }

            //----------------------------------------------------------------

            /*
            GUILayout.BeginHorizontal();
            GUILayout.Label("Add/Substract Lightness:");
            
            LightnessStep = EditorGUILayout.IntField(LightnessStep, GUILayout.Width(30));
            if(LightnessStep > 99) LightnessStep = 99;
            else if(LightnessStep < 1) LightnessStep = 1;
            GUILayout.Label("%");
            
            if(GUILayout.Button("-", EditorStyles.miniButtonLeft, GUILayout.Width(22)))
            {
                addLightness(true);
            }
            if(GUILayout.Button("+", EditorStyles.miniButtonRight, GUILayout.Width(22)))
            {
                addLightness(false);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            */

            //----------------------------------------------------------------

            GUILayout.Label("Color Modules to affect:");

            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            TintStartColor = GUILayout.Toggle(TintStartColor,
                new GUIContent("Start Color", "If checked, the \"Start Color\" value(s) will be affected."),
                EditorStyles.toolbarButton);
            TintColorModule = GUILayout.Toggle(TintColorModule,
                new GUIContent("Color over Lifetime",
                    "If checked, the \"Color over Lifetime\" value(s) will be affected."), EditorStyles.toolbarButton);
            TintColorSpeedModule = GUILayout.Toggle(TintColorSpeedModule,
                new GUIContent("Color by Speed", "If checked, the \"Color by Speed\" value(s) will be affected."),
                EditorStyles.toolbarButton);
            GUILayout.Space(4);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            //----------------------------------------------------------------
        }

        //Separator
        GUISeparator();
        //GUILayout.Box("",GUILayout.Width(this.position.width - 12), GUILayout.Height(3));
//		GUILayout.Space(6);

        //----------------------------------------------------------------

        EditorGUI.BeginChangeCheck();
        copyFoldout = EditorGUILayout.Foldout(copyFoldout, "COPY MODULES");
        if (EditorGUI.EndChangeCheck()) foldoutChanged = true;
        if (copyFoldout)
        {
            EditorGUILayout.HelpBox("Copy selected modules from a Particle System to others!", MessageType.Info);

            GUILayout.Label("Source Particle System to copy from:");
            GUILayout.BeginHorizontal();
            sourceObject = (ParticleSystem)EditorGUILayout.ObjectField(sourceObject, typeof(ParticleSystem), true);
            if (GUILayout.Button("Get Selected", EditorStyles.miniButton))
                if (Selection.activeGameObject != null)
                {
                    var ps = Selection.activeGameObject.GetComponent<ParticleSystem>();
                    if (ps != null) sourceObject = ps;
                }

            if (GUI.changed) RefreshCurrentlyEnabledModules();
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Modules to Copy:");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(" Select All ", EditorStyles.miniButtonLeft))
                for (var i = 0; i < modulesToCopy.Length; i++)
                    if (modulesToCopy[i].enabledInSource || !pref_HideDisabledModulesCopy)
                        modulesToCopy[i].selected = true;
            if (GUILayout.Button(" Select None ", EditorStyles.miniButtonRight))
                for (var i = 0; i < modulesToCopy.Length; i++)
                    modulesToCopy[i].selected = false;
            GUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(sourceObject == null))
            {
                pref_HideDisabledModulesCopy = GUILayout.Toggle(pref_HideDisabledModulesCopy,
                    new GUIContent(" Hide disabled modules",
                        "Will hide modules that are disabled on the current source Particle System"));
                if (GUI.changed)
                {
                    EditorPrefs.SetBool("CFX_HideDisabledModulesCopy", pref_HideDisabledModulesCopy);
                    RefreshCurrentlyEnabledModules();
                }
            }

            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUISelectModule(modulesToCopy[0]);
            GUISelectModule(modulesToCopy[1]);
            GUISelectModule(modulesToCopy[2]);
            GUILayout.EndHorizontal();

            SelectModulesSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Space(8f);
#if UNITY_5_4_OR_NEWER
            GUISelectModule(modulesToCopy[3]);
            GUISelectModule(modulesToCopy[4]);
            GUISelectModule(modulesToCopy[16]);
            GUISelectModule(modulesToCopy[5]);
#else
			GUISelectModule(modulesToCopy[3]);
			GUISelectModule(modulesToCopy[4]);
			GUISelectModule(modulesToCopy[5]);
#endif
            GUILayout.EndHorizontal();

            SelectModulesSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUISelectModule(modulesToCopy[6]);
            GUISelectModule(modulesToCopy[7]);
            GUILayout.EndHorizontal();

            SelectModulesSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUISelectModule(modulesToCopy[8]);
            GUISelectModule(modulesToCopy[9]);
            GUILayout.EndHorizontal();

            SelectModulesSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUISelectModule(modulesToCopy[10]);
            GUISelectModule(modulesToCopy[11]);
            GUILayout.EndHorizontal();

            SelectModulesSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUISelectModule(modulesToCopy[12]);
#if UNITY_5_4_OR_NEWER
            GUISelectModule(modulesToCopy[17]);
#endif
            GUISelectModule(modulesToCopy[13]);
            GUILayout.EndHorizontal();

            SelectModulesSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Space(8f);
#if UNITY_5_5_OR_NEWER
            GUISelectModule(modulesToCopy[20]);
            GUISelectModule(modulesToCopy[19]);
            GUISelectModule(modulesToCopy[18]);
#endif
            GUISelectModule(modulesToCopy[14]);
            GUILayout.EndHorizontal();

            SelectModulesSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUISelectModule(modulesToCopy[15]);
            GUILayout.EndHorizontal();

            GUI.color = guiColor;

            SelectModulesSpace();
            if (GUILayout.Button("Copy properties to selected Object(s)"))
            {
                var foundPs = false;
                foreach (var go in Selection.gameObjects)
                {
                    ParticleSystem[] systems;
                    if (pref_IncludeChildren) systems = go.GetComponentsInChildren<ParticleSystem>(true);
                    else systems = go.GetComponents<ParticleSystem>();

                    if (systems.Length == 0) continue;

                    foundPs = true;
                    foreach (var system in systems) CopyModules(sourceObject, system);
                }

                if (!foundPs)
                    Debug.LogWarning("Cartoon FX Easy Editor: No Particle System found in the selected GameObject(s)!");
            }
        }

        //----------------------------------------------------------------

        GUILayout.Space(8);

        //Resize window
        if (foldoutChanged && Event.current.type == EventType.Repaint)
        {
            foldoutChanged = false;

            var r = GUILayoutUtility.GetLastRect();
            minSize = new Vector2(300, r.y + 8);
            maxSize = new Vector2(300, r.y + 8);
        }
    }

    private void OnFocus()
    {
        RefreshCurrentlyEnabledModules();
    }

    private void OnSelectionChange()
    {
        UpdateSelectionCount();
        Repaint();
    }

    [MenuItem("Window/Cartoon FX Easy Editor")]
    private static void ShowWindow()
    {
        var window = GetWindow<CFXEasyEditor>(EditorPrefs.GetBool("CFX_ShowAsToolbox", true), "Easy Editor", true);
        window.minSize = new Vector2(300, 8);
        window.maxSize = new Vector2(300, 8);
        window.foldoutChanged = true;
    }

    private void UpdateSelectionCount()
    {
        SelectedParticleSystemsCount = 0;
        foreach (var go in Selection.gameObjects)
        {
            ParticleSystem[] systems;
            if (pref_IncludeChildren)
                systems = go.GetComponentsInChildren<ParticleSystem>(true);
            else
                systems = go.GetComponents<ParticleSystem>();

            SelectedParticleSystemsCount += systems.Length;
        }
    }

    private void RefreshCurrentlyEnabledModules()
    {
        if (sourceObject != null)
        {
            var so = new SerializedObject(sourceObject);
            for (var i = 0; i < modulesToCopy.Length; i++) modulesToCopy[i].CheckIfEnabledInSource(so);
        }
        else
        {
            for (var i = 0; i < modulesToCopy.Length; i++)
                modulesToCopy[i].enabledInSource = true;
        }

        foldoutChanged = true;
    }

    private void SelectModulesSpace()
    {
        if (needSpace)
        {
            GUILayout.Space(4);
            needSpace = false;
        }
    }

    private void GUISelectModule(ParticleSystemModule module)
    {
        if (module.enabledInSource || !pref_HideDisabledModulesCopy)
        {
            module.selected = GUILayout.Toggle(module.selected, module.name, EditorStyles.toolbarButton,
                GUILayout.ExpandWidth(false));
            needSpace = true;
        }
    }

    //Loop effects
    private void loopEffect(bool setLoop)
    {
        foreach (var go in Selection.gameObjects)
        {
            //Scale Shuriken Particles Values
            ParticleSystem[] systems;
            if (pref_IncludeChildren)
                systems = go.GetComponentsInChildren<ParticleSystem>(true);
            else
                systems = go.GetComponents<ParticleSystem>();

            foreach (var ps in systems)
            {
                var so = new SerializedObject(ps);
                so.FindProperty("looping").boolValue = setLoop;
                so.ApplyModifiedProperties();
            }
        }
    }

    //Prewarm effects
    private void prewarmEffect(bool setPrewarm)
    {
        foreach (var go in Selection.gameObjects)
        {
            //Scale Shuriken Particles Values
            ParticleSystem[] systems;
            if (pref_IncludeChildren)
                systems = go.GetComponentsInChildren<ParticleSystem>(true);
            else
                systems = go.GetComponents<ParticleSystem>();

            foreach (var ps in systems)
            {
                var so = new SerializedObject(ps);
                so.FindProperty("prewarm").boolValue = setPrewarm;
                so.ApplyModifiedProperties();
            }
        }
    }

    //Scale Size
    private void applyScale()
    {
        foreach (var go in Selection.gameObjects)
        {
            //Scale Shuriken Particles Values
            ParticleSystem[] systems;
            if (pref_IncludeChildren)
                systems = go.GetComponentsInChildren<ParticleSystem>(true);
            else
                systems = go.GetComponents<ParticleSystem>();

            foreach (var ps in systems) ScaleParticleValues(ps, go);

            //Scale Lights' range
            var lights = go.GetComponentsInChildren<Light>();
            foreach (var light in lights)
            {
                light.range *= ScalingValue;
                light.transform.localPosition *= ScalingValue;
            }
        }
    }

    //Change Color
    private void applyColor()
    {
        foreach (var go in Selection.gameObjects)
        {
            ParticleSystem[] systems;
            if (pref_IncludeChildren)
                systems = go.GetComponentsInChildren<ParticleSystem>(true);
            else
                systems = go.GetComponents<ParticleSystem>();

            foreach (var ps in systems)
            {
                var psSerial = new SerializedObject(ps);
                if (!AffectAlpha)
                {
                    psSerial.FindProperty("InitialModule.startColor.maxColor").colorValue = new Color(ColorValue.r,
                        ColorValue.g, ColorValue.b,
                        psSerial.FindProperty("InitialModule.startColor.maxColor").colorValue.a);
                    psSerial.FindProperty("InitialModule.startColor.minColor").colorValue = new Color(ColorValue2.r,
                        ColorValue2.g, ColorValue2.b,
                        psSerial.FindProperty("InitialModule.startColor.minColor").colorValue.a);
                }
                else
                {
                    psSerial.FindProperty("InitialModule.startColor.maxColor").colorValue = ColorValue;
                    psSerial.FindProperty("InitialModule.startColor.minColor").colorValue = ColorValue2;
                }

                psSerial.ApplyModifiedProperties();
            }
        }
    }

    //TINT COLORS ================================================================================================================================

    private void tintColor()
    {
        if (!TintStartColor && !TintColorModule && !TintColorSpeedModule)
        {
            Debug.LogWarning(
                "Cartoon FX Easy Editor: You must toggle at least one of the three Color Modules to be able to tint anything!");
            return;
        }

        var hue = HSLColor.FromRGBA(TintColorValue).h;

        foreach (var go in Selection.gameObjects)
        {
            ParticleSystem[] systems;
            if (pref_IncludeChildren)
                systems = go.GetComponentsInChildren<ParticleSystem>(true);
            else
                systems = go.GetComponents<ParticleSystem>();

            foreach (var ps in systems)
            {
                var psSerial = new SerializedObject(ps);

                if (TintStartColor)
                    GenericTintColorProperty(psSerial.FindProperty("InitialModule.startColor"), hue, false);

                if (TintColorModule)
                    GenericTintColorProperty(psSerial.FindProperty("ColorModule.gradient"), hue, false);

                if (TintColorSpeedModule)
                    GenericTintColorProperty(psSerial.FindProperty("ColorBySpeedModule.gradient"), hue, false);

                psSerial.ApplyModifiedProperties();
            }
        }
    }

    private void hueShift()
    {
        if (!TintStartColor && !TintColorModule && !TintColorSpeedModule)
        {
            Debug.LogWarning(
                "Cartoon FX Easy Editor: You must toggle at least one of the three Color Modules to be able to use hue shift!");
            return;
        }

        foreach (var go in Selection.gameObjects)
        {
            ParticleSystem[] systems;
            if (pref_IncludeChildren)
                systems = go.GetComponentsInChildren<ParticleSystem>(true);
            else
                systems = go.GetComponents<ParticleSystem>();

            foreach (var ps in systems)
            {
                var psSerial = new SerializedObject(ps);

                if (TintStartColor)
                    GenericTintColorProperty(psSerial.FindProperty("InitialModule.startColor"), TintHueShiftValue,
                        true);

                if (TintColorModule)
                    GenericTintColorProperty(psSerial.FindProperty("ColorModule.gradient"), TintHueShiftValue, true);

                if (TintColorSpeedModule)
                    GenericTintColorProperty(psSerial.FindProperty("ColorBySpeedModule.gradient"), TintHueShiftValue,
                        true);

                psSerial.ApplyModifiedProperties();
            }
        }
    }

    private void GenericTintColorProperty(SerializedProperty colorProperty, float hue, bool shift)
    {
        var state = colorProperty.FindPropertyRelative("minMaxState").intValue;
        switch (state)
        {
            //Constant Color
            case 0:
                colorProperty.FindPropertyRelative("maxColor").colorValue = HSLColor
                    .FromRGBA(colorProperty.FindPropertyRelative("maxColor").colorValue).ColorWithHue(hue, shift);
                break;

            //Gradient
            case 1:
                TintGradient(colorProperty.FindPropertyRelative("maxGradient"), hue, shift);
                break;

            //Random between 2 Colors
            case 2:
                colorProperty.FindPropertyRelative("minColor").colorValue = HSLColor
                    .FromRGBA(colorProperty.FindPropertyRelative("minColor").colorValue).ColorWithHue(hue, shift);
                colorProperty.FindPropertyRelative("maxColor").colorValue = HSLColor
                    .FromRGBA(colorProperty.FindPropertyRelative("maxColor").colorValue).ColorWithHue(hue, shift);
                break;

            //Random between 2 Gradients
            case 3:
                TintGradient(colorProperty.FindPropertyRelative("maxGradient"), hue, shift);
                TintGradient(colorProperty.FindPropertyRelative("minGradient"), hue, shift);
                break;
        }
    }

    private void TintGradient(SerializedProperty gradientProperty, float hue, bool shift)
    {
        gradientProperty.FindPropertyRelative("key0").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key0").colorValue).ColorWithHue(hue, shift);
        gradientProperty.FindPropertyRelative("key1").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key1").colorValue).ColorWithHue(hue, shift);
        gradientProperty.FindPropertyRelative("key2").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key2").colorValue).ColorWithHue(hue, shift);
        gradientProperty.FindPropertyRelative("key3").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key3").colorValue).ColorWithHue(hue, shift);
        gradientProperty.FindPropertyRelative("key4").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key4").colorValue).ColorWithHue(hue, shift);
        gradientProperty.FindPropertyRelative("key5").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key5").colorValue).ColorWithHue(hue, shift);
        gradientProperty.FindPropertyRelative("key6").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key6").colorValue).ColorWithHue(hue, shift);
        gradientProperty.FindPropertyRelative("key7").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key7").colorValue).ColorWithHue(hue, shift);
    }

    //LIGHTNESS OFFSET ================================================================================================================================

    private void addLightness(bool substract)
    {
        if (!TintStartColor && !TintColorModule && !TintColorSpeedModule)
        {
            Debug.LogWarning(
                "Cartoon FX Easy Editor: You must toggle at least one of the three Color Modules to be able to change lightness!");
            return;
        }

        var lightness = LightnessStep / 100f;
        if (substract)
            lightness *= -1f;

        foreach (var go in Selection.gameObjects)
        {
            ParticleSystem[] systems;
            if (pref_IncludeChildren)
                systems = go.GetComponentsInChildren<ParticleSystem>(true);
            else
                systems = go.GetComponents<ParticleSystem>();

            foreach (var ps in systems)
            {
                var psSerial = new SerializedObject(ps);

                if (TintStartColor)
                    GenericAddLightness(psSerial.FindProperty("InitialModule.startColor"), lightness);

                if (TintColorModule)
                    GenericAddLightness(psSerial.FindProperty("ColorModule.gradient"), lightness);

                if (TintColorSpeedModule)
                    GenericAddLightness(psSerial.FindProperty("ColorBySpeedModule.gradient"), lightness);

                psSerial.ApplyModifiedProperties();
                psSerial.Update();
            }
        }
    }

    private void GenericAddLightness(SerializedProperty colorProperty, float lightness)
    {
        var state = colorProperty.FindPropertyRelative("minMaxState").intValue;
        switch (state)
        {
            //Constant Color
            case 0:
                colorProperty.FindPropertyRelative("maxColor").colorValue = HSLColor
                    .FromRGBA(colorProperty.FindPropertyRelative("maxColor").colorValue)
                    .ColorWithLightnessOffset(lightness);
                break;

            //Gradient
            case 1:
                AddLightnessGradient(colorProperty.FindPropertyRelative("maxGradient"), lightness);
                break;

            //Random between 2 Colors
            case 2:
                colorProperty.FindPropertyRelative("minColor").colorValue = HSLColor
                    .FromRGBA(colorProperty.FindPropertyRelative("minColor").colorValue)
                    .ColorWithLightnessOffset(lightness);
                colorProperty.FindPropertyRelative("maxColor").colorValue = HSLColor
                    .FromRGBA(colorProperty.FindPropertyRelative("maxColor").colorValue)
                    .ColorWithLightnessOffset(lightness);
                break;

            //Random between 2 Gradients
            case 3:
                AddLightnessGradient(colorProperty.FindPropertyRelative("maxGradient"), lightness);
                AddLightnessGradient(colorProperty.FindPropertyRelative("minGradient"), lightness);
                break;
        }
    }

    private void AddLightnessGradient(SerializedProperty gradientProperty, float lightness)
    {
        gradientProperty.FindPropertyRelative("key0").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key0").colorValue).ColorWithLightnessOffset(lightness);
        gradientProperty.FindPropertyRelative("key1").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key1").colorValue).ColorWithLightnessOffset(lightness);
        gradientProperty.FindPropertyRelative("key2").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key2").colorValue).ColorWithLightnessOffset(lightness);
        gradientProperty.FindPropertyRelative("key3").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key3").colorValue).ColorWithLightnessOffset(lightness);
        gradientProperty.FindPropertyRelative("key4").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key4").colorValue).ColorWithLightnessOffset(lightness);
        gradientProperty.FindPropertyRelative("key5").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key5").colorValue).ColorWithLightnessOffset(lightness);
        gradientProperty.FindPropertyRelative("key6").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key6").colorValue).ColorWithLightnessOffset(lightness);
        gradientProperty.FindPropertyRelative("key7").colorValue = HSLColor
            .FromRGBA(gradientProperty.FindPropertyRelative("key7").colorValue).ColorWithLightnessOffset(lightness);
    }

    //Scale Lifetime only
    private void applySpeed()
    {
        foreach (var go in Selection.gameObjects)
        {
            ParticleSystem[] systems;
            if (pref_IncludeChildren)
                systems = go.GetComponentsInChildren<ParticleSystem>(true);
            else
                systems = go.GetComponents<ParticleSystem>();

            //Scale Lifetime
            foreach (var ps in systems)
            {
#if UNITY_5_5_OR_NEWER
                var main = ps.main;
                main.simulationSpeed = LTScalingValue;
#else
				ps.playbackSpeed = LTScalingValue;
#endif
            }
        }
    }

    //Set Duration
    private void applyDuration()
    {
        foreach (var go in Selection.gameObjects)
        {
            //Scale Shuriken Particles Values
            ParticleSystem[] systems;
            if (pref_IncludeChildren)
                systems = go.GetComponentsInChildren<ParticleSystem>(true);
            else
                systems = go.GetComponents<ParticleSystem>();

            foreach (var ps in systems)
            {
                var so = new SerializedObject(ps);
                so.FindProperty("lengthInSec").floatValue = DurationValue;
                so.ApplyModifiedProperties();
            }
        }
    }

    //Change delay
    private void applyDelay()
    {
        foreach (var go in Selection.gameObjects)
        {
            ParticleSystem[] systems;
            if (pref_IncludeChildren)
                systems = go.GetComponentsInChildren<ParticleSystem>(true);
            else
                systems = go.GetComponents<ParticleSystem>();

            //Scale Lifetime
            foreach (var ps in systems)
            {
#if UNITY_5_5_OR_NEWER
                var main = ps.main;
                main.startDelay = DelayValue;
#else
				ps.startDelay = DelayValue;
#endif
            }
        }
    }

    //Copy Selected Modules
    private void CopyModules(ParticleSystem source, ParticleSystem dest)
    {
        if (source == null)
        {
            Debug.LogWarning("Cartoon FX Easy Editor: Select a source Particle System to copy properties from first!");
            return;
        }

        var psSource = new SerializedObject(source);
        var psDest = new SerializedObject(dest);

        //Initial Module
        if (modulesToCopy[0].selected)
        {
            psDest.FindProperty("prewarm").boolValue = psSource.FindProperty("prewarm").boolValue;
            psDest.FindProperty("lengthInSec").floatValue = psSource.FindProperty("lengthInSec").floatValue;
            psDest.FindProperty("moveWithTransform").boolValue = psSource.FindProperty("moveWithTransform").boolValue;

            GenericModuleCopy(psSource.FindProperty("InitialModule"), psDest.FindProperty("InitialModule"));

#if UNITY_5_5_OR_NEWER
            var dmain = dest.main;
            dmain.startDelay = source.main.startDelay;
            dmain.loop = source.main.loop;
            dmain.playOnAwake = source.main.playOnAwake;
            dmain.simulationSpeed = source.main.simulationSpeed;
            dmain.startSpeed = source.main.startSpeed;
            dmain.startSize = source.main.startSize;
            dmain.startColor = source.main.startColor;
            dmain.startRotation = source.main.startRotation;
            dmain.startLifetime = source.main.startLifetime;
            dmain.gravityModifier = source.main.gravityModifier;
#else
			dest.startDelay = source.startDelay;
			dest.loop = source.loop;
			dest.playOnAwake = source.playOnAwake;
			dest.playbackSpeed = source.playbackSpeed;
#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2
			dest.emissionRate = source.emissionRate;
#endif
			dest.startSpeed = source.startSpeed;
			dest.startSize = source.startSize;
			dest.startColor = source.startColor;
			dest.startRotation = source.startRotation;
			dest.startLifetime = source.startLifetime;
			dest.gravityModifier = source.gravityModifier;
#endif
        }

        if (modulesToCopy[1].selected)
            GenericModuleCopy(psSource.FindProperty("EmissionModule"), psDest.FindProperty("EmissionModule"));
        if (modulesToCopy[2].selected)
            GenericModuleCopy(psSource.FindProperty("ShapeModule"), psDest.FindProperty("ShapeModule"));
        if (modulesToCopy[3].selected)
            GenericModuleCopy(psSource.FindProperty("VelocityModule"), psDest.FindProperty("VelocityModule"));
        if (modulesToCopy[4].selected)
            GenericModuleCopy(psSource.FindProperty("ClampVelocityModule"), psDest.FindProperty("ClampVelocityModule"));
        if (modulesToCopy[5].selected)
            GenericModuleCopy(psSource.FindProperty("ForceModule"), psDest.FindProperty("ForceModule"));
        if (modulesToCopy[6].selected)
            GenericModuleCopy(psSource.FindProperty("ColorModule"), psDest.FindProperty("ColorModule"));
        if (modulesToCopy[7].selected)
            GenericModuleCopy(psSource.FindProperty("ColorBySpeedModule"), psDest.FindProperty("ColorBySpeedModule"));
        if (modulesToCopy[8].selected)
            GenericModuleCopy(psSource.FindProperty("SizeModule"), psDest.FindProperty("SizeModule"));
        if (modulesToCopy[9].selected)
            GenericModuleCopy(psSource.FindProperty("SizeBySpeedModule"), psDest.FindProperty("SizeBySpeedModule"));
        if (modulesToCopy[10].selected)
            GenericModuleCopy(psSource.FindProperty("RotationModule"), psDest.FindProperty("RotationModule"));
        if (modulesToCopy[11].selected)
            GenericModuleCopy(psSource.FindProperty("RotationBySpeedModule"),
                psDest.FindProperty("RotationBySpeedModule"));
        if (modulesToCopy[12].selected)
            GenericModuleCopy(psSource.FindProperty("CollisionModule"), psDest.FindProperty("CollisionModule"));
        if (modulesToCopy[13].selected) SubModuleCopy(psSource, psDest);
        if (modulesToCopy[14].selected)
            GenericModuleCopy(psSource.FindProperty("UVModule"), psDest.FindProperty("UVModule"));
        if (modulesToCopy[16].selected)
            GenericModuleCopy(psSource.FindProperty("InheritVelocityModule"),
                psDest.FindProperty("InheritVelocityModule"));
        if (modulesToCopy[17].selected)
            GenericModuleCopy(psSource.FindProperty("TriggerModule"), psDest.FindProperty("TriggerModule"));
        if (modulesToCopy[18].selected)
            GenericModuleCopy(psSource.FindProperty("LightsModule"), psDest.FindProperty("LightsModule"));
        if (modulesToCopy[19].selected)
            GenericModuleCopy(psSource.FindProperty("NoiseModule"), psDest.FindProperty("NoiseModule"));
        if (modulesToCopy[20].selected)
            GenericModuleCopy(psSource.FindProperty("TrailModule"), psDest.FindProperty("TrailModule"));

        //Renderer
        if (modulesToCopy[15].selected)
        {
            var rendSource = source.GetComponent<ParticleSystemRenderer>();
            var rendDest = dest.GetComponent<ParticleSystemRenderer>();

            psSource = new SerializedObject(rendSource);
            psDest = new SerializedObject(rendDest);

            var ss = psSource.GetIterator();
            ss.Next(true);

            var sd = psDest.GetIterator();
            sd.Next(true);

            GenericModuleCopy(ss, sd, false);
        }
    }

    //Copy One Module's Values
    private void GenericModuleCopy(SerializedProperty ss, SerializedProperty sd, bool depthBreak = true)
    {
        while (true)
        {
            //Next Property
            if (!ss.NextVisible(true)) break;
            sd.NextVisible(true);

            //If end of module: break
            if (depthBreak && ss.depth == 0) break;

            var found = true;

            switch (ss.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    sd.boolValue = ss.boolValue;
                    break;
                case SerializedPropertyType.Integer:
                    sd.intValue = ss.intValue;
                    break;
                case SerializedPropertyType.Float:
                    sd.floatValue = ss.floatValue;
                    break;
                case SerializedPropertyType.Color:
                    sd.colorValue = ss.colorValue;
                    break;
                case SerializedPropertyType.Bounds:
                    sd.boundsValue = ss.boundsValue;
                    break;
                case SerializedPropertyType.Enum:
                    sd.enumValueIndex = ss.enumValueIndex;
                    break;
                case SerializedPropertyType.ObjectReference:
                    sd.objectReferenceValue = ss.objectReferenceValue;
                    break;
                case SerializedPropertyType.Rect:
                    sd.rectValue = ss.rectValue;
                    break;
                case SerializedPropertyType.String:
                    sd.stringValue = ss.stringValue;
                    break;
                case SerializedPropertyType.Vector2:
                    sd.vector2Value = ss.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    sd.vector3Value = ss.vector3Value;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    sd.animationCurveValue = ss.animationCurveValue;
                    break;
#if !UNITY_3_5
                case SerializedPropertyType.Gradient:
                    copyGradient(ss, sd);
                    break;
#endif

                default:
                    found = false;
                    break;
            }

            if (!found)
            {
                found = true;

                switch (ss.type)
                {
                    default:
                        found = false;
                        break;
                }
            }
        }

        //Apply Changes
        sd.serializedObject.ApplyModifiedProperties();

        ss.Dispose();
        sd.Dispose();
    }

#if !UNITY_3_5
    private void copyGradient(SerializedProperty ss, SerializedProperty sd)
    {
        var gradient = ss.Copy();
        var copyGrad = sd.Copy();
        gradient.Next(true);
        copyGrad.Next(true);
        do
        {
            switch (gradient.propertyType)
            {
                case SerializedPropertyType.Color:
                    copyGrad.colorValue = gradient.colorValue;
                    break;
                case SerializedPropertyType.Integer:
                    copyGrad.intValue = gradient.intValue;
                    break;
                default:
                    Debug.Log("CopyGradient: Unrecognized property type:" + gradient.propertyType);
                    break;
            }

            gradient.Next(true);
            copyGrad.Next(true);
        } while (gradient.depth > 2);
    }
#endif

    //Specific Copy for Sub Emitters Module (duplicate Sub Particle Systems)
    private void SubModuleCopy(SerializedObject source, SerializedObject dest)
    {
        dest.FindProperty("SubModule.enabled").boolValue = source.FindProperty("SubModule.enabled").boolValue;

        GameObject copy;
#if UNITY_5_5_OR_NEWER
        var arraySize = source.FindProperty("SubModule.subEmitters.Array.size").intValue;
        dest.FindProperty("SubModule.subEmitters.Array").ClearArray();
        for (var i = 0; i < arraySize; i++)
            if (source.FindProperty("SubModule.subEmitters.Array.data[" + i + "].emitter").objectReferenceValue != null)
            {
                copy = Instantiate(
                    (source.FindProperty("SubModule.subEmitters.Array.data[" + i + "].emitter")
                        .objectReferenceValue as ParticleSystem).gameObject);
                //Set as child of destination
                var localPos = copy.transform.localPosition;
                var localScale = copy.transform.localScale;
                var localAngles = copy.transform.localEulerAngles;
                copy.transform.parent = (dest.targetObject as ParticleSystem).transform;
                copy.transform.localPosition = localPos;
                copy.transform.localScale = localScale;
                copy.transform.localEulerAngles = localAngles;

                //Assign as sub Particle Emitter
                dest.FindProperty("SubModule.subEmitters.Array").InsertArrayElementAtIndex(i);
                dest.FindProperty("SubModule.subEmitters.Array.data[" + i + "].emitter").objectReferenceValue = copy;

                dest.FindProperty("SubModule.subEmitters.Array.data[" + i + "].type").intValue =
                    source.FindProperty("SubModule.subEmitters.Array.data[" + i + "].type").intValue;
                dest.FindProperty("SubModule.subEmitters.Array.data[" + i + "].properties").intValue =
                    source.FindProperty("SubModule.subEmitters.Array.data[" + i + "].properties").intValue;
            }
#else
		if(source.FindProperty("SubModule.subEmitterBirth").objectReferenceValue != null)
		{
			//Duplicate sub Particle Emitter
			copy =
 (GameObject)Instantiate((source.FindProperty("SubModule.subEmitterBirth").objectReferenceValue as ParticleSystem).gameObject);
			
			//Set as child of destination
			Vector3 localPos = copy.transform.localPosition;
			Vector3 localScale = copy.transform.localScale;
			Vector3 localAngles = copy.transform.localEulerAngles;
			copy.transform.parent = (dest.targetObject as ParticleSystem).transform;
			copy.transform.localPosition = localPos;
			copy.transform.localScale = localScale;
			copy.transform.localEulerAngles = localAngles;
			
			//Assign as sub Particle Emitter
			dest.FindProperty("SubModule.subEmitterBirth").objectReferenceValue = copy;
		}
		
		if(source.FindProperty("SubModule.subEmitterDeath").objectReferenceValue != null)
		{
			//Duplicate sub Particle Emitter
			copy =
 (GameObject)Instantiate((source.FindProperty("SubModule.subEmitterDeath").objectReferenceValue as ParticleSystem).gameObject);
			
			//Set as child of destination
			Vector3 localPos = copy.transform.localPosition;
			Vector3 localScale = copy.transform.localScale;
			Vector3 localAngles = copy.transform.localEulerAngles;
			copy.transform.parent = (dest.targetObject as ParticleSystem).transform;
			copy.transform.localPosition = localPos;
			copy.transform.localScale = localScale;
			copy.transform.localEulerAngles = localAngles;
			
			//Assign as sub Particle Emitter
			dest.FindProperty("SubModule.subEmitterDeath").objectReferenceValue = copy;
		}
		
		if(source.FindProperty("SubModule.subEmitterCollision").objectReferenceValue != null)
		{
			//Duplicate sub Particle Emitter
			copy =
 (GameObject)Instantiate((source.FindProperty("SubModule.subEmitterCollision").objectReferenceValue as ParticleSystem).gameObject);
			
			//Set as child of destination
			Vector3 localPos = copy.transform.localPosition;
			Vector3 localScale = copy.transform.localScale;
			Vector3 localAngles = copy.transform.localEulerAngles;
			copy.transform.parent = (dest.targetObject as ParticleSystem).transform;
			copy.transform.localPosition = localPos;
			copy.transform.localScale = localScale;
			copy.transform.localEulerAngles = localAngles;
			
			//Assign as sub Particle Emitter
			dest.FindProperty("SubModule.subEmitterCollision").objectReferenceValue = copy;
		}
#endif

        //Apply Changes
        dest.ApplyModifiedProperties();
    }

    //Scale System
    private void ScaleParticleValues(ParticleSystem ps, GameObject parent)
    {
        //Particle System
        if (ps.gameObject != parent)
            ps.transform.localPosition *= ScalingValue;

        var psSerial = new SerializedObject(ps);

        foreach (var path in PropertiesToScale)
        {
            var prop = psSerial.FindProperty(path);
            if (prop != null)
            {
                if (prop.propertyType == SerializedPropertyType.Float)
                    prop.floatValue *= ScalingValue;
                else
                    Debug.LogWarning("Property in ParticleSystem is not a float: " + path + "\n");
            }
            else
            {
                Debug.LogWarning("Property doesn't exist in ParticleSystem: " + path + "\n");
            }
        }

        //Shape Module special case
        if (psSerial.FindProperty("ShapeModule.enabled").boolValue)
            //(ShapeModule.type 6 == Mesh)
            if (psSerial.FindProperty("ShapeModule.type").intValue == 6)
            {
                //Unity 4+ : changing the Transform scale will affect the shape Mesh
                ps.transform.localScale = ps.transform.localScale * ScalingValue;
                EditorUtility.SetDirty(ps.transform);
            }

        //Apply Modified Properties
        psSerial.ApplyModifiedProperties();
    }

    private void GUISeparator()
    {
        GUILayout.Space(4);
        if (EditorGUIUtility.isProSkin)
        {
            GUILine(new Color(.15f, .15f, .15f), 1);
            GUILine(new Color(.4f, .4f, .4f), 1);
        }
        else
        {
            GUILine(new Color(.3f, .3f, .3f), 1);
            GUILine(new Color(.9f, .9f, .9f), 1);
        }

        GUILayout.Space(4);
    }

    public static void GUILine(Color color, float height = 2f)
    {
        var position = GUILayoutUtility.GetRect(0f, float.MaxValue, height, height, LineStyle);

        if (Event.current.type == EventType.Repaint)
        {
            var orgColor = GUI.color;
            GUI.color = orgColor * color;
            LineStyle.Draw(position, false, false, false, false);
            GUI.color = orgColor;
        }
    }

    private class ParticleSystemModule
    {
        public bool enabledInSource;
        public readonly string name;
        public bool selected;
        private readonly string serializedPropertyPath;

        public ParticleSystemModule(string name, string serializedPropertyPath)
        {
            this.name = " " + name + " ";
            this.serializedPropertyPath = serializedPropertyPath;
        }

        public void CheckIfEnabledInSource(SerializedObject serializedSource)
        {
            enabledInSource = true;

            var sp = serializedSource.FindProperty(serializedPropertyPath);
            if (sp != null)
            {
                var enabled = sp.FindPropertyRelative("enabled");
                if (enabled != null) enabledInSource = enabled.boolValue;
            }
        }
    }

    //RGB / HSL Conversions
    private struct HSLColor
    {
        public float h;
        public float s;
        public float l;
        public readonly float a;

        public HSLColor(float h, float s, float l, float a)
        {
            this.h = h;
            this.s = s;
            this.l = l;
            this.a = a;
        }

        public HSLColor(float h, float s, float l)
        {
            this.h = h;
            this.s = s;
            this.l = l;
            a = 1f;
        }

        public HSLColor(Color c)
        {
            var temp = FromRGBA(c);
            h = temp.h;
            s = temp.s;
            l = temp.l;
            a = temp.a;
        }

        public static HSLColor FromRGBA(Color c)
        {
            float h, s, l, a;
            a = c.a;

            var cmin = Mathf.Min(Mathf.Min(c.r, c.g), c.b);
            var cmax = Mathf.Max(Mathf.Max(c.r, c.g), c.b);

            l = (cmin + cmax) / 2f;

            if (cmin == cmax)
            {
                s = 0;
                h = 0;
            }
            else
            {
                var delta = cmax - cmin;

                s = l <= .5f ? delta / (cmax + cmin) : delta / (2f - (cmax + cmin));

                h = 0;

                if (c.r == cmax)
                    h = (c.g - c.b) / delta;
                else if (c.g == cmax)
                    h = 2f + (c.b - c.r) / delta;
                else if (c.b == cmax) h = 4f + (c.r - c.g) / delta;

                h = Mathf.Repeat(h * 60f, 360f);
            }

            return new HSLColor(h, s, l, a);
        }

        public Color ToRGBA()
        {
            float r, g, b, a;
            a = this.a;

            float m1, m2;

            m2 = l <= .5f ? l * (1f + s) : l + s - l * s;
            m1 = 2f * l - m2;

            if (s == 0f)
            {
                r = g = b = l;
            }
            else
            {
                r = Value(m1, m2, h + 120f);
                g = Value(m1, m2, h);
                b = Value(m1, m2, h - 120f);
            }

            return new Color(r, g, b, a);
        }

        private static float Value(float n1, float n2, float hue)
        {
            hue = Mathf.Repeat(hue, 360f);

            if (hue < 60f)
                return n1 + (n2 - n1) * hue / 60f;
            if (hue < 180f)
                return n2;
            if (hue < 240f)
                return n1 + (n2 - n1) * (240f - hue) / 60f;
            return n1;
        }

        public Color VividColor()
        {
            l = 0.5f;
            s = 1.0f;
            return ToRGBA();
        }

        public Color ColorWithHue(float hue, bool shift)
        {
            if (shift)
            {
                h += hue;
                while (h >= 360f)
                    h -= 360f;
                while (h <= -360f)
                    h += 360f;
            }
            else
            {
                h = hue;
            }

            return ToRGBA();
        }

        public Color ColorWithLightnessOffset(float lightness)
        {
            l += lightness;
            if (l > 1.0f) l = 1.0f;
            else if (l < 0.0f) l = 0.0f;

            return ToRGBA();
        }

        public static implicit operator HSLColor(Color src)
        {
            return FromRGBA(src);
        }

        public static implicit operator Color(HSLColor src)
        {
            return src.ToRGBA();
        }
    }
}