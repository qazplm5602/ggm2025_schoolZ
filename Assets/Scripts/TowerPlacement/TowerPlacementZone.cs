using UnityEngine;

/// <summary>
/// 타워 설치 가능 위치 컴포넌트
/// 플레이어가 가까이 오면 UI를 표시합니다
/// </summary>
public class TowerPlacementZone : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float activationRange = 3f; // UI 활성화 범위
    [SerializeField] private Vector3 towerOffset = Vector3.up; // 타워 생성 위치 오프셋

    [Header("상태")]
    [SerializeField] private bool isOccupied = false; // 이 위치에 타워가 있는지

    private Transform player;

    private void Start()
    {
        // 플레이어 찾기 (참조용으로만 사용)
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }

        // Zone들은 이제 거리 체크를 하지 않음
        // 시스템이 Tab 키를 눌렀을 때 직접 확인함
    }

    /// <summary>
    /// 타워 생성 위치 반환
    /// </summary>
    public Vector3 GetTowerPosition()
    {
        return transform.position + towerOffset;
    }

    /// <summary>
    /// 이 위치의 점유 상태 설정
    /// </summary>
    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
    }

    /// <summary>
    /// 이 위치가 사용 가능한지 확인
    /// </summary>
    public bool IsAvailable()
    {
        return !isOccupied;
    }

    private void OnDrawGizmosSelected()
    {
        // 에디터에서 활성화 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRange);

        // 타워 생성 위치 표시
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(GetTowerPosition(), 0.2f);
    }
}
