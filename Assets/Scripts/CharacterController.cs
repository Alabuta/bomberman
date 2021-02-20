using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterController : MonoBehaviour
{
    private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
    private static readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");

    private static readonly float2 HorizontalMovementMask = new float2(1, 0);
    private static readonly float2 VerticalMovementMask = new float2(0, 1);

    public float Speed;

    private float3 _movementVector = float3.zero;
    private Animator _animator;

    private void Start()
    {
        _animator = gameObject.GetComponent<Animator>();
    }

    private void Update()
    {
        _movementVector.x = Input.GetAxis("Horizontal");
        _movementVector.y = Input.GetAxis("Vertical");

        _movementVector.xy *= math.select(HorizontalMovementMask, VerticalMovementMask, _movementVector.y != 0);

        _animator.SetFloat(HorizontalSpeed, _movementVector.x);
        _animator.SetFloat(VerticalSpeed, _movementVector.y);

        _movementVector = math.round(_movementVector) * Speed;
    }

    private void FixedUpdate()
    {
        transform.Translate(_movementVector * Time.fixedDeltaTime);
    }
}
