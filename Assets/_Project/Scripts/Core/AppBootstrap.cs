using UnityEngine;

namespace TapMiner.Core
{
    /// <summary>
    /// Minimal bootstrap entry point for the application.
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
#pragma warning disable 0414
        [SerializeField]
        private string bootstrapVersion = "0.1.0";
#pragma warning restore 0414
    }
}
