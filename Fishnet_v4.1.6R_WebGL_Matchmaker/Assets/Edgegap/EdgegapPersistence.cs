// Keeps the "Edgegap" GameObject alive across FishNet scene changes (MainMenu -> BattleScene -> MainMenu).
// Without it the server lifecycle (self-stop) would be destroyed when the server loads the online scene,
// and returning to the menu would create a second matchmaking client.
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public class EdgegapPersistence : MonoBehaviour
{
    private static EdgegapPersistence instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // The menu scene was loaded again: keep the original instance and its matchmaking state.
            DestroyImmediate(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
