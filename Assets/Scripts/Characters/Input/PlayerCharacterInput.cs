using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerCharacterInput : MonoBehaviour
{
    private Animator CharAnimator = null;
    private Rigidbody2D CharRigBody2D = null;

    private float MoveSpeed = 3f;
    private Vector2 MoveInput = default;

    private static readonly int DirectionX = Animator.StringToHash("DirectionX");
    private static readonly int DirectionY = Animator.StringToHash("DirectionY");

    private void Awake()
    {
        CharRigBody2D = GetComponentInChildren<Rigidbody2D>();
        CharAnimator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // 처음에는 정면을 바라본다.
        CharAnimator.SetFloat(DirectionX, 0f);
        CharAnimator.SetFloat(DirectionY, -1f);
    }

    public void OnMove(InputValue value)
    {
        // var를 붙이면 같은 이름의 지역 변수가 생겨 필드에 저장되지 않는다.
        // 그 탓에 입력을 받아도 캐릭터가 움직이지 않았다.
        MoveInput = Vector2.ClampMagnitude(value.Get<Vector2>(), 1f);
    }

    private void Update()
    {
        // 입력이 없어도 마지막으로 바라본 방향을 유지한다.
        if (MoveInput.sqrMagnitude < 0.001f)
        { 
            return;
        }

        // 게임패드 입력도 가장 가까운 8방향으로 맞춘다.
        var angle = Mathf.Atan2(MoveInput.y, MoveInput.x);
        var step = Mathf.PI / 4f;
        var snappedAngle = Mathf.Round(angle / step) * step;

        CharAnimator.SetFloat(DirectionX, Mathf.Cos(snappedAngle));
        CharAnimator.SetFloat(DirectionY, Mathf.Sin(snappedAngle));
    }

    private void FixedUpdate()
    {
        CharRigBody2D.linearVelocity = MoveInput * MoveSpeed;

        CharAnimator.SetFloat("Speed", CharRigBody2D.linearVelocity.magnitude);
    }

    private void OnDisable()
    {
        MoveInput = Vector2.zero;

        if (CharRigBody2D != null)
        { 
            CharRigBody2D.linearVelocity = Vector2.zero;
        }
    }
}