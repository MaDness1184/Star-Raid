using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloningMachine : MonoBehaviour
{
    public static Vector3 location;

    private void Awake()
    {
        location = transform.position;
    }
}
