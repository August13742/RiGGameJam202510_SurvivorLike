using UnityEngine;

namespace Survivor.Game
{

    public class GameStateManager : MonoBehaviour
    {

        public static GameStateManager Instance { get; private set; }
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); return;
            }
            Instance = this;
            DontDestroyOnLoad(this);
        }
    }
}