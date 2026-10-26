using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HireButton : MonoBehaviour
{
    [SerializeField] EmployeeSpawner spawner;
    [SerializeField] JobRole role = JobRole.Sell;

    void Awake() => GetComponent<Button>().onClick.AddListener(() => spawner.Hire(role));
}