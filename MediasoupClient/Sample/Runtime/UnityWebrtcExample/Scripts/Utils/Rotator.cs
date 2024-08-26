using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField]
    private Vector3 _rotationAngle;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(_rotationAngle);
    }
}
