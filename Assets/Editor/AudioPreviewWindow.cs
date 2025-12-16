using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;

public class AudioPreviewWindow : EditorWindow
{
    #region Fields

    private AudioClip audioClip;
    private AudioClip[] allAudioClips;
    private int currentIndex;
    private bool isLooping;

    private bool showTrimSection;
    private float trimStart;
    private float trimEnd;

    private static Type audioUtilType;
    private static MethodInfo playClipMethod;
    private static MethodInfo stopClipMethod;
    private static MethodInfo isClipPlayingMethod;
    private static MethodInfo getClipPositionMethod;
    private static MethodInfo setClipSamplePositionMethod;

    #endregion

    #region Properties

    private bool HasAudioClips => allAudioClips != null && allAudioClips.Length > 0;
    private bool CanGoPrev => HasAudioClips && currentIndex > 0;
    private bool CanGoNext => HasAudioClips && currentIndex < allAudioClips.Length - 1;

    #endregion

    #region Lifecycle

    [MenuItem("Window/Audio Preview")]
    public static void ShowWindow()
    {
        var window = GetWindow<AudioPreviewWindow>("Audio Preview");
        window.minSize = new Vector2(450, 400);
    }

    private void OnEnable()
    {
        InitializeAudioUtil();
        LoadAllAudioClips();
        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        StopClip();
        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnSelectionChanged()
    {
        if (Selection.activeObject is AudioClip clip)
        {
            SetAudioClip(clip);
            Repaint();
        }
    }

    private void OnEditorUpdate()
    {
        if (audioClip == null || !IsPlaying()) return;

        if (isLooping && GetClipPosition() >= audioClip.length - 0.05f)
        {
            SetClipSamplePosition(0);
        }
        Repaint();
    }

    #endregion

    #region Core Logic

    private void LoadAllAudioClips()
    {
        var guids = AssetDatabase.FindAssets("t:AudioClip");
        allAudioClips = new AudioClip[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            allAudioClips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        UpdateCurrentIndex();
    }

    private void SetAudioClip(AudioClip clip)
    {
        StopClip();
        audioClip = clip;
        UpdateCurrentIndex();
        ResetTrimRange();
    }

    private void ChangeClipByIndex(int newIndex)
    {
        if (!HasAudioClips) return;
        currentIndex = Mathf.Clamp(newIndex, 0, allAudioClips.Length - 1);
        SetAudioClip(allAudioClips[currentIndex]);
    }

    private void UpdateCurrentIndex()
    {
        if (audioClip == null || !HasAudioClips)
        {
            currentIndex = 0;
            return;
        }
        currentIndex = Mathf.Max(0, Array.IndexOf(allAudioClips, audioClip));
    }

    private void ResetTrimRange()
    {
        trimStart = 0f;
        trimEnd = audioClip != null ? audioClip.length : 0f;
    }

    #endregion

    #region GUI

    private void OnGUI()
    {
        EditorGUILayout.Space(15);
        DrawClipSelector();

        if (audioClip == null)
        {
            EditorGUILayout.HelpBox("AudioClip을 선택하세요.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(5);
        DrawPlaybackControls();
        EditorGUILayout.Space(5);
        DrawTrimSection();
    }

    private void DrawClipSelector()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = CanGoPrev;
        if (GUILayout.Button("◀", GUILayout.Width(25)))
            ChangeClipByIndex(currentIndex - 1);

        GUI.enabled = true;
        EditorGUI.BeginChangeCheck();
        var newClip = (AudioClip)EditorGUILayout.ObjectField(audioClip, typeof(AudioClip), false);
        if (EditorGUI.EndChangeCheck())
            SetAudioClip(newClip);

        GUI.enabled = CanGoNext;
        if (GUILayout.Button("▶", GUILayout.Width(25)))
            ChangeClipByIndex(currentIndex + 1);

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (HasAudioClips)
            EditorGUILayout.LabelField($"{currentIndex + 1} / {allAudioClips.Length}", EditorStyles.centeredGreyMiniLabel);
    }

    private void DrawPlaybackControls()
    {
        float currentPos = IsPlaying() ? GetClipPosition() : 0f;

        EditorGUI.BeginChangeCheck();
        float newPos = EditorGUILayout.Slider(currentPos, 0f, audioClip.length);
        if (EditorGUI.EndChangeCheck())
            SetClipSamplePosition(TimeToSample(newPos));

        EditorGUILayout.LabelField($"{FormatTime(currentPos)} / {FormatTime(audioClip.length)}", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.BeginHorizontal();
        isLooping = GUILayout.Toggle(isLooping, "Loop", GUILayout.Width(45));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button(IsPlaying() ? "Stop" : "Play", GUILayout.Height(28)))
        {
            if (IsPlaying()) StopClip();
            else PlayClip();
        }
    }

    private void DrawTrimSection()
    {
        showTrimSection = EditorGUILayout.Foldout(showTrimSection, "Trim & Save", true);
        if (!showTrimSection) return;

        if (trimEnd <= 0f) trimEnd = audioClip.length;

        DrawTrimSlider("Start", ref trimStart, () => trimStart = IsPlaying() ? GetClipPosition() : 0f);
        DrawTrimSlider("End", ref trimEnd, () => trimEnd = IsPlaying() ? GetClipPosition() : audioClip.length);

        if (trimStart >= trimEnd)
            trimStart = Mathf.Max(0, trimEnd - 0.1f);

        float duration = trimEnd - trimStart;
        EditorGUILayout.LabelField($"Selection: {FormatTime(trimStart)} - {FormatTime(trimEnd)} ({duration:F2}s)", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.Space(3);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview", GUILayout.Height(24)))
            PlayTrimmedPreview();
        if (GUILayout.Button("Save WAV", GUILayout.Height(24)))
            SaveTrimmedWav();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTrimSlider(string label, ref float value, Action onSetClick)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(35));
        value = EditorGUILayout.Slider(value, 0f, audioClip.length);
        if (GUILayout.Button("Set", GUILayout.Width(35)))
            onSetClick?.Invoke();
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Trim & Save

    private void PlayTrimmedPreview()
    {
        if (audioClip == null) return;
        StopClip();
        playClipMethod?.Invoke(null, new object[] { audioClip, TimeToSample(trimStart), false });
    }

    private void SaveTrimmedWav()
    {
        if (audioClip == null) return;

        var path = EditorUtility.SaveFilePanel("Save Trimmed Audio", "Assets", $"{audioClip.name}_trimmed", "wav");
        if (string.IsNullOrEmpty(path)) return;

        int channels = audioClip.channels;
        int frequency = audioClip.frequency;
        int startSample = (int)(trimStart * frequency);
        int endSample = (int)(trimEnd * frequency);
        int sampleCount = (endSample - startSample) * channels;

        var allSamples = new float[audioClip.samples * channels];
        audioClip.GetData(allSamples, 0);

        var trimmedSamples = new float[sampleCount];
        Array.Copy(allSamples, startSample * channels, trimmedSamples, 0, sampleCount);

        WriteWavFile(path, trimmedSamples, channels, frequency);

        if (path.StartsWith(Application.dataPath))
            AssetDatabase.Refresh();

        Debug.Log($"오디오 저장 완료: {path}");
    }

    private void WriteWavFile(string path, float[] samples, int channels, int frequency)
    {
        using (var stream = new FileStream(path, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            int sampleCount = samples.Length;
            int byteRate = frequency * channels * 2;

            // RIFF header
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + sampleCount * 2);
            writer.Write("WAVE".ToCharArray());

            // fmt chunk
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(frequency);
            writer.Write(byteRate);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);

            // data chunk
            writer.Write("data".ToCharArray());
            writer.Write(sampleCount * 2);

            foreach (float sample in samples)
                writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
        }
    }

    #endregion

    #region AudioUtil Wrapper

    private static void InitializeAudioUtil()
    {
        if (audioUtilType != null) return;

        var assembly = typeof(AudioImporter).Assembly;
        audioUtilType = assembly.GetType("UnityEditor.AudioUtil");
        var flags = BindingFlags.Static | BindingFlags.Public;

        playClipMethod = audioUtilType.GetMethod("PlayPreviewClip", flags, null,
            new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        stopClipMethod = audioUtilType.GetMethod("StopAllPreviewClips", flags);
        isClipPlayingMethod = audioUtilType.GetMethod("IsPreviewClipPlaying", flags);
        getClipPositionMethod = audioUtilType.GetMethod("GetPreviewClipPosition", flags);
        setClipSamplePositionMethod = audioUtilType.GetMethod("SetPreviewClipSamplePosition", flags, null,
            new[] { typeof(AudioClip), typeof(int) }, null);
    }

    private void PlayClip()
    {
        if (audioClip == null || playClipMethod == null) return;
        StopClip();
        playClipMethod.Invoke(null, new object[] { audioClip, 0, isLooping });
    }

    private void StopClip() => stopClipMethod?.Invoke(null, null);

    private bool IsPlaying() => isClipPlayingMethod != null && (bool)isClipPlayingMethod.Invoke(null, null);

    private float GetClipPosition() => getClipPositionMethod != null && audioClip != null
        ? (float)getClipPositionMethod.Invoke(null, null) : 0f;

    private void SetClipSamplePosition(int sample)
    {
        if (setClipSamplePositionMethod != null && audioClip != null)
            setClipSamplePositionMethod.Invoke(null, new object[] { audioClip, sample });
    }

    #endregion

    #region Utility

    private int TimeToSample(float time) => (int)(time / audioClip.length * audioClip.samples);

    private string FormatTime(float seconds) => $"{(int)(seconds / 60):D2}:{(int)(seconds % 60):D2}";

    #endregion
}
