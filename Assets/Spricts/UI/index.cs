using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class index : MonoBehaviour
{
    [Header("引用")]
    public Button entry;                 // 进入按钮
    public RawImage bg;                  // 背景图
    public AudioSource sound;            // 音效
    public VideoPlayer videoPlayer;      // 视频（可选）
    public ParticleSystem[] effects;     // 粒子特效（可多个）
    public RippleEffect rippleEffect;    // 涟漪波纹特效（UI 版）

    [Header("过渡设置")]
    [Tooltip("过渡速率：0.5 约 2 秒完成")]
    public float speed = 0.5f;
    public float bgTargetAlpha = 0f;      // 进入时背景目标透明度
    public float soundTargetVolume = 0f;  // 进入时音量目标值

    [Header("初始状态（Reset 时恢复用）")]
    public float defaultAlpha = 1f;      // 回到界面时背景透明度
    public float defaultVolume = 1f;      // 回到界面时音量
    
    // 运行时状态
    private bool isEntering = false;
    private bool hasEntered = false;

    public void Awake()
    {
        entry.onClick.AddListener(() => StartCoroutine(OnEntryButtonClick()));
    }

    // ==================== 进入逻辑 ====================

    IEnumerator OnEntryButtonClick()
    {
        if (isEntering || hasEntered) yield break;
        isEntering = true;
        entry.interactable = false;

        // 音频：没在播就播；在播就让它继续（不重置进度）
        if (sound != null && !sound.isPlaying)
            sound.Play();

        float startAlpha = bg != null ? bg.color.a : 0f;
        float startVolume = sound != null ? sound.volume : 0f;
        float duration = 1f / Mathf.Max(speed, 0.01f);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float ease = Mathf.SmoothStep(0f, 1f, k);

            // 过渡期间只改 alpha 和 volume，不动其他进度
            if (bg != null)
            {
                Color c = bg.color;
                c.a = Mathf.Lerp(startAlpha, bgTargetAlpha, ease);
                bg.color = c;
            }
            if (sound != null)
                sound.volume = Mathf.Lerp(startVolume, soundTargetVolume, ease);

            yield return null;
        }

        // 精确终值
        if (bg != null) { var c = bg.color; c.a = bgTargetAlpha; bg.color = c; }
        if (sound != null) sound.volume = soundTargetVolume;

        // 读秒结束，才关闭所有组件
        CloseAll();

        hasEntered = true;
        isEntering = false;
        Debug.Log("[index] 读秒结束，组件已关闭");
        this.gameObject.SetActive(false);
    }

    /// <summary>读秒结束后关闭 RawImage / VideoPlayer / AudioSource / 粒子 / 涟漪</summary>
    void CloseAll()
    {
        if (bg != null) bg.enabled = false;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.enabled = false;
        }

        if (sound != null)
        {
            sound.Stop();
            sound.enabled = false;
        }

        if (effects != null)
        {
            foreach (var ps in effects)
            {
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }
        }

        // 涟漪：先清（组件还启用），再停生成，再禁用
        if (rippleEffect != null)
        {
            rippleEffect.ClearRipples();
            rippleEffect.StopRipple();
            rippleEffect.enabled = false;
            var ri = rippleEffect.GetComponent<RawImage>();
            if (ri != null) ri.enabled = false;
        }
    }

    // ==================== 回到 index 界面 ====================

    /// <summary>
    /// 回到本界面：重置音量、视频进度、涟漪进度，并让所有组件重新出现。
    /// 由外部"回到 index"的按钮调用。
    /// </summary>
    public void ResetToEntry()
    {
        StopAllCoroutines();
        isEntering = false;
        hasEntered = false;

        // 1. 背景：启用 + 透明度恢复
        if (bg != null)
        {
            bg.enabled = true;
            Color c = bg.color;
            c.a = defaultAlpha;
            bg.color = c;
        }

        // 2. 视频：启用 + 进度归零 + 从头播
        if (videoPlayer != null)
        {
            videoPlayer.enabled = true;
            videoPlayer.Stop();
            videoPlayer.frame = 0;
            videoPlayer.time = 0;
            videoPlayer.Play();
        }

        // 3. 音效：启用 + 音量恢复 + 进度归零 + 从头播
        if (sound != null)
        {
            sound.enabled = true;
            sound.Stop();
            sound.time = 0f;
            sound.volume = defaultVolume;
            sound.Play();
        }

        // 4. 粒子：启用 + 清空 + 重播
        if (effects != null)
        {
            foreach (var ps in effects)
            {
                if (ps == null) continue;
                ps.gameObject.SetActive(true);
                ps.Clear(true);
                ps.Play();
            }
        }

        // 5. 涟漪：启用 + 清进度 + 重新开始
        if (rippleEffect != null)
        {
            var ri = rippleEffect.GetComponent<RawImage>();
            if (ri != null) ri.enabled = true;
            rippleEffect.enabled = true;
            rippleEffect.ClearRipples();
            rippleEffect.StartRipple();
        }

        // 6. 按钮恢复可点
        if (entry != null)
            entry.interactable = true;

        Debug.Log("[index] 回到 index 界面，音量与进度已重置");
    }
}