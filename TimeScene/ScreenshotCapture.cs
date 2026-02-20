using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;

namespace Yume
{
    public class ScreenshotCapture : MonoBehaviour
    {
        [SerializeField] private Camera captureCamera;
        [SerializeField] private List<Canvas> hiddenCanvases = new List<Canvas>();
        [SerializeField] private List<string> excludedCanvasTags = new List<string>();
        [SerializeField] private LayerMask excludedCanvasLayers;
        [SerializeField] private List<string> excludedCanvasNamePrefixes = new List<string> { "FadeCanvas" };
        [SerializeField] private bool excludeCanvasesWithMarker = true;
        [SerializeField] private int superSize = 1;
        [SerializeField] private string fileNamePrefix = "screenshot";
        [SerializeField] private LayerMask additionalExcludedLayers;
        [SerializeField] private string customBasePath = string.Empty;

        private void Awake()
        {
            ResolveCaptureCamera("Awake");
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [SerializeField] private bool preferSteamOverlayScreenshot;

        public void Capture()
        {
            if (preferSteamOverlayScreenshot && SteamManager.Initialized)
            {
                Debug.Log("Triggering Steam overlay screenshot capture.");
                SteamScreenshots.TriggerScreenshot();
                return;
            }

            if (preferSteamOverlayScreenshot && !SteamManager.Initialized)
            {
                Debug.LogWarning("Steam overlay screenshot requested, but Steam API is not initialized. Falling back to custom capture.");
            }

            ResolveCaptureCamera("Capture");

            StartCoroutine(CaptureRoutine());
        }

        private IEnumerator CaptureRoutine()
        {
            var hiddenCanvasStates = new Dictionary<Canvas, bool>();
            var canvasesToHide = CollectCanvasesToHide();

            foreach (var canvas in canvasesToHide)
            {
                if (canvas == null)
                {
                    continue;
                }

                hiddenCanvasStates[canvas] = canvas.enabled;
                canvas.enabled = false;
            }

            Debug.Log(BuildHiddenCanvasSummary(canvasesToHide));

            yield return new WaitForEndOfFrame();

            ResolveCaptureCamera("CaptureRoutine");

            Texture2D texture;
            if (captureCamera != null)
            {
                Debug.Log($"Using captureCamera.Render() path. camera={captureCamera.name}");
                var width = Screen.width * superSize;
                var height = Screen.height * superSize;
                var renderTexture = new RenderTexture(width, height, 24);

                var previousTarget = captureCamera.targetTexture;
                var previousActive = RenderTexture.active;
                var previousCullingMask = captureCamera.cullingMask;

                try
                {
                    var uiLayer = LayerMask.NameToLayer("UI");
                    var excludeMask = additionalExcludedLayers;
                    if (uiLayer >= 0)
                    {
                        excludeMask |= 1 << uiLayer;
                    }
                    captureCamera.cullingMask &= ~excludeMask;

                    captureCamera.targetTexture = renderTexture;
                    captureCamera.Render();

                    RenderTexture.active = renderTexture;

                    texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture.Apply();
                }
                finally
                {
                    captureCamera.targetTexture = previousTarget;
                    captureCamera.cullingMask = previousCullingMask;
                    RenderTexture.active = previousActive;
                    Destroy(renderTexture);
                }
            }
            else
            {
                Debug.LogWarning("captureCamera was null or destroyed right before capture. Falling back to ScreenCapture.CaptureScreenshotAsTexture().");
                texture = ScreenCapture.CaptureScreenshotAsTexture(superSize);
            }

            var bytes = texture.EncodeToPNG();
            var fileName = string.Format("{0}_{1:yyyyMMdd_HHmmss}.png", fileNamePrefix, DateTime.Now);

            var basePath = ResolveBasePath();
            var path = Path.Combine(basePath, fileName);

            Debug.Log($"Resolved screenshot path: {path}");

            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(path, bytes);
                Debug.Log($"Screenshot saved to: {path}");

                if (SteamManager.Initialized)
                {
                    var screenshotHandle = SteamScreenshots.AddScreenshotToLibrary(path, null, texture.width, texture.height);
                    Debug.Log($"Steam screenshot registered. Handle: {screenshotHandle.m_ScreenshotHandle}");
                }
                else
                {
                    Debug.Log("Steam API not initialized. Skipping Steam screenshot registration.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save screenshot to resolved path {path}: {ex.Message}");
            }
            finally
            {
                foreach (var hiddenCanvasState in hiddenCanvasStates)
                {
                    if (hiddenCanvasState.Key != null)
                    {
                        hiddenCanvasState.Key.enabled = hiddenCanvasState.Value;
                    }
                }

                Destroy(texture);
            }
        }

        private List<Canvas> CollectCanvasesToHide()
        {
            var canvasesToHide = new HashSet<Canvas>();

            foreach (var canvas in hiddenCanvases)
            {
                if (canvas != null)
                {
                    canvasesToHide.Add(canvas);
                }
            }

            var sceneCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var canvas in sceneCanvases)
            {
                if (canvas == null || !canvas.enabled)
                {
                    continue;
                }

                if (ShouldExcludeCanvas(canvas))
                {
                    canvasesToHide.Add(canvas);
                }
            }

            return new List<Canvas>(canvasesToHide);
        }

        private bool ShouldExcludeCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                return false;
            }

            if ((excludedCanvasLayers.value & (1 << canvas.gameObject.layer)) != 0)
            {
                return true;
            }

            foreach (var excludedTag in excludedCanvasTags)
            {
                if (!string.IsNullOrWhiteSpace(excludedTag) && canvas.CompareTag(excludedTag))
                {
                    return true;
                }
            }

            foreach (var namePrefix in excludedCanvasNamePrefixes)
            {
                if (!string.IsNullOrWhiteSpace(namePrefix) && canvas.name.StartsWith(namePrefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            if (excludeCanvasesWithMarker && canvas.TryGetComponent<ScreenshotExcludeCanvasMarker>(out _))
            {
                return true;
            }

            return false;
        }

        private static string BuildHiddenCanvasSummary(List<Canvas> canvasesToHide)
        {
            if (canvasesToHide == null || canvasesToHide.Count == 0)
            {
                return "ScreenshotCapture: hid 0 canvases before capture.";
            }

            var names = new List<string>();
            foreach (var canvas in canvasesToHide)
            {
                if (canvas != null)
                {
                    names.Add(canvas.name);
                }
            }

            return $"ScreenshotCapture: hid {names.Count} canvas(es) before capture [{string.Join(", ", names)}]";
        }

        private string ResolveBasePath()
        {
#if DEMO_VERSION
            const string versionFolder = "demo";
#else
            const string versionFolder = "release";
#endif

            var basePath = !string.IsNullOrWhiteSpace(customBasePath)
                ? customBasePath
                : ResolvePlatformDefaultPath();

            basePath = Environment.ExpandEnvironmentVariables(basePath);

            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = Application.persistentDataPath;
            }

            var versionedPath = Path.Combine(basePath, versionFolder);

            Directory.CreateDirectory(versionedPath);

            return versionedPath;
        }

        private string ResolvePlatformDefaultPath()
        {
            const string windowsDefaultPath = "%USERPROFILE%\\Pictures\\Screenshots";

            if (Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.WindowsEditor)
            {
                var expandedWindowsPath = Environment.ExpandEnvironmentVariables(windowsDefaultPath);

                if (!string.IsNullOrWhiteSpace(expandedWindowsPath))
                {
                    Directory.CreateDirectory(expandedWindowsPath);
                    return expandedWindowsPath;
                }
            }

            return Application.persistentDataPath;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResolveCaptureCamera($"SceneLoaded({scene.name}, {mode})");
        }

        private void ResolveCaptureCamera(string context)
        {
            if (captureCamera != null)
            {
                return;
            }

            captureCamera = Camera.main;

            if (captureCamera == null)
            {
                var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (cameras.Length > 0)
                {
                    Array.Sort(cameras, (left, right) => right.depth.CompareTo(left.depth));
                    captureCamera = cameras[0];
                }
            }

            if (captureCamera != null)
            {
                Debug.Log($"Resolved captureCamera in {context}: {captureCamera.name}");
            }
            else
            {
                Debug.LogWarning($"Failed to resolve captureCamera in {context}. Will fall back to ScreenCapture if capture proceeds.");
            }
        }
    }

    public class ScreenshotExcludeCanvasMarker : MonoBehaviour
    {
    }
}
