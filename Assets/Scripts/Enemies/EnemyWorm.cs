using UnityEngine;

public class EnemyWorm : EnemyBase
{
    [Header("Patrol Points")]
    public Transform pointA;
    public Transform pointB;
    private Transform currentTarget;

    protected override void Awake()
    {
        base.Awake(); 
        currentTarget = pointB;
        if (pointA != null) pointA.parent = null;
        if (pointB != null) pointB.parent = null;
    }

    protected override void ExecuteAI()
    {
        float distance = Vector2.Distance(transform.position, currentTarget.position);

        if (distance < 0.2f)
        {
            currentTarget = (currentTarget == pointA) ? pointB : pointA;
        }

        float directionX = currentTarget.position.x - transform.position.x;
        if (Mathf.Abs(directionX) > 0.1f)
        {
            Flip(directionX);
        }

        Vector2 direction = (currentTarget.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
    }
}