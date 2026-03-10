using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("ºËĞÄÅäÖÃ")]
    public EnemyData enemyData;
    public Transform player;
    [Header("×´Ì¬")]
    private int currentHealth;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        currentHealth = enemyData.maxHealth;

        // ×Ô¶¯²éÕÒÍæ¼Ò
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    void Update()
    {
        if (isDead || player == null) return;
        ChasePlayer();
    }

    /// <summary>
    /// ×·»÷Íæ¼Ò
    /// </summary>
    void ChasePlayer()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= enemyData.chaseRange)
        {
            Vector2 moveDir = (player.position - transform.position).normalized;
            rb.velocity = moveDir * enemyData.moveSpeed;

            // ·­×ª³¯Ïò
            sr.flipX = moveDir.x < 0;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    /// <summary>
    /// ÊÜ»÷Âß¼­
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"µĞÈËÊ£ÓàÑªÁ¿£º{currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // ÊÜ»÷ÉÁºì
            StartCoroutine(HitFlash());
        }
    }

    /// <summary>
    /// ËÀÍöÂß¼­
    /// </summary>
    void Die()
    {
        isDead = true;
        rb.velocity = Vector2.zero;
        Debug.Log("µĞÈËËÀÍö");
        Destroy(gameObject, 1f);
    }

    /// <summary>
    /// ÊÜ»÷ÉÁºìĞ§¹û
    /// </summary>
    IEnumerator HitFlash()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }

    /// <summary>
    /// ¼ì²âµ¶ÈĞ¹¥»÷
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("AttackCheck"))
        {
            TakeDamage(1);
        }
    }
}