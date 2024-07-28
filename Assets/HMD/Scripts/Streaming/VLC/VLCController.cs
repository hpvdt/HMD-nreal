namespace HMD.Scripts.Streaming.VLC
{
    using System;
    using System.Collections.Generic;
    using Util;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.Serialization;
    using UnityEngine.UI;

    
    ///This script controls all the GUI for the VLC Unity Canvas Example
    ///It sets up event handlers and updates the GUI every frame
    ///This example shows how to safely set up LibVLC events and a simple way to call Unity functions from them
    public class VlcController : MonoBehaviour
    {
        public VlcDisplay mainDisplay;
        public DashPanel dashPanel;

        //GUI Elements
        //public RawImage screen;
        //public AspectRatioFitter screenAspectRatioFitter;
        public Button rewind10Button;
        public Button ffw10Button;

        public Slider seekBar;

        // public Slider scaleBar;
        public Button playButton;
        public Button pauseButton;
        public Button stopButton;
        public Button fileButton;

        // public Button tracksButton; // TODO: use this group for other things
        
        public Button consoleButton;
        public GameObject consoleGroup;
        public InputField pathInputField; // TODO: this won't be on the dashUI, will be moved to HUD
        public Button pathEnterButton;
        
        public Button volumeButton;
        public GameObject volumeGroup;
        public Slider volumeBar;
        
        public Text currentTimecode;

        public Slider aspectRatioSlider;
        public GameObject aspectRatioText;

        private bool _isDraggingARWidthBar = false;
        private bool _isDraggingARHeightBar = false;
        private bool _isDraggingaspectRatioSlider = false;

        //Configurable Options
        public int maxVolume = 100; //The highest volume the slider can reach. 100 is usually good but you can go higher.

        //State variables
        private bool
            _isPlaying = false; //We use VLC events to track whether we are playing, rather than relying on IsPlaying 

        private bool _isDraggingSeekBar = false; //We advance the seek bar every frame, unless the user is dragging it

        // private bool _isDraggingScaleBar = false;

        ///Unity wants to do everything on the main thread, but VLC events use their own thread.
        ///These variables can be set to true in a VLC event handler indicate that a function should be called next Update.
        ///This is not actually thread safe and should be gone soon!
        private bool _shouldUpdateTracks = false; //Set this to true and the Tracks menu will regenerate next frame

        private bool _shouldClearTracks = false; //Set this to true and the Tracks menu will clear next frame

        private List<Button> _videoTracksButtons = new List<Button>();
        private List<Button> _audioTracksButtons = new List<Button>();
        private List<Button> _textTracksButtons = new List<Button>();

        private void Start()
        {
            mainDisplay.controller = this;
            dashPanel.controller = this;

            LinkDisplay();

            mainDisplay.Stop();
        }

        private void LinkDisplay()
        {
            if (mainDisplay?.vlcFeed.Player is null)
            {
                Debug.LogError("VLC Player mediaPlayer not found");
                return;
            }

            //VLC Event Handlers
            mainDisplay.vlcFeed.Player.Playing += (object sender, EventArgs e) =>
            {
                //Always use Try/Catch for VLC Events
                try
                {
                    //Because many Unity functions can only be used on the main thread, they will fail in VLC event handlers
                    //A simple way around this is to set flag variables which cause functions to be called on the next Update
                    _isPlaying = true; //Switch to the Pause button next update
                    _shouldUpdateTracks = true; //Regenerate tracks next update
                }
                catch (Exception ex)
                {
                    Debug.LogError("Exception caught in mediaPlayer.Play: \n" + ex);
                }
            };

            mainDisplay.vlcFeed.Player.Paused += (object sender, EventArgs e) =>
            {
                //Always use Try/Catch for VLC Events
                try
                {
                    _isPlaying = false; //Switch to the Play button next update
                }
                catch (Exception ex)
                {
                    Debug.LogError("Exception caught in mediaPlayer.Paused: \n" + ex);
                }
            };

            mainDisplay.vlcFeed.Player.Stopped += (object sender, EventArgs e) =>
            {
                //Always use Try/Catch for VLC Events
                try
                {
                    _isPlaying = false; //Switch to the Play button next update
                    _shouldClearTracks = true; //Clear tracks next update
                }
                catch (Exception ex)
                {
                    Debug.LogError("Exception caught in mediaPlayer.Stopped: \n" + ex);
                }
            };

            //Buttons
            rewind10Button.onClick.AddListener(() =>
            {
                Debug.Log("Rewind10Button");
                mainDisplay.vlcFeed.SeekBack10();
            });
            ffw10Button.onClick.AddListener(() =>
            {
                Debug.Log("FFW10Button");
                mainDisplay.vlcFeed.SeekForward10();
            });
            pauseButton.onClick.AddListener(() => { mainDisplay.Pause(); });
            playButton.onClick.AddListener(() => { mainDisplay.Play(); });
            stopButton.onClick.AddListener(() => { mainDisplay.Stop(); });
            
            consoleButton.onClick.AddListener(() =>
            {
                if (ToggleElement(consoleGroup))
                    pathInputField.Select();
            });
            fileButton.onClick.AddListener(() => { mainDisplay.PromptUserFilePicker(); });
            
            // tracksButton.onClick.AddListener(() =>
            // {
            //     ToggleElement(tracksButtonsGroup);
            //     SetupTrackButtons();
            // });
            volumeButton.onClick.AddListener(() => { ToggleElement(volumeGroup.gameObject); });
            pathEnterButton.onClick.AddListener(() =>
            {
                ToggleElement(consoleGroup);
                mainDisplay.vlcFeed.Open(pathInputField.text);
            });

            //Seek Bar Events
            var seekBarEvents = seekBar.GetComponent<EventTrigger>();

            var seekBarPointerDown = new EventTrigger.Entry();
            seekBarPointerDown.eventID = EventTriggerType.PointerDown;
            seekBarPointerDown.callback.AddListener((data) => { _isDraggingSeekBar = true; });
            seekBarEvents.triggers.Add(seekBarPointerDown);

            var seekBarPointerUp = new EventTrigger.Entry();
            seekBarPointerUp.eventID = EventTriggerType.PointerUp;
            seekBarPointerUp.callback.AddListener((data) =>
            {
                _isDraggingSeekBar = false;
                mainDisplay.vlcFeed.SetTime((long)((double)mainDisplay.vlcFeed.Duration * seekBar.value));
            });
            seekBarEvents.triggers.Add(seekBarPointerUp);
            
            // AR Combo Bar Events
            var aspectRatioSliderEvents = aspectRatioSlider.GetComponent<EventTrigger>();
            
            // TODO: the following drag & drop with EventTrigger should have a shared class
            var aspectRatioSliderPointerDown = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerDown
                };
            aspectRatioSliderPointerDown.callback.AddListener((_) => { _isDraggingaspectRatioSlider = true; });
            aspectRatioSliderEvents.triggers.Add(aspectRatioSliderPointerDown);

            var aspectRatioSliderPointerUp = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerUp
                };
            
            void SyncText(float value)
            {
                var arDecimal = Mathf.Round(value * 100f) / 100f;
                // Get the aspect ratio fraction from the decimal
                var frac = Frac.FromDouble(arDecimal);

                mainDisplay.AspectRatio = frac;

                // var updater = new AspectRatioUpdater(mainDisplay);
                // updater.SyncText();
            }
            
            aspectRatioSliderPointerUp.callback.AddListener((_) =>
            {
                if (_isDraggingaspectRatioSlider) SyncText(aspectRatioSlider.value);
                _isDraggingaspectRatioSlider = false;
            });
            aspectRatioSliderEvents.triggers.Add(aspectRatioSliderPointerUp);
            aspectRatioSlider.onValueChanged.AddListener(SyncText);

            //Volume Bar
            volumeBar.wholeNumbers = true;
            volumeBar.maxValue = maxVolume; //You can go higher than 100 but you risk audio clipping
            volumeBar.value = mainDisplay.vlcFeed.Volume;
            volumeBar.onValueChanged.AddListener((data) => { mainDisplay.vlcFeed.SetVolume((int)volumeBar.value); });
        }


        private void Update()
        {
            //Update screen aspect ratio. Doing this every frame is probably more than is necessary.

            //if(vlcPlayer.texture != null)
            //	screenAspectRatioFitter.aspectRatio = (float)vlcPlayer.texture.width / (float)vlcPlayer.texture.height;

            UpdatePlayPauseButton(_isPlaying);

            UpdateSeekBar();
        }

        //Show the Pause button if we are playing, or the Play button if we are paused or stopped
        private void UpdatePlayPauseButton(bool playing)
        {
            pauseButton.gameObject.SetActive(playing);
            playButton.gameObject.SetActive(!playing);
        }

        //Update the position of the Seek slider to the match the VLC Player
        private void UpdateSeekBar()
        {
            var mm = mainDisplay;
            // Get the current playback time as a TimeSpan object
            var currentTime = mainDisplay.vlcFeed.Time;
            var currentTimeSpan = TimeSpan.FromMilliseconds(currentTime);

            // Format the TimeSpan object as a string in the desired format
            var timecode = currentTimeSpan.ToString(@"hh\:mm\:ss");

            currentTimecode.text = timecode;

            if (!_isDraggingSeekBar)
            {
                var duration = mainDisplay.vlcFeed.Duration;
                if (duration > 0)
                    seekBar.value = (float)((double)mainDisplay.vlcFeed.Time / duration);
            }
        }

        //Enable a GameObject if it is disabled, or disable it if it is enabled
        private bool ToggleElement(GameObject element)
        {
            var toggled = !element.activeInHierarchy;
            element.SetActive(toggled);
            return toggled;
        }
    }
}
