using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Optional custom-avatar layer for PlayerProfilePanel.
/// Reuses PlayerProfileManager.avatarIndex by reserving one sentinel value for a user-selected image.
/// The selected image is normalized to PNG and stored under Application.persistentDataPath.
/// </summary>
[DisallowMultipleComponent]
public class PlayerCustomAvatarPicker : MonoBehaviour
{
    [Header("References")]
    public Image avatarImage;
    public Button customAvatarButton;

    [Header("Custom Avatar Save")]
    [Tooltip("Reserved avatarIndex used to indicate that the custom image should be displayed.")]
    public int customAvatarIndexSentinel = 2147483000;
    [Tooltip("Subfolder created inside Application.persistentDataPath.")]
    public string saveFolderName = "Profile";
    [Tooltip("Normalized PNG file name stored inside the profile folder.")]
    public string customAvatarFileName = "custom_avatar.png";
    public bool deleteCustomAvatarOnProfileReset = true;

    [Header("Image Limits")]
    [Tooltip("Reject source files larger than this many megabytes. Set 0 for no file-size limit.")]
    [Min(0)] public int maxSourceFileSizeMB = 12;
    [Tooltip("Images larger than this dimension are downscaled before being saved. Set 0 to keep original resolution.")]
    [Min(0)] public int maxSavedDimension = 1024;
    [Tooltip("Mipmaps are unnecessary for a UI avatar and consume extra memory.")]
    public bool generateMipMaps = false;

    [Header("Picker Presentation")]
    public string pickerTitle = "Choose profile avatar";
    public string fileFilterLabel = "Image Files";

    private Texture2D runtimeTexture;
    private Sprite runtimeSprite;

    private void OnEnable()
    {
        PlayerProfileManager.OnProfileLoaded += RefreshFromProfile;
        PlayerProfileManager.OnProfileIdentityChanged += RefreshFromProfile;
        PlayerProfileManager.OnProfileReset += HandleProfileReset;

        if (customAvatarButton != null)
        {
            customAvatarButton.onClick.RemoveListener(OpenImagePicker);
            customAvatarButton.onClick.AddListener(OpenImagePicker);
        }

        RefreshFromProfile();
    }

    private void OnDisable()
    {
        PlayerProfileManager.OnProfileLoaded -= RefreshFromProfile;
        PlayerProfileManager.OnProfileIdentityChanged -= RefreshFromProfile;
        PlayerProfileManager.OnProfileReset -= HandleProfileReset;

        if (customAvatarButton != null)
            customAvatarButton.onClick.RemoveListener(OpenImagePicker);
    }

    private void OnDestroy()
    {
        ReleaseRuntimeAvatar();
    }

    public void OpenImagePicker()
    {
        string selectedPath;
        if (!NativeImageFilePicker.TryPickImage(pickerTitle, fileFilterLabel, out selectedPath))
            return;

        if (string.IsNullOrWhiteSpace(selectedPath) || !File.Exists(selectedPath))
            return;

        try
        {
            FileInfo info = new FileInfo(selectedPath);
            if (maxSourceFileSizeMB > 0)
            {
                long limitBytes = (long)maxSourceFileSizeMB * 1024L * 1024L;
                if (info.Length > limitBytes)
                {
                    Debug.LogWarning($"Custom avatar rejected: file is larger than {maxSourceFileSizeMB} MB.", this);
                    return;
                }
            }

            byte[] bytes = File.ReadAllBytes(selectedPath);
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, generateMipMaps, false);
            if (!ImageConversion.LoadImage(source, bytes, false))
            {
                Destroy(source);
                Debug.LogWarning("Custom avatar could not be decoded as an image.", this);
                return;
            }

            Texture2D normalized = NormalizeTexture(source);
            if (normalized != source)
                Destroy(source);

            byte[] png = normalized.EncodeToPNG();
            string folder = GetSaveFolderPath();
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(GetSavedAvatarPath(), png);

            ApplyRuntimeTexture(normalized);

            PlayerProfileManager profile = PlayerProfileManager.Instance;
            if (profile != null)
                profile.SetAvatarIndex(customAvatarIndexSentinel, true);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to import custom avatar: " + ex.Message, this);
        }
    }

    public void RefreshFromProfile()
    {
        PlayerProfileManager profile = PlayerProfileManager.Instance;
        if (profile == null || profile.AvatarIndex != customAvatarIndexSentinel)
            return;

        LoadSavedAvatar();
    }

    private void HandleProfileReset()
    {
        if (deleteCustomAvatarOnProfileReset)
        {
            try
            {
                string path = GetSavedAvatarPath();
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Could not delete saved custom avatar during profile reset: " + ex.Message, this);
            }
        }

        ReleaseRuntimeAvatar();
    }

    private void LoadSavedAvatar()
    {
        string path = GetSavedAvatarPath();
        if (!File.Exists(path))
        {
            Debug.LogWarning("Profile is set to custom avatar but the saved image file is missing.", this);
            return;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, generateMipMaps, false);
            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                Destroy(texture);
                return;
            }

            ApplyRuntimeTexture(texture);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Could not load saved custom avatar: " + ex.Message, this);
        }
    }

    private Texture2D NormalizeTexture(Texture2D source)
    {
        if (source == null || maxSavedDimension <= 0)
            return source;

        int largest = Mathf.Max(source.width, source.height);
        if (largest <= maxSavedDimension)
            return source;

        float scale = maxSavedDimension / (float)largest;
        int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
        int height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));

        RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture previous = RenderTexture.active;
        Graphics.Blit(source, temporary);
        RenderTexture.active = temporary;

        Texture2D resized = new Texture2D(width, height, TextureFormat.RGBA32, generateMipMaps, false);
        resized.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        resized.Apply(generateMipMaps, false);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temporary);
        return resized;
    }

    private void ApplyRuntimeTexture(Texture2D texture)
    {
        ReleaseRuntimeAvatar();
        runtimeTexture = texture;
        runtimeTexture.name = "RuntimeCustomAvatar";
        runtimeSprite = Sprite.Create(
            runtimeTexture,
            new Rect(0f, 0f, runtimeTexture.width, runtimeTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        runtimeSprite.name = "RuntimeCustomAvatarSprite";

        if (avatarImage != null)
        {
            avatarImage.sprite = runtimeSprite;
            avatarImage.preserveAspect = true;
            avatarImage.enabled = true;
        }
    }

    private void ReleaseRuntimeAvatar()
    {
        if (runtimeSprite != null)
            Destroy(runtimeSprite);
        if (runtimeTexture != null)
            Destroy(runtimeTexture);
        runtimeSprite = null;
        runtimeTexture = null;
    }

    private string GetSaveFolderPath()
    {
        string folder = string.IsNullOrWhiteSpace(saveFolderName) ? "Profile" : saveFolderName.Trim();
        return Path.Combine(Application.persistentDataPath, folder);
    }

    private string GetSavedAvatarPath()
    {
        string fileName = string.IsNullOrWhiteSpace(customAvatarFileName) ? "custom_avatar.png" : customAvatarFileName.Trim();
        if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            fileName += ".png";
        return Path.Combine(GetSaveFolderPath(), fileName);
    }

    private static class NativeImageFilePicker
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OpenFileName
        {
            public int structSize;
            public IntPtr dlgOwner;
            public IntPtr instance;
            public string filter;
            public string customFilter;
            public int maxCustFilter;
            public int filterIndex;
            public StringBuilder file;
            public int maxFile;
            public StringBuilder fileTitle;
            public int maxFileTitle;
            public string initialDir;
            public string title;
            public int flags;
            public short fileOffset;
            public short fileExtension;
            public string defExt;
            public IntPtr custData;
            public IntPtr hook;
            public string templateName;
            public IntPtr reservedPtr;
            public int reservedInt;
            public int flagsEx;
        }

        [DllImport("Comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetOpenFileName(ref OpenFileName ofn);

        private const int OFN_FILEMUSTEXIST = 0x00001000;
        private const int OFN_PATHMUSTEXIST = 0x00000800;
        private const int OFN_NOCHANGEDIR = 0x00000008;
        private const int OFN_EXPLORER = 0x00080000;
#endif

        public static bool TryPickImage(string title, string filterLabel, out string selectedPath)
        {
            selectedPath = null;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            StringBuilder fileBuffer = new StringBuilder(4096);
            StringBuilder titleBuffer = new StringBuilder(512);
            string label = string.IsNullOrWhiteSpace(filterLabel) ? "Image Files" : filterLabel;

            OpenFileName ofn = new OpenFileName
            {
                structSize = Marshal.SizeOf(typeof(OpenFileName)),
                filter = label + "\0*.png;*.jpg;*.jpeg;*.bmp\0PNG\0*.png\0JPEG\0*.jpg;*.jpeg\0Bitmap\0*.bmp\0All Files\0*.*\0\0",
                filterIndex = 1,
                file = fileBuffer,
                maxFile = fileBuffer.Capacity,
                fileTitle = titleBuffer,
                maxFileTitle = titleBuffer.Capacity,
                title = string.IsNullOrWhiteSpace(title) ? "Choose profile avatar" : title,
                flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR,
                defExt = "png"
            };

            if (!GetOpenFileName(ref ofn))
                return false;

            selectedPath = ofn.file.ToString();
            return !string.IsNullOrWhiteSpace(selectedPath);
#else
            Debug.LogWarning("Custom avatar native file picker is currently implemented for Windows standalone/editor only.");
            return false;
#endif
        }
    }
}
