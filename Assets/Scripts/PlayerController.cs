using Unity.Mathematics;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float Speed;

    private float2 _movementVector = float2.zero;

    private void Update()
    {
        _movementVector.x = Input.GetAxis("Horizontal");
        _movementVector.y = Input.GetAxis("Vertical");

        
    }

    private void FixedUpdate()
    {
        if (_movementVector.y != 0)
            transform.Translate(0, _movementVector.y * Time.fixedDeltaTime, 0);

        else if (_movementVector.x != 0)
            transform.Translate(_movementVector.x * Time.fixedDeltaTime, 0, 0);
    }
}
