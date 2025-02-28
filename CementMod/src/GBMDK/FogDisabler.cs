using Il2Cpp;
using UnityEngine;

namespace GBMDK
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class FogDisabler : MonoBehaviour
    {
        private void Update()
        {
            var feature = FindObjectOfType<OpaqueSurfaceFogRendererFeature>();

            if (feature)
                feature.SetActive(false);
        }

        private void OnDisable()
        {
            var feature = FindObjectOfType<OpaqueSurfaceFogRendererFeature>();

            if (feature)
                feature.SetActive(false);
        }

        private void OnDestroy()
        {
            var feature = FindObjectOfType<OpaqueSurfaceFogRendererFeature>();

            if (feature)
                feature.SetActive(false);
        }
    }
}
