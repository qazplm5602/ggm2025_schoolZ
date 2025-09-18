using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Player : Agent
{
    [Header("게임 오버 설정")]
    [SerializeField] private string enemyTag = "Enemy"; // 적 태그
    [SerializeField] private LayerMask enemyLayer; // 적 레이어 (성능 최적화용)
    [SerializeField] private float gameOverDelay = 5f; // 게임 오버 메시지 표시 시간
    [SerializeField] private float detectionRadius = 1.5f; // 적 감지 반경

    private CharacterController characterController;
    private bool isGameOverTriggered = false; // 중복 게임 오버 방지

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogWarning("CharacterController가 없습니다. 일반 충돌 감지를 사용합니다.");
        }

        // 레이어 마스크가 설정되지 않은 경우 태그 기반으로 찾기
        if (enemyLayer == 0)
        {
            // 기본 Enemy 레이어를 찾거나 생성
            int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
            if (enemyLayerIndex != -1)
            {
                enemyLayer = 1 << enemyLayerIndex;
            }
            else
            {
                Debug.LogWarning("Enemy 레이어가 없습니다. 태그 기반 감지를 사용합니다.");
            }
        }
    }

    private void Update()
    {
        // Character Controller가 있는 경우 매 프레임 충돌 체크
        if (characterController != null && !isGameOverTriggered)
        {
            CheckForEnemyCollision();
        }
    }

    /// <summary>
    /// Character Controller용 적 충돌 감지 (최적화 버전)
    /// </summary>
    private void CheckForEnemyCollision()
    {
        // 레이어 마스크를 사용한 최적화된 감지
        if (enemyLayer != 0)
        {
            // 특정 레이어의 콜라이더만 감지 (성능 우수)
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);

            foreach (var hitCollider in hitColliders)
            {
                // 자신은 제외
                if (hitCollider.gameObject == gameObject) continue;

                // 적인지 추가 확인
                if (IsEnemy(hitCollider.gameObject))
                {
                    TriggerGameOver();
                    return;
                }
            }
        }
        else
        {
            // 레이어 마스크가 없는 경우 일반 감지
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);

            foreach (var hitCollider in hitColliders)
            {
                // 자신은 제외
                if (hitCollider.gameObject == gameObject) continue;

                // 적인지 확인
                if (IsEnemy(hitCollider.gameObject))
                {
                    TriggerGameOver();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 오브젝트가 적인지 확인
    /// </summary>
    private bool IsEnemy(GameObject obj)
    {
        // 태그 확인
        if (obj.CompareTag(enemyTag))
        {
            return true;
        }

        // 컴포넌트 확인
        if (obj.GetComponent<BaseEnemy>() != null)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 게임 오버 트리거 (중복 방지)
    /// </summary>
    private void TriggerGameOver()
    {
        if (isGameOverTriggered) return;

        isGameOverTriggered = true;
        StartCoroutine(HandleGameOver());
    }

    /// <summary>
    /// Character Controller 충돌 콜백 (백업용)
    /// </summary>
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isGameOverTriggered) return;

        if (IsEnemy(hit.gameObject))
        {
            TriggerGameOver();
        }
    }

    /// <summary>
    /// 일반 충돌 감지 (Rigidbody가 있는 경우)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (isGameOverTriggered) return;

        if (IsEnemy(other.gameObject))
        {
            TriggerGameOver();
        }
    }

    /// <summary>
    /// 게임 오버 처리 코루틴
    /// </summary>
    private IEnumerator HandleGameOver()
    {
        // 게임 오버 상태로 변경
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }

        // 패배 메시지 표시
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.ShowWarningMessage("패배! 게임을 다시 시작합니다...", gameOverDelay);
            Debug.Log("패배 메시지를 표시했습니다.");
        }
        else
        {
            Debug.LogError("WaveManager를 찾을 수 없습니다!");
        }

        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(gameOverDelay);

        // GameManager를 통한 게임 재시작 (씬 리로딩 대신 상태 리셋)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            // GameManager가 없는 경우에만 씬 리로딩 사용 (fallback)
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
    }
}
