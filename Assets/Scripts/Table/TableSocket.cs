using System;
using UnityEngine;

public class TableSocket : MonoBehaviour
{
    [SerializeField] private InputSO input;
    private TableCore currentTable;
    private Collider[] overlapResult = new Collider[1];
    [SerializeField] private LayerMask detectLayer;
    [SerializeField] private Transform detectPoint;
    [SerializeField] private Vector3 detectSize;

    void OnEnable()
    {
        input.OnInteractPress += HandleInteract;
    }

    void OnDisable()
    {
        input.OnInteractPress -= HandleInteract;
    }

    public void AttachNearTable()
    {
        // detectPoint가 설정되지 않은 경우 경고 표시
        if (detectPoint == null)
        {
            Debug.LogWarning("TableSocket: detectPoint가 설정되지 않았습니다. Inspector에서 detectPoint를 설정해주세요.");
            return;
        }

        int count = Physics.OverlapBoxNonAlloc(detectPoint.position, detectSize, overlapResult, detectPoint.rotation, detectLayer);
        if (count == 0)
        {
            Debug.Log("근처에 테이블이 없습니다.");
            return;
        }

        Collider tableCol = overlapResult[0];
        TableCore tableCore = tableCol.GetComponent<TableCore>();

        if (tableCore != null)
        {
            currentTable = tableCore;
            currentTable.StartPushing();
            Debug.Log("테이블을 밀기 시작했습니다.");
        }
        else
        {
            Debug.LogWarning("발견된 오브젝트에 TableCore 컴포넌트가 없습니다.");
        }
    }

    void Update()
    {
        if (!currentTable) return;

        // 테이블이 밀리고 있는 상태일 때만 위치 업데이트
        if (currentTable.IsBeingPushed)
        {
            Transform pushSocket = currentTable.PushSocket;

            // PushSocket이 설정되지 않은 경우 기본 위치 사용
            if (pushSocket == null)
            {
                Debug.LogWarning("TableSocket: PushSocket이 설정되지 않았습니다. 테이블 중심을 기준으로 계산합니다.");
                // 플레이어 앞 1미터 위치에 테이블 배치
                Vector3 tablePos = transform.position + transform.forward * 1f;
                currentTable.transform.position = tablePos;
                currentTable.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
                return;
            }

            // 플레이어의 위치와 회전
            Vector3 targetPos = transform.position;
            Quaternion targetRot = transform.rotation;

            // 가장 간단한 방식: PushSocket을 플레이어 앞에 고정
            Vector3 targetSocketPos = targetPos + targetRot * new Vector3(0, 0, 0.5f);

            // 현재 PushSocket의 월드 위치
            Vector3 currentSocketPos = pushSocket.position;

            // PushSocket이 목표 위치로 이동하도록 테이블 전체 이동
            Vector3 moveOffset = targetSocketPos - currentSocketPos;
            currentTable.transform.position += moveOffset;

            // 테이블 회전 설정
            currentTable.transform.rotation = Quaternion.Euler(0, targetRot.eulerAngles.y, 0);

            // 디버그: 값 확인 (필요시 주석 해제)
            // Debug.Log($"Target Socket: {targetSocketPos}, Current Socket: {currentSocketPos}, Move: {moveOffset}");
        }
    }

    private void HandleInteract()
    {
        if (currentTable)
        {
            // 테이블이 연결되어 있으면 놓기
            PlaceCurrentTable();
        }
        else
        {
            // 테이블이 없으면 주변 테이블 찾기 및 연결
            AttachNearTable();
        }
    }

    /// <summary>
    /// 현재 연결된 테이블을 놓는 메소드
    /// </summary>
    private void PlaceCurrentTable()
    {
        if (currentTable == null) return;

        // 테이블 놓기
        currentTable.PlaceTable();

        // 연결 해제
        currentTable = null;

        Debug.Log("테이블을 놓았습니다.");

        // 시각적 피드백 (옵션)
        ShowPlacementEffect();
    }

    /// <summary>
    /// 테이블 놓기 효과 표시
    /// </summary>
    private void ShowPlacementEffect()
    {
        // 간단한 시각적 효과
        StartCoroutine(PlacementEffectCoroutine());
    }

    /// <summary>
    /// 테이블 놓기 효과 코루틴
    /// </summary>
    private System.Collections.IEnumerator PlacementEffectCoroutine()
    {
        // 효과 지속 시간
        float effectDuration = 0.5f;
        float elapsed = 0f;

        while (elapsed < effectDuration)
        {
            elapsed += Time.deltaTime;

            // 점진적인 색상 변화 (초록색으로)
            float t = elapsed / effectDuration;
            Color effectColor = Color.Lerp(Color.green, Color.white, t);

            // 현재 연결된 테이블이 없으므로 마지막으로 놓은 테이블의 위치에 효과 표시
            // 실제 구현에서는 파티클 시스템이나 광원 효과를 사용할 수 있습니다.

            yield return null;
        }

        Debug.Log("테이블 놓기 효과 완료");
    }

    void OnDrawGizmos()
    {
        // detectPoint가 설정되지 않은 경우 Gizmos 표시하지 않음
        if (detectPoint == null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, detectSize);
            Gizmos.color = Color.white;
            return;
        }

        // 테이블 감지 영역 표시
        if (currentTable != null && currentTable.IsBeingPushed)
        {
            // 테이블을 밀고 있을 때는 파란색으로 표시
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(detectPoint.position, detectSize);
            Gizmos.color = Color.white;
        }
        else
        {
            // 일반 상태에서는 빨간색으로 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(detectPoint.position, detectSize);
            Gizmos.color = Color.white;
        }

        // 연결된 테이블이 있으면 선으로 연결 표시
        if (currentTable != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTable.transform.position);
            Gizmos.color = Color.white;
        }
    }
}
