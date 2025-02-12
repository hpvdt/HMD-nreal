using UnityEngine;

namespace HMD.Scripts.Streaming
{
    using System;
    using Unity.VisualScripting;

    public class TextureView : IDisposable
    // immutable, can only be initialised once, all derivative textures can only be destroyed together
    {
        private readonly Texture _source; //This is the texture libVLC writes to directly.
        private readonly RenderTexture _cache; //We copy it into this texture which we actually use in unity.

        public readonly Lazy<(int, int)> Size;

        public Lazy<string> NativeAspectRatioText; // width:height

        public TextureView(Texture source)
        {
            _source = source;
            _cache = new RenderTexture(_source.width, _source.height, 0);
            //Make a renderTexture the same size as _source.

            Debug.Log($"[TextureView] {source.width}x{source.height}");

            _cache.Create();

            Size = new Lazy<(int, int)>((_source.width, _source.height));
            NativeAspectRatioText = new Lazy<string>(() => $"{Size.Value.Item1}:{Size.Value.Item2}");
        }

        public Texture Source
        {
            get { return _source; }
        }

        public RenderTexture Cache
        {
            get { return _cache; }
        }

        public Texture Effective
        {
            get { return Cache; }
        }

        public void Dispose()
        {
            _cache.Release();
        }

        private TextureView()
        {
            Dispose();
        }

        public override string ToString()
        {
            try
            {
                return $"{_source}: {Size.Value.Item1}x{Size.Value.Item2}";
            }
            catch (Exception)
            {
                return _source.ToSafeString();
            }
        }
    }
}
