using UnityEngine;

public class CarrierStat : MonoBehaviour
{
    [SerializeField] private int capacity = 3; // 한 번에 옮길 수 있는 양
    public int Capacity => capacity;
}
