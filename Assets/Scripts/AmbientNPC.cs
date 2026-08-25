using UnityEngine;

public class AmbientNPC : MonoBehaviour
{
    [SerializeField] private float minimumTurnDelay = 2.5f;
    [SerializeField] private float maximumTurnDelay = 6f;
    [SerializeField] private float maximumTurnAngle = 55f;
    [SerializeField] private float turnSpeed = 65f;

    private Quaternion targetRotation;
    private float nextTurnTime;

    private void OnEnable()
    {
        targetRotation = transform.rotation;
        ScheduleNextTurn();
    }

    private void Update()
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );

        if (Time.time >= nextTurnTime)
        {
            float turnAmount = Random.Range(
                -maximumTurnAngle,
                maximumTurnAngle
            );

            targetRotation =
                transform.rotation *
                Quaternion.Euler(0f, turnAmount, 0f);

            ScheduleNextTurn();
        }
    }

    private void ScheduleNextTurn()
    {
        nextTurnTime = Time.time + Random.Range(
            minimumTurnDelay,
            maximumTurnDelay
        );
    }
}
