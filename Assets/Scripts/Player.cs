using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private bool facingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Move(Vector2 direction)
    {
        rb.velocity = direction * -moveSpeed;

        // 방향 전환
        if (direction.x > 0 && facingRight)
        {
            Flip();
        }
        else if (direction.x < 0 && !facingRight)
        {
            Flip();
        }
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}