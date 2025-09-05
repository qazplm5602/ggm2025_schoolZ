using UnityEngine;

public class DoVFX : MonoBehaviour
{
    public ParticleSystem VFX;
    public void StartVFX()
    {
        VFX.Play();
    }
}
