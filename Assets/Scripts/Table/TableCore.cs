using UnityEngine;

public class TableCore : MonoBehaviour
{
    [field: SerializeField] public Transform PushSocket { get; private set; }

    [Header("테이블 상태")]
    [SerializeField] private bool isPlaced = true; // 테이블이 놓여진 상태인지
    [SerializeField] private bool isBeingPushed = false; // 플레이어가 밀고 있는 중인지

    [Header("물리 설정")]
    [SerializeField] private Rigidbody tableRigidbody;

    private void Awake()
    {
        if (tableRigidbody == null)
        {
            tableRigidbody = GetComponent<Rigidbody>();
            if (tableRigidbody == null)
            {
                // Rigidbody가 없으면 자동으로 추가
                tableRigidbody = gameObject.AddComponent<Rigidbody>();
//                Debug.Log("TableCore: Rigidbody 컴포넌트를 자동으로 추가했습니다.");
            }
        }

        // 초기 상태 설정
        UpdatePhysicsState();
    }

    /// <summary>
    /// 테이블을 들기 시작할 때 호출
    /// </summary>
    public void StartPushing()
    {
        isBeingPushed = true;
        UpdatePhysicsState();
    }

    /// <summary>
    /// 테이블을 놓을 때 호출
    /// </summary>
    public void PlaceTable()
    {
        isBeingPushed = false;
        isPlaced = true;
        UpdatePhysicsState();
    }

    /// <summary>
    /// 테이블 물리 상태 업데이트
    /// </summary>
    private void UpdatePhysicsState()
    {
        if (tableRigidbody != null)
        {
            if (isBeingPushed)
            {
                // 밀고 있을 때는 물리 시뮬레이션 비활성화
                tableRigidbody.isKinematic = true;
                tableRigidbody.useGravity = false;
            }
            else
            {
                // 놓여진 상태에서는 물리 시뮬레이션 활성화
                tableRigidbody.isKinematic = false;
                tableRigidbody.useGravity = true;
            }
        }
    }

    /// <summary>
    /// 테이블이 놓여진 상태인지 확인
    /// </summary>
    public bool IsPlaced => isPlaced;

    /// <summary>
    /// 테이블이 밀리고 있는 상태인지 확인
    /// </summary>
    public bool IsBeingPushed => isBeingPushed;
}
