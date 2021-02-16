using Unity.Mathematics;
using UnityEngine;

public class AIEnemyController : MonoBehaviour
{
    public float Speed = 1;
    private bool Hit = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {


        if (Hit == true)
        {

            Speed = +Speed;
            Hit = false;

        }

        else
        {
            Speed = -Speed;
            Hit = true;
        }


    }

    void Update()
    {
        print(Hit);
        print(Speed);
    }


    
    private void FixedUpdate()
    {

        if (Hit == false)
        {
            transform.Translate(0, Speed * Time.fixedDeltaTime, 0);

        }


        if (Hit == true)
        {
            transform.Translate(Speed * Time.fixedDeltaTime, 0, 0);

        }

    }
    


}
