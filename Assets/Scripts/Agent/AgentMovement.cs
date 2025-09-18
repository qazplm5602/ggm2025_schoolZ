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
    [SerializeField] protected bool useNavMesh = true;

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
            navMeshAgent.angularSpeed = rotationSpeed * 100f; // NavMeshAgent는 degree 단위
            navMeshAgent.stoppingDistance = stoppingDistance;
            navMeshAgent.autoBraking = true;
        }
    }

    protected virtual void Update()
    {
        // 이동 관련 업데이트 로직 (하위 클래스에서 구현)
    }
    // 외부에서 호출할 수 있는 이동 메소드들
    public void SetDestination(Vector3 destination)
    {
        targetPosition = destination;

        if (useNavMesh && navMeshAgent != null)
        {
            navMeshAgent.SetDestination(destination);
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
        navMeshAgent.angularSpeed = rotationSpeed * 100f;
        navMeshAgent.stoppingDistance = stoppingDistance;
        navMeshAgent.autoBraking = true;
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
