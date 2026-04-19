using UnityEngine;

public class SpaceyRotator : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second on each local axis.")]
    public Vector3 angularVelocity = new Vector3(7f, 13f, 5f);

    private void Update()
    {
        transform.Rotate(angularVelocity * Time.deltaTime, Space.Self);
    }
}
