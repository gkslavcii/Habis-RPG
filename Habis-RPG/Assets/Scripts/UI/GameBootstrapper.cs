using UnityEngine;
using HabisRPG.Managers;

namespace HabisRPG.UI
{
    /// <summary>
    /// Auto-initializes the game on scene load.
    /// No need to attach to any GameObject - runs automatically.
    /// </summary>
    public static class GameBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Create GameManager
            if (GameManager.Instance == null)
            {
                var gmGO = new GameObject("GameManager");
                gmGO.AddComponent<GameManager>();
            }

            // Create UIManager
            if (UIManager.Instance == null)
            {
                var uiGO = new GameObject("UIManager");
                var ui = uiGO.AddComponent<UIManager>();
                ui.Initialize();
            }

            // Create EventSystem for UI input
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Debug.Log("[Habis RPG] Game bootstrapped successfully!");
        }
    }
}
