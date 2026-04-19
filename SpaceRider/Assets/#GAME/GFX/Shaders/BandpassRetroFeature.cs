using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class BandpassRetroFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material material;

        [Range(2f, 32f)]  public float bandCount          = 8f;
        [Range(0f, 1f)]   public float scanlineStrength   = 0.3f;
        [Range(50f, 600f)]public float scanlineFrequency  = 200f;
        [Range(0f, 2f)]   public float vignetteStrength   = 0.5f;
        [Range(0f, 3f)]   public float chromaShift        = 0f;
    }

    public Settings settings = new Settings();

    BandpassRetroPass _pass;

    public override void Create()
    {
        _pass = new BandpassRetroPass(settings)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null) return;
        if (renderingData.cameraData.cameraType == CameraType.Preview) return;

        _pass.UpdateMaterial();
        renderer.EnqueuePass(_pass);
    }

    // ── Pass ────────────────────────────────────────────────────────────────

    class BandpassRetroPass : ScriptableRenderPass
    {
        static readonly int _BandCount         = Shader.PropertyToID("_BandCount");
        static readonly int _ScanlineStrength  = Shader.PropertyToID("_ScanlineStrength");
        static readonly int _ScanlineFrequency = Shader.PropertyToID("_ScanlineFrequency");
        static readonly int _VignetteStrength  = Shader.PropertyToID("_VignetteStrength");
        static readonly int _ChromaShift       = Shader.PropertyToID("_ChromaShift");

        readonly Settings _settings;

        public BandpassRetroPass(Settings settings) => _settings = settings;

        public void UpdateMaterial()
        {
            var m = _settings.material;
            m.SetFloat(_BandCount,         _settings.bandCount);
            m.SetFloat(_ScanlineStrength,  _settings.scanlineStrength);
            m.SetFloat(_ScanlineFrequency, _settings.scanlineFrequency);
            m.SetFloat(_VignetteStrength,  _settings.vignetteStrength);
            m.SetFloat(_ChromaShift,       _settings.chromaShift);
        }

        class PassData { public TextureHandle src; public Material mat; }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer) return;

            TextureHandle src = resourceData.activeColorTexture;

            var desc = renderGraph.GetTextureDesc(src);
            desc.name        = "_BandpassRetroTmp";
            desc.clearBuffer = false;
            TextureHandle dst = renderGraph.CreateTexture(desc);

            using var builder = renderGraph.AddRasterRenderPass<PassData>("BandpassRetro", out var data);
            data.src = src;
            data.mat = _settings.material;

            builder.UseTexture(src);
            builder.SetRenderAttachment(dst, 0);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), d.mat, 0));

            resourceData.cameraColor = dst;
        }
    }
}
