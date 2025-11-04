using UnityEngine;

namespace Tvg
{
    /// <summary>
    /// Asset that stores SVG vector data with pre-rendered sprite.
    /// Can be used directly on SpriteRenderer or with TvgPlayer.
    /// Automatically created when importing .svg files.
    /// </summary>
    public class SvgAsset : ScriptableObject
    {
        [SerializeField]
        [HideInInspector]
        private string svgData;

        [SerializeField]
        [HideInInspector]
        private Sprite sprite;

        [SerializeField]
        [HideInInspector]
        private Texture2D texture;

        /// <summary>
        /// Gets the pre-rendered sprite (for use with SpriteRenderer)
        /// </summary>
        public Sprite Sprite => sprite;

        /// <summary>
        /// Gets the rendered texture
        /// </summary>
        public Texture2D Texture => texture;

        /// <summary>
        /// Gets the raw SVG text data (for use with TvgPlayer)
        /// </summary>
        public string GetData() => svgData;

        /// <summary>
        /// Sets the SVG data and rendered assets (used by importer)
        /// </summary>
        public void SetData(string data, Texture2D tex, Sprite spr)
        {
            svgData = data;
            texture = tex;
            sprite = spr;
        }

        /// <summary>
        /// Implicit conversion to string for easy use with TvgTexture
        /// </summary>
        public static implicit operator string(SvgAsset asset)
        {
            return asset?.svgData;
        }

        /// <summary>
        /// Implicit conversion to Sprite for easy use with SpriteRenderer
        /// </summary>
        public static implicit operator Sprite(SvgAsset asset)
        {
            return asset?.sprite;
        }
    }
}
