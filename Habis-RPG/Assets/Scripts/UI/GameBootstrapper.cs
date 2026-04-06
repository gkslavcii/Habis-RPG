using UnityEngine;
using HabisRPG.Managers;

namespace HabisRPG.UI
{
    /// <summary>
    /// Game initializer - works both as MonoBehaviour AND static initializer.
    /// Double safety: RuntimeInitializeOnLoadMethod + MonoBehaviour.Start
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        private static bool _initialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StaticBootstrap()
        {
            if (_initialized) return;
            DoBootstrap();
        }

        private void Start()
        {
            if (_initialized) return;
            DoBootstrap();
        }

        private static void DoBootstrap()
        {
            _initialized = true;

            // Create GameManager
            if (GameManager.Instance == null)
            {
                var gmGO = new GameObject("GameManager");
                gmGO.AddComponent<GameManager>();
                Object.DontDestroyOnLoad(gmGO);
            }

            // Create UIManager
            if (UIManager.Instance == null)
            {
                var uiGO = new GameObject("UIManager");
                var ui = uiGO.AddComponent<UIManager>();
                ui.Initialize();
                Object.DontDestroyOnLoad(uiGO);
            }

            // Create EventSystem for UI input
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Object.DontDestroyOnLoad(esGO);
            }

            Debug.Log("[Habis RPG] Game bootstrapped successfully!");
        }
    }
}
