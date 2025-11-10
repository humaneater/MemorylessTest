using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class CustomRenderPassFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        public int index;

        public bool memoryless;

        // This class stores the data needed by the RenderGraph pass.
        // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
        public class PassData
        {
            internal TextureHandle DepTextureHandle;
            internal TextureHandle ColTextureHandle;
            internal TextureHandle origin;
        }

        // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
        // It is used to execute draw commands.
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            Blitter.BlitTexture(context.cmd, data.origin, Vector4.one, 0, true);
        }

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "Render Custom Pass";
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(1024, 1024, RenderTextureFormat.Depth, 32);
            RenderTextureDescriptor descriptorc = new RenderTextureDescriptor(1024, 1024);

            resourceData.depthHandle[index] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "memless" + index, true);
            resourceData.colorHandle[index] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptorc, "memlessC" + index, true);

            // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                passData.ColTextureHandle = resourceData.colorHandle[index];
                passData.DepTextureHandle =resourceData.depthHandle[index];
                passData.origin = resourceData.cameraColor;
                builder.SetRenderAttachment(passData.ColTextureHandle, 0);
                builder.SetRenderAttachmentDepth(passData.DepTextureHandle, AccessFlags.WriteAll);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
            
            using (var builder1 = renderGraph.AddRasterRenderPass<PassData>("aaaaa",out var passData))
            {
                passData.ColTextureHandle = resourceData.colorHandle[index];
                passData.DepTextureHandle = resourceData.depthHandle[index];
                passData.origin = resourceData.cameraColor;
                builder1.SetRenderAttachment(passData.ColTextureHandle, 0);
                builder1.SetRenderAttachmentDepth(passData.DepTextureHandle, AccessFlags.WriteAll);
                builder1.AllowPassCulling(false);
                builder1.UseAllGlobalTextures(true);
                if (!memoryless &&passData.ColTextureHandle.IsValid())
                {
                    //builder1.SetGlobalTextureAfterPass(passData.ColTextureHandle, Shader.PropertyToID("memlessC" + index));
                }
                builder1.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }


        // NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    CustomRenderPass m_ScriptablePass;
    public int index1 = 0;
    public bool needMemory = false;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        m_ScriptablePass.index = this.index1;
        m_ScriptablePass.memoryless = needMemory;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_ScriptablePass.memoryless = !m_ScriptablePass.memoryless ;
        renderer.EnqueuePass(m_ScriptablePass);
    }
}