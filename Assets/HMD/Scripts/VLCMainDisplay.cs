﻿using UnityEngine;
using System;
using System.Threading.Tasks;
using LibVLCSharp;
//using NRKernal;
using System.Collections.Generic;
//using UnityEngine.Device;
using UnityEngine.UI;
using Application = UnityEngine.Device.Application;
using NRKernal;
using System.Collections;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using System.IO;

public class VLCMainDisplay : MonoBehaviour
{
    [SerializeField]
    public enum VideoMode
    {
        Mono,

        _3D, // Side-By-Side/SBS

        _3D_OU // Over-Under
        // TODO: there is no Mono_OU?
    }

    private float nextActionTime = 0.0f;
    public float period = 1.0f;

    [FormerlySerializedAs("jakesRemoteController")]
    public HeadDownController headDownController;


    private LibVLC _libVLC;

    //Create a new static LibVLC instance and dispose of the old one. You should only ever have one LibVLC instance.
    private void RefreshLibVLC()
    {
        Log("VLCPlayerExample CreateLibVLC");
        //Dispose of the old libVLC if necessary
        if (_libVLC != null)
        {
            _libVLC.Dispose();
            _libVLC = null;
        }

        Core.Initialize(Application.dataPath); //Load VLC dlls
        _libVLC = new LibVLC(
            true); //You can customize LibVLC with advanced CLI options here https://wiki.videolan.org/VLC_command-line_help/

        //Setup Error Logging
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        _libVLC.Log += (s, e) =>
        {
            //Always use try/catch in LibVLC events.
            //LibVLC can freeze Unity if an exception goes unhandled inside an event handler.
            try
            {
                if (logToConsole) Log(e.FormattedLog);
            }
            catch (Exception ex)
            {
                Log("Exception caught in libVLC.Log: \n" + ex.ToString());
            }
        };
    }

    private LibVLC libVLC
    {
        get
        {
            if (_libVLC == null) RefreshLibVLC();
            return _libVLC;
        }
        set => _libVLC = value;
    }

    private MediaPlayer _mediaPlayer;

    //Create a new MediaPlayer object and dispose of the old one. 
    private void RefreshMediaPlayer()
    {
        Log("VLCPlayerExample CreateMediaPlayer");
        if (_mediaPlayer != null) DestroyMediaPlayer();
        _mediaPlayer = new MediaPlayer(libVLC);
        Log("Media Player SET!");
    }

    public MediaPlayer mediaPlayer
    {
        get
        {
            if (_mediaPlayer == null) RefreshMediaPlayer();
            return _mediaPlayer;
        }
        set => _mediaPlayer = value;
    }

    private AndroidJavaClass _brightnessHelper;

    [SerializeField] private GameObject NRCameraRig; // TODO: remove
    [SerializeField] private GameObject XRRig;

    [SerializeField] private Camera LeftCamera;
    [SerializeField] private Camera CenterCamera;
    [SerializeField] private Camera RightCamera;

    [SerializeField] private Camera XRMainCamera;

    private List<Camera> MainCameras()
    {
        return new List<Camera> { CenterCamera, XRMainCamera };
    }

    private List<Camera> AllCameras()
    {
        var result = MainCameras();
        result.Add(LeftCamera);
        result.Add(RightCamera);

        return result;
    }

    private float leftCameraXOnStart;
    private float rightCameraXOnStart;

    [SerializeField]
    // public MyIAPHandler myIAPHandler;
    private GameObject _hideWhenLocked;

    private GameObject _lockScreenNotice;
    private GameObject _menuToggleButton;

    private GameObject _logo;

    [SerializeField] private GameObject mainDisplay;

    [SerializeField] private GameObject leftEyeScreen;
    [SerializeField] private GameObject rightEyeScreen;

    private List<GameObject> AllScreens()
    {
        return new List<GameObject> { leftEyeScreen, rightEyeScreen };
    }

    private Vector3 _startPosition;

    private Renderer _morphDisplayLeftRenderer;
    private Renderer _morphDisplayRightRenderer;

    [SerializeField] public Slider fovBar;

    // [SerializeField] public Slider scaleBar;

    [SerializeField] public Slider distanceBar;

    [SerializeField] public Slider deformBar;

    // [SerializeField] public Slider brightnessBar; // TODO: enable
    //
    // [SerializeField] public Slider contrastBar;
    //
    // [SerializeField] public Slider gammaBar;
    //
    // [SerializeField] public Slider saturationBar;
    //
    // [SerializeField] public Slider hueBar;
    //
    // [SerializeField] public Slider sharpnessBar;

    [SerializeField] public Slider horizontalBar;

    [SerializeField] public Slider verticalBar;

    [SerializeField] public Slider depthBar;

    [SerializeField] public Slider focusBar;

    private GameObject _cone;
    private GameObject _pointLight;

    private bool _screenLocked = false;
    private int _brightnessOnLock = 0;
    private int _brightnessModeOnLock = 0;

    private bool _flipStereo = false;

    [SerializeField] public VideoMode _videoMode = VideoMode.Mono; // 2d by default

    // // Flat Left
    // [SerializeField] public GameObject leftEye;
    //
    // // Flat Right
    // [SerializeField] public GameObject rightEye;
    //
    // // Sphere Left
    // [SerializeField] public GameObject leftEyeSphere;
    //
    // // Sphere Right
    // [SerializeField] public GameObject rightEyeSphere;

    private Renderer m_lRenderer;
    private Renderer m_rRenderer;

    private Renderer m_l360Renderer;
    private Renderer m_r360Renderer;

    public Material m_lMaterial;
    public Material m_rMaterial;
    public Material m_monoMaterial;
    public Material m_leftEyeTBMaterial;
    public Material m_rightEyeTBMaterial;

    // deprecated
    /*public Material m_leftEye360Material;
    public Material m_rightEye360Material;*/

    // deprecated
    // TODO: combine 180 and 360 into 2 materials instead of 4?
    /*public Material m_leftEye180Material;
    public Material m_rightEye180Material;*/

    // deprecated
    /*public Material m_3602DSphericalMaterial;
    public Material m_1802DSphericalMaterial;*/

    /// <summary> The NRInput. </summary>
    [SerializeField] private NRInput m_NRInput;

    private Texture2D _vlcTexture = null; //This is the texture libVLC writes to directly. It's private.
    public RenderTexture texture = null; //We copy it into this texture which we actually use in unity.

    private bool _is360 = false;
    private bool _is180 = false;

    private float Yaw;
    private float Pitch;
    private float Roll;

    private bool _3DModeLocked = true;

    // private int _3DTrialPlaybackStartedAt = 0;
    // private float _MaxTrialPlaybackSeconds = 15.0f;
    // private bool _isTrialing3DMode = false;

    private float _aspectRatio;
    private bool m_updatedARSinceOpen = false;
    private float _aspectRatioOverride;
    private string _currentARString;

    /// <summary> The previous position. </summary>
    private Vector2 m_PreviousPos;

    private float fov // 20 for 2D 140 for spherical
    {
        get => CenterCamera.fieldOfView;
        set
        {
            foreach (var c in AllCameras()) c.fieldOfView = value;
        }
    }

    //public string path = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"; //Can be a web path or a local path
    public string
        path = "https://jakedowns.com/media/sbs2.mp4"; // Render a nice lil SBS and 180 and 360 video that can play when you switch modes

    public bool flipTextureX = false; //No particular reason you'd need this but it is sometimes useful
    public bool flipTextureY = true; //Set to false on Android, to true on Windows

    public bool automaticallyFlipOnAndroid = true; //Automatically invert Y on Android

    public bool playOnAwake = true; //Open path and Play during Awake

    public bool logToConsole = true; //Log function calls and LibVLC logs to Unity console

    private AndroidJavaClass unityPlayer;
    private AndroidJavaObject activity;
    private AndroidJavaObject context;

    //Unity Awake, OnDestroy, and Update functions

    #region unity

    private void Awake()
    {
        //Setup Media Player
        RefreshMediaPlayer();

#if UNITY_ANDROID
        if (!Application.isEditor)
        {
            GetContext();



            _brightnessHelper = new AndroidJavaClass("com.jakedowns.BrightnessHelper");
            if (_brightnessHelper == null)
            {
                Debug.Log("error loading _brightnessHelper");
            }
        }
#endif

        Debug.Log($"[VLC] LibVLC version and architecture {libVLC.Changeset}");
        Debug.Log($"[VLC] LibVLCSharp version {typeof(LibVLC).Assembly.GetName().Version}");

        if (fovBar is not null) fovBar.value = fov;

        if (deformBar is not null) deformBar.value = 0.0f;

        _startPosition = new Vector3(
            mainDisplay.transform.position.x,
            mainDisplay.transform.position.y,
            mainDisplay.transform.position.z
        );

        headDownController.SetVLC(this);

        // UpdateCameraReferences();

        leftCameraXOnStart = LeftCamera.transform.position.x;
        rightCameraXOnStart = RightCamera.transform.position.x;

        // init
        OnFOVSliderUpdated();

        _cone = GameObject.Find("CONE_PARENT");
        _pointLight = GameObject.Find("Point Light");

        // TODO: extract lockscreen logic into a separate script
        _hideWhenLocked = GameObject.Find("HideWhenScreenLocked");
        _lockScreenNotice = GameObject.Find("LockScreenNotice");
        _logo = GameObject.Find("logo");
        _menuToggleButton = GameObject.Find("MenuToggleButton");

        //Setup Screen
        /*if (screen == null)
            screen = GetComponent<Renderer>();
        if (canvasScreen == null)
            canvasScreen = GetComponent<RawImage>();*/

        _morphDisplayLeftRenderer = leftEyeScreen.GetComponent<Renderer>();
        _morphDisplayRightRenderer = rightEyeScreen.GetComponent<Renderer>();

        //Automatically flip on android
        if (automaticallyFlipOnAndroid && UnityEngine.Application.platform == RuntimePlatform.Android)
            flipTextureY = !flipTextureY;

        if (UnityEngine.Application.platform != RuntimePlatform.Android)
            flipTextureX = !flipTextureX;

        SetVideoModeMono();

        //Play On Start
        if (playOnAwake)
            Open();
    }

    private void OnDestroy()
    {
        //Dispose of mediaPlayer, or it will stay in nemory and keep playing audio
        DestroyMediaPlayer();
    }

    private void UpdateColorGrade()
    {
        // Get the Color Grading effect from the camera's post-processing profile
        /*ColorGrading colorGrading;
        if (camera.TryGetComponent(out PostProcessVolume volume))
        {
            volume.profile.TryGetSettings(out colorGrading);
        }
        else
        {
            return;
        }*/

        // Set the brightness, contrast, and gamma levels
        /*colorGrading.brightness.value = brightnessBar.value;
        colorGrading.contrast.value = contrastBar.value;
        colorGrading.gamma.value = gammaBar.value;*/
    }

    private void Update()
    {
        if ((bool)mediaPlayer?.IsPlaying)
            if (UnityEngine.Time.time > nextActionTime)
                nextActionTime = UnityEngine.Time.time + period;

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1))
            UnityEditor.EditorWindow.focusedWindow.maximized = !UnityEditor.EditorWindow.focusedWindow.maximized;
#endif
        //Get size every frame
        uint height = 0;
        uint width = 0;
        mediaPlayer?.Size(0, ref width, ref height);

        //Automatically resize output textures if size changes
        if (_vlcTexture == null || _vlcTexture.width != width || _vlcTexture.height != height)
            ResizeOutputTextures(width, height);

        if (_vlcTexture != null)
        {
            //Update the vlc texture (tex)
            var texptr = mediaPlayer.GetTexture(width, height, out var updated);
            if (updated)
            {
                _vlcTexture.UpdateExternalTexture(texptr);

                //Copy the vlc texture into the output texture, flipped over
                var flip = new Vector2(flipTextureX ? -1 : 1, flipTextureY ? -1 : 1);
                Graphics.Blit(_vlcTexture, texture, flip,
                    Vector2.zero); //If you wanted to do post processing outside of VLC you could use a shader here.
            }
        }
    }

    #endregion

    private void OnDisable()
    {
        DestroyMediaPlayer();
    }

    private void OnApplicationQuit()
    {
        DestroyMediaPlayer();
    }

    // public void Demo3602D()
    // {
    //     //Open("https://streams.videolan.org/streams/360/eagle_360.mp4");
    //     Open("https://streams.videolan.org/streams/360/kolor-balloon-icare-full-hd.mp4");
    //     SetVideoMode3602D();
    // }

    private static Vector2 SCALE_RANGE = new(1f, 4.702173720867682f);

    public void OnDeformSliderUpdated()
    {
        // if (deformBar is null) return;

        var value = deformBar.value;

        var scale = Mathf.Lerp(SCALE_RANGE.x, SCALE_RANGE.y, value / 100);

        // Debug.Log("value set to " + value);

        var screens = AllScreens();
        foreach (var screen in screens)
            if (screen is not null)
            {
                screen.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(0, value);
                screen.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(1, value);
            }

        mainDisplay.transform.localScale = new Vector3(scale, scale, scale);
    }

    private float lerpDuration = 1; // TODO: dynamic duration based on startValue
    private float startValue = 0;
    private float endValue = 10;
    private IEnumerator lerpLZero;
    private IEnumerator lerpLOne;
    private IEnumerator lerpRZero;

    private IEnumerator lerpROne;
    //float valueToLerp;

    public void TogglePlaneToSphere() // TODO: cleanup
    {
        var current = leftEyeScreen.GetComponent<SkinnedMeshRenderer>().GetBlendShapeWeight(0);
        if (current < 50)
            AnimatePlaneToSphere();
        else
            AnimateSphereToPlane();
    }

    public void AnimatePlaneToSphere()
    {
        DoPlaneSphereLerp(100.0f);
    }

    public void AnimateSphereToPlane()
    {
        DoPlaneSphereLerp(0.0f);
    }

    public void DoPlaneSphereLerp(float _endValue)
    {
        if (lerpLZero is not null)
            StopCoroutine(lerpLZero);

        if (lerpLOne is not null)
            StopCoroutine(lerpLOne);

        if (lerpRZero is not null)
            StopCoroutine(lerpRZero);

        if (lerpROne is not null)
            StopCoroutine(lerpROne);

        endValue = _endValue;

        lerpLZero = LerpPlaneToSphere(leftEyeScreen.GetComponent<SkinnedMeshRenderer>(), 0);
        lerpLOne = LerpPlaneToSphere(leftEyeScreen.GetComponent<SkinnedMeshRenderer>(), 1);

        StartCoroutine(lerpLZero);
        StartCoroutine(lerpLOne);

        lerpRZero = LerpPlaneToSphere(rightEyeScreen.GetComponent<SkinnedMeshRenderer>(), 0);
        lerpROne = LerpPlaneToSphere(rightEyeScreen.GetComponent<SkinnedMeshRenderer>(), 1);

        StartCoroutine(lerpRZero);
        StartCoroutine(lerpROne);
    }

    private IEnumerator LerpPlaneToSphere(SkinnedMeshRenderer renderer, int ShapeIndex)
    {
        float timeElapsed = 0;
        startValue = renderer.GetBlendShapeWeight(ShapeIndex);
        while (timeElapsed < lerpDuration)
        {
            renderer.SetBlendShapeWeight(ShapeIndex, Mathf.Lerp(startValue, endValue, timeElapsed / lerpDuration));
            timeElapsed += UnityEngine.Time.deltaTime;
            yield return null;
        }

        renderer.SetBlendShapeWeight(ShapeIndex, endValue);
    }

    public void OnBrightnessSliderUpdated()
    {
    }

    public void OnGammaSliderUpdated()
    {
    }

    public void OnContrastSliderUpdated()
    {
    }

    public void OnDistanceSliderUpdated()
    {
        var newDistance = (float)distanceBar.value;
        mainDisplay.transform.localPosition = new Vector3(
            mainDisplay.transform.localPosition.x,
            mainDisplay.transform.localPosition.y,
            newDistance
        );
    }

    /* Horizontal (X) axis offset for screen */
    public void OnHorizontalSliderUpdated()
    {
        var newOffset = (float)horizontalBar.value;
        mainDisplay.transform.localPosition = new Vector3(
            newOffset,
            mainDisplay.transform.localPosition.y,
            mainDisplay.transform.localPosition.z
        );
    }

    /* Vertical (Y) axis offset for screen */
    public void OnVerticalSliderUpdated()
    {
        var newOffset = (float)verticalBar.value;
        mainDisplay.transform.localPosition = new Vector3(
            mainDisplay.transform.localPosition.x,
            newOffset,
            mainDisplay.transform.localPosition.z
        );
    }

    public void ResetDisplayAdjustments()
    {
        mainDisplay.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        mainDisplay.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private float leftCameraMinX = -1.5f;
    private float rightCameraMaxX = 1.5f;

    public void OnDepthBarUpdated()
    {
        var newDepth = (float)depthBar.value;

        // move left and right camera closer or further to each other depending on the depthbar value
        // if the value is 0, the cameras are the min distance apart from each other on their local x axis (leftCameraXOnStart / rightCameraXOnStart)
        // if the value is 100, the cameras are at the max distance apart from each other on their local x axis (leftCameraMinX / rightCameraMaxX)

        var leftCameraX = Mathf.Lerp(leftCameraXOnStart, leftCameraMinX, newDepth / 100.0f);
        var rightCameraX = Mathf.Lerp(rightCameraXOnStart, rightCameraMaxX, newDepth / 100.0f);

        Debug.Log($"{newDepth} , {leftCameraX} , {rightCameraX}");

        LeftCamera.transform.localPosition =
            new Vector3(leftCameraX, LeftCamera.transform.localPosition.y, LeftCamera.transform.localPosition.z);
        RightCamera.transform.localPosition =
            new Vector3(rightCameraX, RightCamera.transform.localPosition.y, RightCamera.transform.localPosition.z);
    }

    private static float maxFocal = 15.0f;
    private static float minFocal = -15.0f;

    public void OnFocusBarUpdated()
    {
        var focus = (float)focusBar.value; // percentage 0-100

        /* rotate the left and right camera ever so slightly so that the convergence plane / focus plane changes */
        var focal = Mathf.Lerp(minFocal, maxFocal, focus / 100.0f);
        LeftCamera.transform.localRotation = Quaternion.Euler(0.0f, focal, 0.0f);
        RightCamera.transform.localRotation = Quaternion.Euler(0.0f, -focal, 0.0f);
    }

    // public void UpdateCameraReferences()
    // {
    //     LeftCamera = GameObject.Find("LeftCamera")?.GetComponent<Camera>();
    //     CenterCamera = GameObject.Find("CenterCamera")?.GetComponent<Camera>();
    //     RightCamera = GameObject.Find("RightCamera")?.GetComponent<Camera>();
    // }

    public void OnFOVSliderUpdated()
    {
        // NOTE: NRSDK doesn't support custom FOV on cameras
        // NOTE: TESTING COMMENTING OUT camera.projectionMatrix = statements in NRHMDPoseTracker

        fov = (float)fovBar.value;
        Debug.Log("fov " + fov);

        Do360Navigation();
    }

    public void SetAR4_3()
    {
        if (mediaPlayer is not null)
            mediaPlayer.AspectRatio = "4:3";
    }

    public void SetAR169()
    {
        if (mediaPlayer is not null)
            mediaPlayer.AspectRatio = "16:9";
    }

    public void SetAR16_10()
    {
        if (mediaPlayer is not null)
            mediaPlayer.AspectRatio = "16:10";
    }

    public void SetAR_2_35_to_1()
    {
        if (mediaPlayer is not null)
            mediaPlayer.AspectRatio = "2.35:1";
    }

    public void SetARNull()
    {
        if (mediaPlayer is not null)
            mediaPlayer.AspectRatio = null;
    }

    private float _sphereScale;

    private void OnGUI()
    {
        if (!headDownController.OGMenuVisible()) return;
        if (NRInput.GetButtonDown(ControllerButton.TRIGGER))
        {
            m_PreviousPos = NRInput.GetTouch();
        }
        else if (NRInput.GetButton(ControllerButton.TRIGGER))
        {
            //UpdateScroll();
            Do360Navigation();
        }
        else if (NRInput.GetButtonUp(ControllerButton.TRIGGER))
        {
            //m_PreviousPos = Vector2.zero;
        }
    }

    private void Do360Navigation()
    {
        var range = Math.Max(Screen.width, Screen.height);

        Yaw = mediaPlayer.Viewpoint.Yaw;
        Pitch = mediaPlayer.Viewpoint.Pitch;
        Roll = mediaPlayer.Viewpoint.Roll;


        var deltaMove = NRInput.GetTouch() - m_PreviousPos;
        m_PreviousPos = NRInput.GetTouch();

        var absX = Mathf.Abs(deltaMove.x);
        var absY = Mathf.Abs(deltaMove.y);

        var eighty_or_delta_x = absX > 0 ? absX * 10000 : 80;
        var eighty_or_delta_y = absY > 0 ? absY * 10000 : 80;

        //Debug.Log($"*80x {eighty_or_delta_x} 80y {eighty_or_delta_y} fov {fov} fov2 {nreal_fov}");

        bool? result = null;
        try
        {
            if (Input.GetKey(KeyCode.RightArrow) || deltaMove.x > 0)
                result = mediaPlayer.UpdateViewpoint(Yaw + (float)(eighty_or_delta_x * +40 / range), Pitch, Roll, fov,
                    true);
            else if (Input.GetKey(KeyCode.LeftArrow) || deltaMove.x < 0)
                result = mediaPlayer.UpdateViewpoint(Yaw - (float)(eighty_or_delta_x * +40 / range), Pitch, Roll, fov,
                    true);
            else if (Input.GetKey(KeyCode.DownArrow) || deltaMove.y < 0)
                result = mediaPlayer.UpdateViewpoint(Yaw, Pitch + (float)(eighty_or_delta_y * +20 / range), Roll, fov,
                    true);
            else if (Input.GetKey(KeyCode.UpArrow) || deltaMove.y > 0)
                result = mediaPlayer.UpdateViewpoint(Yaw, Pitch - (float)(eighty_or_delta_y * +20 / range), Roll, fov,
                    true);
        }
        catch (Exception e)
        {
            Debug.LogWarning("error updating viewpoint " + e);
        }

        //Debug.Log("Update Viewpoint Result " + result.ToString());
    }

    //Public functions that expose VLC MediaPlayer functions in a Unity-friendly way. You may want to add more of these.

    #region vlc

    public void Open(string path)
    {
        Log("VLCPlayerExample Open " + path);
        if (path.ToLower().EndsWith(".url") || path.ToLower().EndsWith(".txt"))
        {
            var urlContent = File.ReadAllText(path);
            var lines = urlContent.Split('\n');

            var foundURL = false;
            foreach (var line in lines)
            {
                var _line = line.ToLower();
                if (_line.StartsWith("url="))
                {
                    this.path = _line.Substring(4).Trim();
                    foundURL = true;
                    break;
                }
            }

            if (foundURL == false) throw new Exception("No URL= line found in .url file");
        }
        else
        {
            this.path = path;
        }

        Open();
    }

    public void Open()
    {
        Log("VLCPlayerExample Open");

        if (mediaPlayer?.Media != null)
            mediaPlayer.Media.Dispose();

        SetVideoModeMono();

        var trimmedPath = path.Trim(new char[]
            { '"' }); //Windows likes to copy paths with quotes but Uri does not like to open them
        mediaPlayer.Media = new Media(new Uri(trimmedPath));

        Task.Run(async () =>
        {
            var result = await mediaPlayer.Media.ParseAsync(libVLC, MediaParseOptions.ParseNetwork);
            var trackList = mediaPlayer.Media.TrackList(TrackType.Video);
            _is360 = trackList[0].Data.Video.Projection == VideoProjection.Equirectangular;

            Debug.Log($"projection {trackList[0].Data.Video.Projection}");

            // TODO: add SBS / OU / TB filename recognition

            // if (_is360)
            // {
            //     Debug.Log("The video is a 360 video");
            //     SetVideoMode(VideoMode._360_2D);
            // }
            //
            // else
            // {
            //     Debug.Log("The video was not identified as a 360 video by VLC");
            // SetVideoMode(VideoMode.Mono);
            // }

            trackList.Dispose();
        });

        // flag to read and store the texture aspect ratio
        m_updatedARSinceOpen = false;

        Play();

        // TODO: If removed or set the delay too short, will cause the VLC screen to blackout.
        //  How did this happen?
        //  Is it because the texture is not ready yet?
        StartCoroutine(SetVideoModeDelayed(3));
    }

    private IEnumerator SetVideoModeDelayed(int secs)
    {
        Debug.Log("[JakeDowns] SetVideoModeDelayed " + secs);
        yield return new WaitForSeconds(secs);
        SetVideoModeMono();
    }

    // public void OpenExternal()
    // {
    //     // TODO: Prompt user for path
    // }

    public void Play()
    {
        Log("VLCPlayerExample Play");

        _cone?.SetActive(false); // hide cone logo
        _pointLight?.SetActive(false);

        mainDisplay?.SetActive(true);

        mediaPlayer.Play();
    }

    public void Pause()
    {
        Log("VLCPlayerExample Pause");
        mediaPlayer.Pause();
    }

    public void Stop()
    {
        Log("VLCPlayerExample Stop");
        mediaPlayer?.Stop();

        mainDisplay.SetActive(false);

        // TODO: encapsulate this
        if (m_lRenderer?.material is not null)
            m_lRenderer.material.mainTexture = null;

        if (m_rRenderer?.material is not null)
            m_rRenderer.material.mainTexture = null;

        if (m_l360Renderer?.material is not null)
            m_l360Renderer.material.mainTexture = null;

        if (m_r360Renderer?.material is not null)
            m_r360Renderer.material.mainTexture = null;


        // show cone
        _cone?.SetActive(true);
        _pointLight?.SetActive(true);


        // clear to black
        _vlcTexture = null;
        texture = null;
    }

    public void SeekForward10()
    {
        SeekSeconds((float)10);
    }

    public void SeekBack10()
    {
        SeekSeconds((float)-10);
    }

    public void SeekSeconds(float seconds)
    {
        Seek((long)seconds * 1000);
    }

    public void Seek(long timeDelta)
    {
        Debug.Log("VLCPlayerExample Seek " + timeDelta);
        mediaPlayer.SetTime(mediaPlayer.Time + timeDelta);
    }

    public void SetTime(long time)
    {
        Log("VLCPlayerExample SetTime " + time);
        mediaPlayer.SetTime(time);
    }

    public void SetVolume(int volume = 100)
    {
        Log("VLCPlayerExample SetVolume " + volume);
        mediaPlayer.SetVolume(volume);
    }

    public int Volume
    {
        get
        {
            if (mediaPlayer == null)
                return 0;
            return mediaPlayer.Volume;
        }
    }

    public bool IsPlaying
    {
        get
        {
            if (mediaPlayer == null)
                return false;
            return mediaPlayer.IsPlaying;
        }
    }

    public long Duration
    {
        get
        {
            if (mediaPlayer == null || mediaPlayer.Media == null)
                return 0;
            return mediaPlayer.Media.Duration;
        }
    }

    public long Time
    {
        get
        {
            if (mediaPlayer == null)
                return 0;
            return mediaPlayer.Time;
        }
    }

    public List<MediaTrack> Tracks(TrackType type)
    {
        Log("VLCPlayerExample Tracks " + type);
        return ConvertMediaTrackList(mediaPlayer?.Tracks(type));
    }

    public MediaTrack SelectedTrack(TrackType type)
    {
        Log("VLCPlayerExample SelectedTrack " + type);
        return mediaPlayer?.SelectedTrack(type);
    }

    public void Select(MediaTrack track)
    {
        Log("VLCPlayerExample Select " + track.Name);
        mediaPlayer?.Select(track);
    }

    public void Unselect(TrackType type)
    {
        Log("VLCPlayerExample Unselect " + type);
        mediaPlayer?.Unselect(type);
    }

    //This returns the video orientation for the currently playing video, if there is one
    public VideoOrientation? GetVideoOrientation()
    {
        var tracks = mediaPlayer?.Tracks(TrackType.Video);

        if (tracks == null || tracks.Count == 0)
            return null;

        var orientation =
            tracks[0]?.Data.Video.Orientation; //At the moment we're assuming the track we're playing is the first track

        return orientation;
    }

    #endregion

    //Private functions create and destroy VLC objects and textures

    #region internal

    //Dispose of the MediaPlayer object. 
    private void DestroyMediaPlayer()
    {
        if (m_lRenderer?.material is not null)
            m_lRenderer.material.mainTexture = null;

        if (m_rRenderer?.material is not null)
            m_rRenderer.material.mainTexture = null;

        if (m_l360Renderer is not null && m_l360Renderer?.material is not null)
            m_l360Renderer.material.mainTexture = null;

        if (m_r360Renderer is not null && m_r360Renderer?.material is not null)
            m_r360Renderer.material.mainTexture = null;

        _vlcTexture = null;

        mediaPlayer?.Stop();
        mediaPlayer?.Dispose();
        mediaPlayer = null;

        libVLC?.Dispose();
        libVLC = null;

        Log("DestroyMediaPlayer");
        mediaPlayer?.Stop();
        mediaPlayer?.Dispose();
        mediaPlayer = null;
    }

    //Resize the output textures to the size of the video
    private void ResizeOutputTextures(uint px, uint py)
    {
        var texptr = mediaPlayer.GetTexture(px, py, out var updated);
        if (px != 0 && py != 0 && updated && texptr != IntPtr.Zero)
        {
            //If the currently playing video uses the Bottom Right orientation, we have to do this to avoid stretching it.
            if (GetVideoOrientation() == VideoOrientation.BottomRight)
            {
                var swap = px;
                px = py;
                py = swap;
            }

            _vlcTexture =
                Texture2D.CreateExternalTexture((int)px, (int)py, TextureFormat.RGBA32, false, true,
                    texptr); //Make a texture of the proper size for the video to output to
            texture = new RenderTexture(_vlcTexture.width, _vlcTexture.height, 0,
                RenderTextureFormat.ARGB32); //Make a renderTexture the same size as vlctex

            Debug.Log($"texture size {px} {py} | {_vlcTexture.width} {_vlcTexture.height}");

            if (!m_updatedARSinceOpen)
            {
                m_updatedARSinceOpen = true;
                _aspectRatio = (float)texture.width / (float)texture.height;
                Debug.Log($"[SBSVLC] aspect ratio {_aspectRatio}");
                _currentARString = $"{texture.width}:{texture.height}";
                mediaPlayer.AspectRatio = _currentARString;
            }

            if (m_lRenderer != null)
                m_lRenderer.material.mainTexture = texture;

            if (m_rRenderer != null)
                m_rRenderer.material.mainTexture = texture;

            /*if (m_l360Renderer != null)
                m_l360Renderer.material.mainTexture = texture;

            if (m_r360Renderer != null)
                m_r360Renderer.material.mainTexture = texture;*/
        }
    }

    //Converts MediaTrackList objects to Unity-friendly generic lists. Might not be worth the trouble.
    private List<MediaTrack> ConvertMediaTrackList(MediaTrackList tracklist)
    {
        if (tracklist == null)
            return new List<MediaTrack>(); //Return an empty list

        var tracks = new List<MediaTrack>((int)tracklist.Count);
        for (uint i = 0; i < tracklist.Count; i++) tracks.Add(tracklist[i]);
        return tracks;
    }

    public void ToggleFlipStereo()
    {
        _flipStereo = !_flipStereo;
        SetVideoMode(_videoMode);
    }

    public string GetCurrentAR()
    {
        return _currentARString;
    }

    public void SetCurrentAR(string currentARString)
    {
        _currentARString = currentARString;
        mediaPlayer.AspectRatio = _currentARString;

        var split = _currentARString.Split(':');
        var ar_width = float.Parse(split[0]);
        var ar_height = float.Parse(split[1]);
        var ar_float = ar_width / ar_height;

        if (m_lMaterial is not null)
            m_lMaterial.SetFloat("AspectRatio", ar_float);

        // todo: make a combined shader?
        if (m_rMaterial is not null)
            m_rMaterial.SetFloat("AspectRatio", ar_float);
    }

    public void ClearMaterialTextureLinks()
    {
        if (_morphDisplayLeftRenderer.material is not null)
        {
            _morphDisplayLeftRenderer.material.mainTexture = null;
            _morphDisplayLeftRenderer.material = null;
        }

        if (_morphDisplayRightRenderer.material is not null)
        {
            _morphDisplayRightRenderer.material.mainTexture = null;
            _morphDisplayRightRenderer.material = null;
        }
    }

    public void SetVideoMode(VideoMode mode)
    {
        _videoMode = mode;
        Debug.Log($"[JakeDowns] set video mode {mode}");

        //flipTextureX = false;

        ClearMaterialTextureLinks();

        if (texture == null) Debug.LogWarning("[SetVideoMode] texture is null!");

        if (mode == VideoMode.Mono)
        {
            // 2D
            leftEyeScreen.layer = LayerMask.NameToLayer("Default");
            rightEyeScreen.SetActive(false);

            _morphDisplayLeftRenderer.material = m_monoMaterial; // m_lMaterial;
            _morphDisplayLeftRenderer.material.mainTexture = texture;
        }
        else
        {
            // 3D

            leftEyeScreen.layer = LayerMask.NameToLayer("LeftEyeOnly");

            rightEyeScreen.SetActive(true);
            rightEyeScreen.layer = LayerMask.NameToLayer("RightEyeOnly");

            if (mode is VideoMode._3D_OU)
            {
                _morphDisplayLeftRenderer.material = _flipStereo ? m_rightEyeTBMaterial : m_leftEyeTBMaterial;
                _morphDisplayRightRenderer.material = _flipStereo ? m_leftEyeTBMaterial : m_rightEyeTBMaterial;
            }
            else
            {
                _morphDisplayLeftRenderer.material = _flipStereo ? m_rMaterial : m_lMaterial;
                _morphDisplayRightRenderer.material = _flipStereo ? m_lMaterial : m_rMaterial;
            }

            _morphDisplayLeftRenderer.material.mainTexture = texture;
            _morphDisplayRightRenderer.material.mainTexture = texture;
        }
    }

    public void SetAspectRatio(string value)
    {
        mediaPlayer.AspectRatio = value;
    }

    // https://answers.unity.com/questions/1549639/enum-as-a-function-param-in-a-button-onclick.html?page=2&pageSize=5&sort=votes

    public void SetVideoModeMono()
    {
        SetVideoMode(VideoMode.Mono);
    }

    public void SetVideoMode3D()
    {
        SetVideoMode(VideoMode._3D);
    }

    public void SetVideoModeOU()
    {
        SetVideoMode(VideoMode._3D_OU);
    }

    public void ResetScreen() // TODO: bind it to button
    {
        leftEyeScreen.transform.localPosition = _startPosition;
        leftEyeScreen.transform.localRotation = Quaternion.identity;
        leftEyeScreen.transform.localScale = new Vector3(1, 1, 1);

        rightEyeScreen.transform.localPosition = _startPosition;
        rightEyeScreen.transform.localRotation = Quaternion.identity;
        rightEyeScreen.transform.localScale = new Vector3(1, 1, 1);
    }

    public void promptUserFilePicker()
    {
#if UNITY_ANDROID
        // Use MIMEs on Android
        string[] fileTypes = new string[] { "video/*" };
#else
        // Use UTIs on iOS or Windows
        var fileTypes = new string[] { "video/*", "video/movie", "text/url", "text/txt", "*" };
#endif

        // Pick image(s) and/or video(s)
        var permission = NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                Debug.Log("Operation cancelled");
            }
            else
            {
                Debug.Log("Picked file: " + path);
                Open(path);
            }
        }, fileTypes);

        if (permission is not NativeFilePicker.Permission.Granted)
        {
            _ShowAndroidToastMessage($"Permission result: {permission}");
            Debug.Log("Permission result: " + permission);
        }
    }

    /// <param name="message">Message string to show in the toast.</param>
    private void _ShowAndroidToastMessage(string message)
    {
        var unityPlayer = new AndroidJavaClass("com.jakedowns.VLC3D.VLC3DActivity");
        var unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        if (unityActivity != null)
        {
            var toastClass = new AndroidJavaClass("android.widget.Toast");
            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                var toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity, message, 0);
                toastObject.Call("show");
            }));
        }
    }

    public void OnSingleTap(string name)
    {
        Debug.Log($"[SBSVLC] Single Tap Triggered {name}");
        if (name == "LockScreenButton")
            if (!_screenLocked)
                ToggleScreenLock();
    }

    // we require a double-tap to unlock
    public void OnDoubleTap(string name)
    {
        Debug.Log($"[SBSVLC] Double Tap Triggered {name}");
        if (name == "LockScreenButton")
            if (_screenLocked)
                ToggleScreenLock();
    }

    private void GetContext()
    {
        unityPlayer = new AndroidJavaClass("com.jakedowns.VLC3D.VLC3DActivity");
        try
        {
            activity = unityPlayer?.GetStatic<AndroidJavaObject>("currentActivity");
            context = activity?.Call<AndroidJavaObject>("getApplicationContext");
        }
        catch (Exception e)
        {
            Debug.Log("error getting context " + e.ToString());
        }
    }

    public void ToggleScreenLock()
    {
        _screenLocked = !_screenLocked;

        if (_screenLocked)
        {
            // Hide All UI except for the lock button
            _hideWhenLocked.SetActive(false);
            _lockScreenNotice.SetActive(true);
            _logo.SetActive(false);
            _menuToggleButton.SetActive(false);
            // Lower Brightness
            var _unityBrightnessOnLock = Screen.brightness;
            Debug.Log($"lockbrightness Unity brightness on lock {_unityBrightnessOnLock}");

#if UNITY_ANDROID
            if (!Application.isEditor)
            {
                try
                {
                    // get int from _brightnessHelper
                    _brightnessOnLock = (int)(_brightnessHelper?.CallStatic<int>("getBrightness"));

                    _brightnessModeOnLock = (int)(_brightnessHelper?.CallStatic<int>("getBrightnessMode"));

                    Debug.Log($"lockbrightness Android brightness on lock {_brightnessOnLock}");
                }catch(Exception e)
                {
                    Debug.Log("Error getting brightness " + e.ToString());
                }

                // Set it to 0? 0.1?
                //Debug.Log($"set brightness with unity");
                //Screen.brightness = 0.1f;

                if (context is null)
                {
                    Debug.Log("context is null");
                    GetContext();
                }
                if (context is not null)
                {
                    // TODO: maybe try to fetch it again now?

                    object _args = new object[2] { context, 1 };

                    // call _brightnessHelper
                    _brightnessHelper?.CallStatic("SetBrightness", _args);
                }
                 

            }
#endif
        }
        else
        {
#if UNITY_ANDROID
            if (!Application.isEditor)
            {
                if (context is null)
                {
                    Debug.Log("context is null");
                    GetContext();
                }
                if (context is not null)
                {
                    try
                    {
                        object _args = new object[2] { context, _brightnessOnLock };
                        _brightnessHelper?.CallStatic("setBrightness", _args);

                        // restore brightness mode
                        object _args_mode = new object[2] { context, _brightnessModeOnLock };
                        _brightnessHelper?.CallStatic("setBrightnessMode", _args_mode);
                    }
                    catch(Exception e)
                    {
                        Debug.Log("error setting brightness " + e.ToString());
                    }
                    
                }
                
            }
#else
            // Restore Brightness
            Screen.brightness = _brightnessOnLock;
#endif

            // Show All UI when screen is unlocked
            _hideWhenLocked.SetActive(true);
            _lockScreenNotice.SetActive(false);
            _logo.SetActive(true);
            _menuToggleButton.SetActive(true);
        }
    }

    private void Log(object message)
    {
        if (logToConsole)
            Debug.Log($"[VLC] {message}");
    }

    #endregion
}