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

        private void Awake()
        {
            if (captureCamera == null)
            {
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    var screenshotObj = Instantiate(
                        mainCamera.gameObject,
                        mainCamera.transform.position,
                        mainCamera.transform.rotation,
                        mainCamera.transform.parent);
                    screenshotObj.name = "ScreenshotCamera";

                    captureCamera = screenshotObj.GetComponent<Camera>();
                    if (captureCamera != null)
                    {
                        captureCamera.enabled = false;

                        var uiLayer = LayerMask.NameToLayer("UI");
                        var excludeMask = additionalExcludedLayers;
                        if (uiLayer >= 0)
                        {
                            excludeMask |= 1 << uiLayer;
                        }
                        captureCamera.cullingMask &= ~excludeMask;
                    }

                    var audioListener = screenshotObj.GetComponent<AudioListener>();
                    if (audioListener != null)
                    {
                        Destroy(audioListener);
                    }

                    var flareLayer = screenshotObj.GetComponent<FlareLayer>();
                    if (flareLayer != null)
                    {
                        Destroy(flareLayer);
                    }
                }
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
                captureCamera.targetTexture = renderTexture;
                captureCamera.Render();

                var previousActive = RenderTexture.active;
                RenderTexture.active = renderTexture;

                texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();

                captureCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Destroy(renderTexture);
            }
            else
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture(superSize);
            }

            var bytes = texture.EncodeToPNG();
            var fileName = string.Format("{0}_{1:yyyyMMdd_HHmmss}.png", fileNamePrefix, DateTime.Now);
            var path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllBytes(path, bytes);

            foreach (var canvas in hiddenCanvases)
            {
                if (canvas != null)
                {
                    canvas.enabled = true;
                }
            }

            Destroy(texture);
            Debug.Log($"Screenshot saved to: {path}");
        }
    }
}
