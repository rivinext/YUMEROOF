using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Yume
{
    public class ScreenshotCapture : MonoBehaviour
    {
        [SerializeField] private Camera captureCamera;
        [SerializeField] private List<Canvas> hiddenCanvases = new List<Canvas>();
        [SerializeField] private int superSize = 1;
        [SerializeField] private string fileNamePrefix = "screenshot";
        [SerializeField] private LayerMask additionalExcludedLayers;
        [SerializeField] private string customBasePath = string.Empty;

        private void Awake()
        {
            if (captureCamera == null)
            {
                captureCamera = Camera.main;
            }
        }

        public void Capture()
        {
            StartCoroutine(CaptureRoutine());
        }

        private IEnumerator CaptureRoutine()
        {
            foreach (var canvas in hiddenCanvases)
            {
                if (canvas != null)
                {
                    canvas.enabled = false;
                }
            }

            yield return new WaitForEndOfFrame();

            Texture2D texture;
            if (captureCamera != null)
            {
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
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save screenshot to resolved path {path}: {ex.Message}");
            }
            finally
            {
                foreach (var canvas in hiddenCanvases)
                {
                    if (canvas != null)
                    {
                        canvas.enabled = true;
                    }
                }

                Destroy(texture);
            }
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
    }
}
