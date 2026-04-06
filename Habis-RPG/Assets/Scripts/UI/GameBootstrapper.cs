using System.Collections;
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
                Object.DontDestroyOnLoad(uiGO);
                var ui = uiGO.AddComponent<UIManager>();
                // Wait one frame for Awake() to complete, then initialize
                GameManager.Instance.StartCoroutine(DelayedUIInit(ui));
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

        private static IEnumerator DelayedUIInit(UIManager ui)
        {
            yield return null; // Wait one frame for Awake()
            ui.Initialize();
            Debug.Log("[Habis RPG] UI initialized!");
        }
    }
}
