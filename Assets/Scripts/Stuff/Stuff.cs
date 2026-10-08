using UnityEngine;

public enum StuffType
{
    Cube,
    Trash,
    Money
}

public class Stuff : MonoBehaviour
{
    public StuffType Type { get; protected set; }
}