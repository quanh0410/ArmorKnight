using UnityEngine;
using System.Collections;

public class EnemyMushroom : EnemyBase
{
    private enum MushroomState { Sleeping, WakingUp, Patrolling }
    private MushroomState currentState;

    [Header("Detection Settings")]
    public float detectionRange = 4f;

    [Header("Patrol Settings")]
    public Transform pointA;
    public Transform pointB;
    private Transform currentPatrolTarget;

    [Header("Animation Settings")]
    public float wakeUpAnimTime = 1f;

    protected override void Awake()
    {
        base.Awake(); // Lấy tất cả component từ lớp cha

        if (pointA != null) pointA.parent = null;
        if (pointB != null) pointB.parent = null;
        currentPatrolTarget = pointB;

        currentState = MushroomState.Sleeping;
        anim.SetBool("isWalking", false);
    }

    protected override void ExecuteAI()
    {
        switch (currentState)
        {
            case MushroomState.Sleeping:
                CheckForPlayer();
                break;
            case MushroomState.WakingUp:
                StopMovement(); 
                break;
            case MushroomState.Patrolling:
                PatrolLogic();
                break;
        }
    }

    private void CheckForPlayer()
    {
        if (player == null) return;
        if (Vector2.Distance(transform.position, player.position) <= detectionRange)
        {
            currentState = MushroomState.WakingUp;
            anim.SetTrigger("WakeUp");
            StartCoroutine(WakingUpComplete());
        }
    }

    private IEnumerator WakingUpComplete()
    {
        yield return new WaitForSeconds(wakeUpAnimTime);
        currentState = MushroomState.Patrolling;
        anim.SetBool("isWalking", true);
    }

    private void PatrolLogic()
    {
        if (currentPatrolTarget == null) return;

        float distanceToTarget = Vector2.Distance(transform.position, currentPatrolTarget.position);

        if (distanceToTarget < 0.2f)
        {
            currentPatrolTarget = (currentPatrolTarget == pointA) ? pointB : pointA;
        }

        float directionX = currentPatrolTarget.position.x - transform.position.x;
        if (Mathf.Abs(directionX) > 0.1f)
        {
            Flip(directionX);
        }

        Vector2 direction = (currentPatrolTarget.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }
    }
}