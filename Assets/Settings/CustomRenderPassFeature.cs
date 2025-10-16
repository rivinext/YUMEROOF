using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Custom renderer feature that provides a simple blit pass using a configurable material.
/// </summary>
public class CustomRenderPassFeature : ScriptableRendererFeature
{
    [Serializable]
    public class Settings
    {
        [Tooltip("Event that controls when the custom pass is executed.")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        [Tooltip("Material used for the blit operation.")]
        public Material blitMaterial;

        [Tooltip("Optional material pass index. Use -1 for the default pass.")]
        public int blitMaterialPassIndex = -1;
    }

    public Settings settings = new Settings();

    class CustomRenderPass : ScriptableRenderPass
    {
        private readonly Settings settings;
        private RenderTargetIdentifier currentTarget;

        public CustomRenderPass(Settings settings)
        {
            this.settings = settings;
            renderPassEvent = settings.renderPassEvent;
        }

        public void Setup(RenderTargetIdentifier colorTarget)
        {
            currentTarget = colorTarget;
        }

        [Obsolete("ScriptableRenderPass.OnCameraSetup is obsolete. Configure the pass instead.")]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(currentTarget);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings.blitMaterial == null)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(nameof(CustomRenderPass));
            cmd.Blit(currentTarget, currentTarget, settings.blitMaterial, settings.blitMaterialPassIndex);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private CustomRenderPass customPass;

    public override void Create()
    {
        customPass = new CustomRenderPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.blitMaterial == null)
        {
            return;
        }

        customPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(customPass);
    }
}
