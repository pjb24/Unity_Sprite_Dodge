using UnityEngine;

/// <summary>
/// 탄환을 일정한 간격으로 화면 경계에서 생성하는 스크립트입니다.
/// 생성 간격과 탄환 속도는 시간이 지남에 따라 점점 빨라집니다.
/// </summary>
public class BulletSpawner : MonoBehaviour
{
    [Tooltip("생성할 탄환 프리팹")]
    public GameObject bulletPrefab;

    [Tooltip("기본 탄환 생성 간격 (초)")]
    public float baseSpawnInterval = 1f;

    [Tooltip("최소 탄환 생성 간격 (초)")]
    public float minSpawnInterval = 0.2f;

    [Tooltip("초당 간격 감소량")]
    public float spawnAcceleration = 0.01f;

    [Tooltip("카메라 경계 밖으로 얼마만큼 띄워서 생성할지")]
    public float spawnOffset = 1f;

    [Tooltip("탄환 기본 속도")]
    public float baseBulletSpeed = 5f;

    [Tooltip("초당 탄환 속도 증가량")]
    public float speedAcceleration = 0.1f;

    // 탄환 생성 타이머
    private float timer;
    // 메인 카메라 참조
    private Camera mainCamera;

    /// <summary>
    /// 게임 시작 시 메인 카메라를 찾아서 저장합니다.
    /// </summary>
    private void Start()
    {
        mainCamera = Camera.main;
    }

    /// <summary>
    /// 매 프레임마다 탄환 생성 타이밍을 체크하고, 조건이 맞으면 탄환을 생성합니다.
    /// </summary>
    private void Update()
    {
        // 게임이 진행 중이 아니면 아무 것도 하지 않음
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning)
        {
            return;
        }

        // 타이머에 경과 시간 추가
        timer += Time.deltaTime;

        // 현재 탄환 생성 간격 계산
        float interval = GetCurrentInterval();

        // 아직 생성 간격이 안 됐으면 리턴
        if (timer < interval)
        {
            return;
        }

        // 타이머 초기화 후 탄환 생성
        timer = 0f;
        SpawnBullet(GetCurrentSpeed());
    }

    /// <summary>
    /// 현재 경과 시간에 따라 탄환 생성 간격을 계산합니다.
    /// 시간이 지날수록 간격이 줄어들며, 최소 간격 이하로는 내려가지 않습니다.
    /// </summary>
    /// <returns>현재 탄환 생성 간격(초)</returns>
    private float GetCurrentInterval()
    {
        // 게임 매니저가 있으면 게임 경과 시간 사용, 없으면 씬 로드 후 경과 시간 사용
        float elapsed = GameManager.Instance != null ? GameManager.Instance.ElapsedTime : Time.timeSinceLevelLoad;
        // 생성 간격 계산 (기본 간격 - 경과 시간 * 감소량)
        float interval = baseSpawnInterval - elapsed * spawnAcceleration;
        // 최소 간격 이하로 내려가지 않도록 보정
        return Mathf.Max(minSpawnInterval, interval);
    }

    /// <summary>
    /// 현재 경과 시간에 따라 탄환 속도를 계산합니다.
    /// 시간이 지날수록 탄환 속도가 빨라집니다.
    /// </summary>
    /// <returns>현재 탄환 속도</returns>
    private float GetCurrentSpeed()
    {
        float elapsed = GameManager.Instance != null ? GameManager.Instance.ElapsedTime : Time.timeSinceLevelLoad;
        // 기본 속도 + 경과 시간 * 속도 증가량
        return baseBulletSpeed + elapsed * speedAcceleration;
    }

    /// <summary>
    /// 화면 경계의 랜덤한 위치에서 탄환을 생성하고, 지정된 방향과 속도를 설정합니다.
    /// </summary>
    /// <param name="bulletSpeed">생성할 탄환의 속도</param>
    private void SpawnBullet(float bulletSpeed)
    {
        // 프리팹 또는 카메라가 없으면 경고 출력 후 리턴
        if (bulletPrefab == null || mainCamera == null)
        {
            Debug.LogWarning("BulletPrefab 또는 Camera가 설정되지 않았습니다.");
            return;
        }

        // 카메라의 절반 높이와 절반 너비 계산 (오쏘그래픽 카메라 기준)
        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * mainCamera.aspect;

        // 0: 왼쪽, 1: 오른쪽, 2: 위, 3: 아래 중 랜덤 선택
        int edge = Random.Range(0, 4);
        Vector2 spawnPos;
        Vector2 direction;

        switch (edge)
        {
            case 0: // 왼쪽 경계
                spawnPos = new Vector2(-halfWidth - spawnOffset, Random.Range(-halfHeight, halfHeight));
                direction = Vector2.right;
                break;
            case 1: // 오른쪽 경계
                spawnPos = new Vector2(halfWidth + spawnOffset, Random.Range(-halfHeight, halfHeight));
                direction = Vector2.left;
                break;
            case 2: // 위쪽 경계
                spawnPos = new Vector2(Random.Range(-halfWidth, halfWidth), halfHeight + spawnOffset);
                direction = Vector2.down;
                break;
            default: // 아래쪽 경계
                spawnPos = new Vector2(Random.Range(-halfWidth, halfWidth), -halfHeight - spawnOffset);
                direction = Vector2.up;
                break;
        }

        // 탄환 프리팹을 지정된 위치에 생성
        GameObject newBullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        // Bullet 컴포넌트가 있으면 방향과 속도 설정
        if (newBullet.TryGetComponent(out Bullet bullet))
        {
            bullet.direction = direction;
            bullet.speed = bulletSpeed;
        }
    }
}
