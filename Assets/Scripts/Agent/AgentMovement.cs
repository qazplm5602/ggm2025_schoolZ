using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class AgentMovement : MonoBehaviour, IAgentComponent
{
    [Header("Movement Settings")]
    [SerializeField] public float moveSpeed = 3f;
    [SerializeField] protected float rotationSpeed = 5f;
    [SerializeField] protected float stoppingDistance = 0.1f;

    [Header("NavMesh Settings")]
    [SerializeField] public bool useNavMesh = true; // Enemy는 기본적으로 NavMesh 사용

    protected NavMeshAgent navMeshAgent;
    protected Agent agent;
    protected bool isMoving = false;
    protected Vector3 targetPosition;

    public virtual void InitAgent(Agent agent)
    {
        this.agent = agent;

        // NavMeshAgent 설정
        if (useNavMesh)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            if (navMeshAgent == null)
            {
                navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
            }

            // NavMeshAgent 초기 설정
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.angularSpeed = rotationSpeed * 500f; // NavMeshAgent는 degree 단위 - 매우 빠른 회전
            navMeshAgent.stoppingDistance = stoppingDistance;
            navMeshAgent.autoBraking = false; // 도착 지점에서 속도 감속 방지

            // Agent 간 충돌 회피 비활성화 - 서로 무시하고 직선 이동
            navMeshAgent.radius = 0.5f; // Agent 반지름
            navMeshAgent.height = 2.0f; // Agent 높이
            navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance; // 충돌 회피 비활성화
            navMeshAgent.avoidancePriority = 50; // 기본 우선순위
            navMeshAgent.acceleration = moveSpeed * 50f; // 매우 빠른 가속도로 즉시 최고 속도 도달
        }
    }

    protected virtual void Update()
    {
        // 이동 관련 업데이트 로직 (하위 클래스에서 구현)

        // NavMeshAgent 속도 일정하게 유지 (좁은 길에서도 느려지지 않도록)
        if (useNavMesh && navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            // 매 프레임마다 속도를 강제로 설정하여 일정하게 유지
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.acceleration = moveSpeed * 50f; // 매우 빠른 가속도로 즉시 최고 속도 도달
            navMeshAgent.angularSpeed = rotationSpeed * 500f; // 매우 빠른 회전
        }
    }
    // 외부에서 호출할 수 있는 이동 메소드들
    public void SetDestination(Vector3 destination)
    {
        targetPosition = destination;

        if (useNavMesh && navMeshAgent != null)
        {
            // NavMeshAgent가 NavMesh 위에 있는지 확인
            if (navMeshAgent.isOnNavMesh && navMeshAgent.isActiveAndEnabled)
            {
                try
                {
                    navMeshAgent.SetDestination(destination);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[{gameObject.name}] AgentMovement SetDestination 실패: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] NavMeshAgent가 NavMesh 위에 없거나 비활성화됨");
            }
        }
    }


    public void StopMovement()
    {
        if (useNavMesh && navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
        }
        isMoving = false;
    }

    public void ResumeMovement()
    {
        if (useNavMesh && navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = false;
        }
        isMoving = true;
    }

    // 이동 모드 설정 메소드들
    public void EnableNavMesh()
    {
        useNavMesh = true;
        SetupNavMeshAgent();
    }

    public void DisableNavMesh()
    {
        useNavMesh = false;
        if (navMeshAgent != null)
        {
            Destroy(navMeshAgent);
            navMeshAgent = null;
        }
    }

    public void SetMovementMode(bool useNavMeshMode)
    {
        if (useNavMeshMode)
        {
            EnableNavMesh();
        }
        else
        {
            DisableNavMesh();
        }
    }

    private void SetupNavMeshAgent()
    {
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            if (navMeshAgent == null)
            {
                navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
            }
        }

        // NavMeshAgent 초기 설정
        navMeshAgent.speed = moveSpeed;
        navMeshAgent.angularSpeed = rotationSpeed * 500f; // 매우 빠른 회전
        navMeshAgent.stoppingDistance = stoppingDistance;
        navMeshAgent.autoBraking = false; // 즉시 최고 속도 도달을 위해 브레이킹 비활성화
        navMeshAgent.acceleration = moveSpeed * 50f; // 매우 빠른 가속도
    }

    // 현재 이동 모드 확인
    public bool IsUsingNavMesh => useNavMesh && navMeshAgent != null;

    /// <summary>
    /// NavMeshAgent의 속도를 현재 moveSpeed로 업데이트
    /// </summary>
    public void UpdateNavMeshSpeed()
    {
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = moveSpeed;
        }
    }

    // 오버라이드 가능한 콜백 메소드들
    protected virtual void OnReachedDestination()
    {
    }

}
