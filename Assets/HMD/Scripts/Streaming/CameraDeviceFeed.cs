namespace HMD.Scripts.Streaming
{
    using System;
    using System.Linq;
    using Unity.VisualScripting;
    using UnityEngine;
    public class CameraDeviceFeed : FeedLike, IFeed
    {
        public struct CameraSelector
        {
            public string Name;
            public Resolution? Res;
        }

        private WebCamTexture _webCamTex;
        private static WebCamDevice[] Devices
        {
            get
            {
                var result = WebCamTexture.devices;
                return result;
            }
        }

        private WebCamDevice? _device;

        public void OpenNextDevice(Resolution? res)
        {
            var devices = Devices;
            var index = Array.IndexOf(devices, _device);
            index = (index + 1) % devices.Length;
            _device = devices[index];

            OpenResolution(res);
        }

        public void OpenResolution(Resolution? res)
        {
            if (_device == null) return;

            var resList = _device?.availableResolutions;

            if (res == null)
            {
                Log($"Setting up camera `{_device.Value.name}`");
                Open(new CameraSelector
                {
                    Name = _device.Value.name
                });
            }
            else
            {
                var _res = res.Value;
                if (resList != null && !resList.Contains(_res))
                {
                    LogWarning(
                        $"resolution `{_res.ToString()}` may be unsupported:\n"
                        + $"supported resolutions are [{string.Join(", ", resList.Select(x => x.ToString()))}]"
                    );
                }

                Open(new CameraSelector
                {
                    Name = _device.Value.name,
                    Res = res
                });
            }
        }

        public void Open(CameraSelector selector)
        {
            Stop();
            // _cameraTexture?.IsDestroyed()

            if (selector.Res.HasValue)
            {
                var res = selector.Res.Value;
                var fps = selector.Res.Value.refreshRate;
                _webCamTex = new WebCamTexture(
                    selector.Name,
                    res.height,
                    res.width,
                    fps
                );

                // _sourceTexture.requestedWidth = res.width;
                // _sourceTexture.requestedHeight = res.height;
                // _sourceTexture.requestedFPS = fps;
            }
            else
            {
                _webCamTex = new WebCamTexture(
                    selector.Name
                );
            }

            Play();
            _webCamTex.GetPixels(); // otherwise width and height will always be 16x16

            Log(
                $"Setting up camera:\n"
                + $"    Seleccted: `{selector.Name}` ({selector.Res.ToSafeString()})\n"
                + $"    Actual: `{_webCamTex.deviceName}` ({_webCamTex.width}x{_webCamTex.height} @ {_webCamTex.requestedFPS}fps)"
            );

        }

        public void Stop()
        {
            Log("Stop");
            _webCamTex?.Stop();
        }

        public TextureView? TryGetTexture(TextureView? existing)
        {

            var tex = existing;
            if (_webCamTex == null || existing?.Source != _webCamTex)
            {
                LogWarning("existing texture is not from this feed, creating new texture");
                existing?.Dispose();
                tex = new TextureView(_webCamTex);
            }

            Graphics.Blit(tex.Source, tex.Cache, Transform, Offset);

            return tex;
        }

        public void Play()
        {
            Log("Play");
            _webCamTex?.Play();
        }

        public void Pause()
        {
            _webCamTex?.Pause();
        }

        public void Dispose()
        {
            Stop();

            Log("Destroy Camera Feed");
            _webCamTex = null;
        }
    }
}
