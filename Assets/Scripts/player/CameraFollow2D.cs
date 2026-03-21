using UnityEngine;

/// <summary>
/// 2D 俯视角：摄像机跟随目标（通常是玩家）。挂在 Main Camera 上。
/// 使用 SmoothDamp 比 Lerp 更顺滑、带一点惯性感。
/// </summary>
[DefaultExecutionOrder(100)]
public class CameraFollow2D : MonoBehaviour
{
    [Tooltip("跟随目标；为空时会在运行时按 targetTag 查找")]
    public Transform target;

    [Tooltip("当 target 为空时，用该 Tag 查找（默认 Player）")]
    public string targetTag = "Player";

    [Tooltip("相对目标的偏移（2D 相机通常 z 为负，例如 -10）")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Tooltip("平滑时间（秒）：越大镜头越“软”、跟随越慢；越小越紧。0 = 每帧直接对齐目标。")]
    [Min(0f)]
    public float smoothTime = 0.22f;

    [Tooltip("可选：限制相机最大移动速度（世界单位/秒），避免目标瞬移时镜头甩得太猛。0 = 不限制")]
    [Min(0f)]
    public float maxFollowSpeed = 0f;

    /// <summary>SmoothDamp 内部速度缓冲，勿手动改</summary>
    private Vector3 _smoothVelocity;

    private void Start()
    {
        TryAssignTarget();
        if (target != null && smoothTime <= 0f)
        {
            SnapToTarget();
        }
    }

    private void LateUpdate()
    {
        TryAssignTarget();

        if (target == null)
            return;

        Vector3 desired = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            offset.z);

        if (smoothTime <= 0f)
        {
            transform.position = desired;
            _smoothVelocity = Vector3.zero;
            return;
        }

        if (maxFollowSpeed > 0f)
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _smoothVelocity, smoothTime, maxFollowSpeed, Time.deltaTime);
        else
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _smoothVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);

        // z 始终用期望值，避免 SmoothDamp 在 z 上产生漂移
        Vector3 p = transform.position;
        p.z = desired.z;
        transform.position = p;
    }

    /// <summary>切换目标或读档时可调用，避免镜头从远处慢慢飘过来</summary>
    public void SnapToTarget()
    {
        if (target == null)
            return;
        transform.position = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            offset.z);
        _smoothVelocity = Vector3.zero;
    }

    private void TryAssignTarget()
    {
        if (target != null || string.IsNullOrEmpty(targetTag))
            return;

        var go = GameObject.FindGameObjectWithTag(targetTag);
        if (go != null)
            target = go.transform;
    }
}
