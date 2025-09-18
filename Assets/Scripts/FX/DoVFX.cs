using UnityEngine;
using UnityEngine.Events;

public class DoVFX : MonoBehaviour
{
    public ParticleSystem VFX;

    // 이벤트 시스템
    [Header("Events")]
    public UnityEvent onVFXStart;     // VFX 시작 시 호출


    // VFX 시작 (이벤트 트리거)
    public void StartVFX()
    {
        if (VFX != null)
        {
            VFX.Play();
        }
        onVFXStart?.Invoke(); // 이벤트 호출
    }
}
