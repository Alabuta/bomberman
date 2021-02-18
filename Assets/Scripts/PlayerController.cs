using System;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
    private static readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");

    public float Speed;

    private float2 _movementVector = float2.zero;
    private Animator _animator;

    private void Start()
    {
        _animator = gameObject.GetComponent<Animator>();
    }

    private void Update()
    {
        _movementVector.x = Input.GetAxis("Horizontal");
        _movementVector.y = Input.GetAxis("Vertical");

        _animator.SetFloat(HorizontalSpeed, _movementVector.x);
        _animator.SetFloat(VerticalSpeed, _movementVector.y);

        _movementVector = math.round(_movementVector) * Speed;
    }

    private void FixedUpdate()
    {
        if (_movementVector.y != 0)
            transform.Translate(0, _movementVector.y * Time.fixedDeltaTime, 0);

        else if (_movementVector.x != 0)
            transform.Translate(_movementVector.x * Time.fixedDeltaTime, 0, 0);
    }
}
