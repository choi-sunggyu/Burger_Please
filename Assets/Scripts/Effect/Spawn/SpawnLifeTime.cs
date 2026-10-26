using UnityEngine;

public class SpawnLifeTime : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, 3f);
    }
}
