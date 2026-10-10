using UnityEngine;

public enum StuffType
{
    Cube,
    Trash,
    Money,
    CubePack
}

public class Stuff : MonoBehaviour
{
    public StuffType Type { get; protected set; }
}