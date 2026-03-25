using UnityEngine;

namespace TapMiner.Core
{
    /// <summary>
    /// Minimal bootstrap entry point for the application.
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
        [SerializeField]
        private string bootstrapVersion = "0.1.0";

        private void Awake()
        {
            Debug.Log($"[AppBootstrap] Started v{bootstrapVersion}");
        }
    }
}
