using Il2Cpp;
using UnityEngine;

namespace GBMDK
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class FogDisabler : MonoBehaviour
    {
        private void Update()
        {
            FindObjectOfType<OpaqueSurfaceFogRendererFeature>().SetActive(false);
        }

        private void OnDisable()
        {
            FindObjectOfType<OpaqueSurfaceFogRendererFeature>().SetActive(true);
        }

        private void OnDestroy()
        {
            FindObjectOfType<OpaqueSurfaceFogRendererFeature>().SetActive(true);
        }
    }
}
