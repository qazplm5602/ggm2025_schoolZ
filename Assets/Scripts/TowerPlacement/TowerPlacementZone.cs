using UnityEngine;

/// <summary>
/// 타워 설치 가능 위치 컴포넌트
/// 플레이어가 가까이 오면 UI를 표시합니다
/// </summary>
public class TowerPlacementZone : MonoBehaviour
{
    [Header("설정")]
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

    /// <summary>
    /// 이 위치에 설치된 타워 반환
    /// </summary>
    public BaseTower GetPlacedTower()
    {
        if (!isOccupied)
            return null;

        // 자식 오브젝트들에서 BaseTower 컴포넌트 찾기
        foreach (Transform child in transform)
        {
            BaseTower tower = child.GetComponent<BaseTower>();
            if (tower != null)
            {
                return tower;
            }
        }

        return null;
    }

    /// <summary>
    /// 점유 상태 확인 (설치된 타워 존재 여부로 자동 판단)
    /// </summary>
    public bool IsOccupied()
    {
        // 설치된 타워가 있으면 점유된 것으로 판단
        return GetPlacedTower() != null;
    }

    private void OnDrawGizmosSelected()
    {
        // 타워 생성 위치 표시 (노란색 원 범위 표시 제거)
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(GetTowerPosition(), 0.2f);
    }
}
