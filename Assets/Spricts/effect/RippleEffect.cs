using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 屏幕波纹效果：挂在全屏 RawImage 上，放在 Canvas 最上层。
/// 点击屏幕任意位置产生波纹。
/// </summary>
[RequireComponent(typeof(RawImage))]
public class RippleEffect : MonoBehaviour
{
    [Header("Wave Settings")]
    public AnimationCurve waveform = new AnimationCurve(
        new Keyframe(0.00f, 0.50f, 0, 0),
        new Keyframe(0.05f, 1.00f, 0, 0),
        new Keyframe(0.15f, 0.10f, 0, 0),
        new Keyframe(0.25f, 0.80f, 0, 0),
        new Keyframe(0.35f, 0.30f, 0, 0),
        new Keyframe(0.45f, 0.60f, 0, 0),
        new Keyframe(0.55f, 0.40f, 0, 0),
        new Keyframe(0.65f, 0.55f, 0, 0),
        new Keyframe(0.75f, 0.46f, 0, 0),
        new Keyframe(0.85f, 0.52f, 0, 0),
        new Keyframe(0.99f, 0.50f, 0, 0)
    );

    [Range(0.01f, 1.0f)] public float refractionStrength = 0.5f;
    public Color reflectionColor = Color.gray;
    [Range(0.01f, 1.0f)] public float reflectionStrength = 0.7f;
    [Range(0.1f, 3.0f)] public float waveSpeed = 0.5f;

    [Header("Click Settings")]
    public bool requireMouseClick = true;

    [Header("Shader")]
    public Shader shader;   // 拖入 UI/Ripple Effect

    class Droplet
    {
        public Vector2 position;
        public float time = 1000f;
        public bool active = false;

        public void Reset(Vector2 pos)
        {
            position = pos;
            time = 0f;
            active = true;
        }

        public void Update()
        {
            time += Time.deltaTime;
        }

        public bool IsActive()
        {
            return active && time < 1.0f;
        }

        public Vector4 MakeShaderParameter(float aspect)
        {
            return new Vector4(position.x * aspect, position.y, time, active ? 1f : 0f);
        }
    }

    Droplet[] droplets;
    Texture2D gradTexture;
    Material material;
    RawImage rawImage;

    // 是否允许生成新波纹（供 Stop/Start 控制）
    private bool rippleEnabled = true;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();

        droplets = new Droplet[1];
        droplets[0] = new Droplet();

        gradTexture = new Texture2D(2048, 1, TextureFormat.Alpha8, false);
        gradTexture.wrapMode = TextureWrapMode.Clamp;
        gradTexture.filterMode = FilterMode.Bilinear;
        for (int i = 0; i < gradTexture.width; i++)
        {
            float x = 1.0f / gradTexture.width * i;
            float a = waveform.Evaluate(x);
            gradTexture.SetPixel(i, 0, new Color(a, a, a, a));
        }
        gradTexture.Apply();

        if (shader == null) shader = Shader.Find("UI/Ripple Effect");
        material = new Material(shader);
        material.hideFlags = HideFlags.DontSave;
        material.SetTexture("_GradTex", gradTexture);
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 1;

        rawImage.material = material;
    }

    void Update()
    {
        // 只有启用时才接受点击
        if (rippleEnabled && requireMouseClick && Input.GetMouseButtonDown(0))
        {
            Vector2 viewportPos = new Vector2(
                Input.mousePosition.x / Screen.width,
                Input.mousePosition.y / Screen.height
            );
            droplets[0].Reset(viewportPos);
        }

        foreach (var d in droplets) d.Update();

        // 波纹消失后标记 inactive，避免残影
        if (!droplets[0].IsActive())
            droplets[0].active = false;

        UpdateShaderParameters();
    }

    void UpdateShaderParameters()
    {
        Rect r = rawImage.rectTransform.rect;
        float aspect = Mathf.Abs(r.height) > 0.0001f
            ? Mathf.Abs(r.width / r.height)
            : (float)Screen.width / Screen.height;

        material.SetVector("_Drop1", droplets[0].MakeShaderParameter(aspect));
        material.SetColor("_Reflection", reflectionColor);
        material.SetVector("_Params1", new Vector4(aspect, 1, 1 / waveSpeed, 0));
        material.SetVector("_Params2", new Vector4(1, 1 / aspect, refractionStrength, reflectionStrength));
    }

    // ==================== 外部接口 ====================

    /// <summary>外部调用：在标准化坐标 (0~1) 处产生波纹</summary>
    public void Emit(Vector2 normalizedPos)
    {
        if (!rippleEnabled) return;
        droplets[0].Reset(normalizedPos);
    }

    /// <summary>停止生成新波纹（已有波纹继续衰减）</summary>
    public void StopRipple()
    {
        rippleEnabled = false;
    }

    /// <summary>重新开始生成新波纹</summary>
    public void StartRipple()
    {
        rippleEnabled = true;
    }

    /// <summary>立即清除所有波纹（进度归零）</summary>
    public void ClearRipples()
    {
        foreach (var d in droplets)
        {
            d.active = false;
            d.time = 1000f;
        }
        UpdateShaderParameters();
    }
}