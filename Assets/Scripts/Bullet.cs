using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Tooltip("탄환 이동 속도")]
    public float speed = 10f;

    [Tooltip("탄환 생존 시간 (초)")]
    public float lifeTime = 5f;

    [HideInInspector]
    public Vector2 direction = Vector2.right;

    private SpriteRenderer sprite;

    private void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning)
        {
            return;
        }

        if (direction.x >= 0)
        {
            sprite.flipX = false;
        }
        else
        {
            sprite.flipX = true;
        }

        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var manager = GameManager.Instance;
        if (manager == null || !manager.IsGameRunning)
        {
            return;
        }

        if (!other.TryGetComponent(out PlayerMove player))
        {
            return;
        }

        manager.DamagePlayer();
        Destroy(gameObject);
    }
}
