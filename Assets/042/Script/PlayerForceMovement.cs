using System.Collections;
using UnityEngine;

public class PlayerForceInput : MonoBehaviour
{
    public Flying playerMovement;

    [Header("Force Settings")]
    public float forwardUpForce = 10f;
    public float sideForce = 8f;

    private Vector3 qForce;
    private Vector3 eForce;

    private bool qPressed = false;
    private bool ePressed = false;

    void Update()
    {
        // 按下 W：給一個向前上的力，1 秒後解除
        if (Input.GetKeyDown(KeyCode.W))
        {
            Vector3 force = playerMovement.transform.forward + playerMovement.transform.up;
            force = force.normalized * forwardUpForce;

            StartCoroutine(ApplyForceForOneSecond(force));
        }

        // 按住 Q：向左施力
        if (Input.GetKeyDown(KeyCode.Q))
        {
            qForce = -playerMovement.transform.right * sideForce;
            playerMovement.AddForce(qForce);
            qPressed = true;
        }

        // 放開 Q：解除向左的力
        if (Input.GetKeyUp(KeyCode.Q) && qPressed)
        {
            playerMovement.RemoveForce(qForce);
            qPressed = false;
        }

        // 按住 E：向右施力
        if (Input.GetKeyDown(KeyCode.E))
        {
            eForce = playerMovement.transform.right * sideForce;
            playerMovement.AddForce(eForce);
            ePressed = true;
        }

        // 放開 E：解除向右的力
        if (Input.GetKeyUp(KeyCode.E) && ePressed)
        {
            playerMovement.RemoveForce(eForce);
            ePressed = false;
        }
    }

    private IEnumerator ApplyForceForOneSecond(Vector3 force)
    {
        playerMovement.AddForce(force);

        yield return new WaitForSeconds(1f);

        playerMovement.RemoveForce(force);
    }
}