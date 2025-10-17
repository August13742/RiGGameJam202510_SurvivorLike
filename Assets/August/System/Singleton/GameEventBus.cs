using UnityEngine;

namespace Survivor.Game
{


    public class GameEventBus : MonoBehaviour
    {

        public static GameEventBus Instance { get; private set; }
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