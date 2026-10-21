using UnityEngine.InputSystem;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject objectPrefab;
    [SerializeField] private ParticleSystem smokePrefab;
    [SerializeField] private Transform spawnPoint;

    public void Spawn()
    {
        Vector3 position = spawnPoint.position;

        // 1. 연기 생성
        ParticleSystem smoke =
            Instantiate(smokePrefab, position, Quaternion.identity); //한 번에 12개 파티클 뿌리도록 했으니 하나만 생성해도 되겠군

        smoke.Play();

        // 2. 오브젝트 생성
        GameObject spawnedObject = Instantiate(
            objectPrefab,
            position,
            Quaternion.identity
        );
        
        // 3. 등장 애니메이션
        if (spawnedObject.TryGetComponent(out SpawnPopEffect popEffect))
        {
            popEffect.Play();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Spawn();
        }
    }
}
