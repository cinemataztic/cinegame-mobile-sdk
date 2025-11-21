using UnityEngine;

public static class TextureUtility {
    /// <summary>
    /// Adjusts the dimensions so they comply with restrictions of the target format.
    /// </summary>
    public static Vector2Int GetAdjustedSize (TextureFormat format, int width, int height, bool mipmap) {
        if (RequiresPowerOfTwo (format, mipmap)) {
            width = Mathf.NextPowerOfTwo (width);
            height = Mathf.NextPowerOfTwo (height);
        }

        // Minimum size rules (applies after POT adjustments)
        Vector2Int min = GetMinimumSize (format);
        width = Mathf.Max (width, min.x);
        height = Mathf.Max (height, min.y);

        // Block-compressed formats require alignment
        Vector2Int block = GetBlockSize (format);
        if (block.x > 1 || block.y > 1) {
            width = AlignUp (width, block.x);
            height = AlignUp (height, block.y);
        }

        return new Vector2Int (width, height);
    }

    /// <summary>
	/// Returns true if the format requires power of two dimensions
	/// </summary>
    public static bool RequiresPowerOfTwo (TextureFormat format, bool mipmap) {
        switch (format) {
        // PVRTC requires strict POT
        case TextureFormat.PVRTC_RGB2:
        case TextureFormat.PVRTC_RGBA2:
        case TextureFormat.PVRTC_RGB4:
        case TextureFormat.PVRTC_RGBA4:
            return true;
        default:
            break;
        }
        if (mipmap) {
            switch (format) {
            case TextureFormat.ASTC_4x4:
            case TextureFormat.ASTC_5x5:
            case TextureFormat.ASTC_6x6:
            case TextureFormat.ASTC_8x8:
            case TextureFormat.ASTC_10x10:
            case TextureFormat.ASTC_12x12:
            case TextureFormat.ASTC_HDR_4x4:
            case TextureFormat.ASTC_HDR_5x5:
            case TextureFormat.ASTC_HDR_6x6:
            case TextureFormat.ASTC_HDR_8x8:
            case TextureFormat.ASTC_HDR_10x10:
            case TextureFormat.ASTC_HDR_12x12:
                return true;
            default:
                break;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns minimal legal texture dimensions for format.
    /// </summary>
    public static Vector2Int GetMinimumSize (TextureFormat format) {
        switch (format) {
        // DXT / BC formats require at least 4x4
        case TextureFormat.DXT1:
        case TextureFormat.DXT5:
        case TextureFormat.BC4:
        case TextureFormat.BC5:
        case TextureFormat.BC6H:
        case TextureFormat.BC7:
            return new Vector2Int (4, 4);

        // ETC / EAC formats also block compressed 4x4
        case TextureFormat.ETC_RGB4:
        case TextureFormat.ETC2_RGB:
        case TextureFormat.ETC2_RGBA8:
        case TextureFormat.EAC_R:
        case TextureFormat.EAC_RG:
            return new Vector2Int (4, 4);

        // ASTC allows any size ≥4×4
        case TextureFormat.ASTC_4x4:
        case TextureFormat.ASTC_5x5:
        case TextureFormat.ASTC_6x6:
        case TextureFormat.ASTC_8x8:
        case TextureFormat.ASTC_10x10:
        case TextureFormat.ASTC_12x12:
        case TextureFormat.ASTC_HDR_4x4:
        case TextureFormat.ASTC_HDR_5x5:
        case TextureFormat.ASTC_HDR_6x6:
        case TextureFormat.ASTC_HDR_8x8:
        case TextureFormat.ASTC_HDR_10x10:
        case TextureFormat.ASTC_HDR_12x12:
            return new Vector2Int (4, 4);

        // PVRTC minimum 8×8 for lower quality
        case TextureFormat.PVRTC_RGB2:
        case TextureFormat.PVRTC_RGBA2:
            return new Vector2Int (16, 8);
        case TextureFormat.PVRTC_RGB4:
        case TextureFormat.PVRTC_RGBA4:
            return new Vector2Int (8, 8);

        default:
            return new Vector2Int (1, 1);
        }
    }

    /// <summary>
    /// Returns the block size for block compressed formats.
    /// Non-block formats return (1,1).
    /// </summary>
    public static Vector2Int GetBlockSize (TextureFormat format) {
        switch (format) {
        // BCn / DXT
        case TextureFormat.DXT1:
        case TextureFormat.DXT5:
        case TextureFormat.BC4:
        case TextureFormat.BC5:
        case TextureFormat.BC6H:
        case TextureFormat.BC7:
            return new Vector2Int (4, 4);

        // ETC / EAC (all are 4×4)
        case TextureFormat.ETC_RGB4:
        case TextureFormat.ETC2_RGB:
        case TextureFormat.ETC2_RGBA8:
        case TextureFormat.EAC_R:
        case TextureFormat.EAC_RG:
            return new Vector2Int (4, 4);

        // ASTC: block size is encoded in format
        case TextureFormat.ASTC_4x4:
        case TextureFormat.ASTC_HDR_4x4:
            return new Vector2Int (4, 4);
        case TextureFormat.ASTC_5x5:
        case TextureFormat.ASTC_HDR_5x5:
            return new Vector2Int (5, 5);
        case TextureFormat.ASTC_6x6:
        case TextureFormat.ASTC_HDR_6x6:
            return new Vector2Int (6, 6);
        case TextureFormat.ASTC_8x8:
        case TextureFormat.ASTC_HDR_8x8:
            return new Vector2Int (8, 8);
        case TextureFormat.ASTC_10x10:
        case TextureFormat.ASTC_HDR_10x10:
            return new Vector2Int (10, 10);
        case TextureFormat.ASTC_12x12:
        case TextureFormat.ASTC_HDR_12x12:
            return new Vector2Int (12, 12);

        // PVRTC is special — not block-based
        //case TextureFormat.PVRTC_RGB2:
        //case TextureFormat.PVRTC_RGBA2:
        //case TextureFormat.PVRTC_RGB4:
        //case TextureFormat.PVRTC_RGBA4:
        default:
            return new Vector2Int (1, 1);
        }
    }

    /// <summary>
	/// Round value up to nearest alignment
	/// </summary>
    static int AlignUp (int value, int alignment) {
        if (alignment <= 1)
            return value;
        return ((value + alignment - 1) / alignment) * alignment;
    }

    /// <summary>
    /// Crops texture so it has same aspect ratio as imageWidth:imageHeight
    /// </summary>
    public static void CropTextureToImage (Texture2D texture, int imageWidth, int imageHeight, out Texture2D croppedTexture) {
        var pHeight = imageHeight * texture.width / imageWidth;
        Color [] pixels;
        if (pHeight <= texture.height) {
            croppedTexture = new Texture2D (texture.width, pHeight, texture.format, false);
            pixels = texture.GetPixels (0, (texture.height - pHeight) / 2, texture.width, pHeight);
        } else {
            var pWidth = imageWidth * texture.height / imageHeight;
            croppedTexture = new Texture2D (pWidth, texture.height, texture.format, false);
            pixels = texture.GetPixels ((texture.width - pWidth) / 2, 0, pWidth, texture.height);
        }
        croppedTexture.SetPixels (pixels);
        croppedTexture.Apply ();
    }

    /// <summary>
    /// Crops texture to specified UV rect. If the texture is non-readable, blit to temp RenderTexture and copy from there
    /// </summary>
    public static void CropTexture (Texture2D texture, Rect uvRect, bool mipChain, bool readable, out Texture2D croppedTexture) {
        var x = (int)(texture.width * uvRect.xMin);
        var y = (int)(texture.height * uvRect.yMin);
        var w = (int)(texture.width * uvRect.size.x);
        var h = (int)(texture.height * uvRect.size.y);
        croppedTexture = new Texture2D (w, h, texture.format, mipChain);
        if (texture.isReadable) {
            var pixels = texture.GetPixels (x, y, w, h, 0);
            croppedTexture.SetPixels (pixels);
            croppedTexture.Apply (mipChain, !readable);
        } else {
            // Create a temporary RenderTexture of the same size as the texture
            var tmpRenderTexture = RenderTexture.GetTemporary (texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit (texture, tmpRenderTexture);
            var prevRenderTexture = RenderTexture.active;
            RenderTexture.active = tmpRenderTexture;

            // Copy the pixels to the cropped texture
            croppedTexture.ReadPixels (new Rect (x, y, w, h), 0, 0);
            croppedTexture.Apply (mipChain, !readable);

            //release RenderTexture
            RenderTexture.active = prevRenderTexture;
            RenderTexture.ReleaseTemporary (tmpRenderTexture);
        }
    }

    /// <summary>
    /// Blit non-readable texture to rendertexture and copy results to readable texture. Remember to release texture when done with it!
    /// </summary>
    public static Texture2D CreateReadableTexture2D (Texture2D texture) {
        // Create a temporary RenderTexture of the same size as the texture
        var tmpRenderTexture = RenderTexture.GetTemporary (texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit (texture, tmpRenderTexture);
        var prevRenderTexture = RenderTexture.active;
        RenderTexture.active = tmpRenderTexture;

        // Create a readable Texture2D and copy the pixels to it
        var readableTexture = new Texture2D (texture.width, texture.height);
        readableTexture.ReadPixels (new Rect (0, 0, tmpRenderTexture.width, tmpRenderTexture.height), 0, 0);
        readableTexture.Apply ();
        RenderTexture.active = prevRenderTexture;
        RenderTexture.ReleaseTemporary (tmpRenderTexture);
        return readableTexture;
    }
}
