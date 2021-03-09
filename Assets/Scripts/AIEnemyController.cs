using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class AIEnemyController : MonoBehaviour
{
    public float Speed = 1;
    private float DistanceShell = 0.46f;
    private float DistanceSmall = 0.005f;
    private float DistanceBig = 0.2f;

    private bool UpSmall;
    private bool DownSmall;
    private bool LeftSmall;
    private bool RightSmall;

    private bool UpBig;
    private bool DownBig;
    private bool LeftBig;
    private bool RightBig;

    private int RangeCount;


private void Start()    //four-way verification
    {

        UpBig = Physics2D.Raycast(new Vector3(transform.position.x, transform.position.y + DistanceShell, transform.position.z), transform.TransformDirection(Vector2.up), DistanceBig);
        DownBig = Physics2D.Raycast(new Vector3(transform.position.x, transform.position.y - DistanceShell, transform.position.z), transform.TransformDirection(Vector2.down), DistanceBig);
        LeftBig = Physics2D.Raycast(new Vector3(transform.position.x - DistanceShell, transform.position.y, transform.position.z), transform.TransformDirection(Vector2.left), DistanceBig);
        RightBig = Physics2D.Raycast(new Vector3(transform.position.x + DistanceShell, transform.position.y, transform.position.z), transform.TransformDirection(Vector2.right), DistanceBig);
        
    }

    private void OnCollisionEnter2D(Collision2D  collision)     //four-way verification
    {

        RangeCount = Random.Range(0, 2);


        UpSmall = Physics2D.Raycast(new Vector3(transform.position.x, transform.position.y + DistanceShell, transform.position.z), transform.TransformDirection(Vector2.up), DistanceSmall);
        DownSmall = Physics2D.Raycast(new Vector3(transform.position.x, transform.position.y - DistanceShell, transform.position.z), transform.TransformDirection(Vector2.down), DistanceSmall);
        LeftSmall = Physics2D.Raycast(new Vector3(transform.position.x - DistanceShell, transform.position.y, transform.position.z), transform.TransformDirection(Vector2.left), DistanceSmall);
        RightSmall = Physics2D.Raycast(new Vector3(transform.position.x + DistanceShell, transform.position.y, transform.position.z), transform.TransformDirection(Vector2.right), DistanceSmall);

        UpBig = Physics2D.Raycast(new Vector3(transform.position.x, transform.position.y + DistanceShell, transform.position.z), transform.TransformDirection(Vector2.up), DistanceBig);
        DownBig = Physics2D.Raycast(new Vector3(transform.position.x, transform.position.y - DistanceShell, transform.position.z), transform.TransformDirection(Vector2.down), DistanceBig);
        LeftBig = Physics2D.Raycast(new Vector3(transform.position.x - DistanceShell, transform.position.y, transform.position.z), transform.TransformDirection(Vector2.left), DistanceBig);
        RightBig = Physics2D.Raycast(new Vector3(transform.position.x + DistanceShell, transform.position.y, transform.position.z), transform.TransformDirection(Vector2.right), DistanceBig);


        if (UpSmall)         // bounce
        {
            transform.Translate(0, -Speed * Time.deltaTime, 0);
        }

        if (DownSmall)
        {
            transform.Translate(0, Speed * Time.deltaTime, 0);
        }

        if (LeftSmall)
        {
            transform.Translate(Speed * Time.deltaTime, 0, 0);

        }
        if (RightSmall)
        {
            transform.Translate(-Speed * Time.deltaTime, 0, 0);
        }        

    }
    
    private void FixedUpdate()
    {


        if (UpBig & DownBig & LeftBig & RightBig)   // waiting for passage and standing 
        {
            UpBig = Physics2D.Raycast(new Vector3(transform.position.x, transform.position.y + DistanceShell, transform.position.z), transform.TransformDirection(Vector2.up), DistanceBig);
            DownBig = Physics2D.Raycast(new Vector3(transform.position.x, transform.position.y - DistanceShell, transform.position.z), transform.TransformDirection(Vector2.down), DistanceBig);
            LeftBig = Physics2D.Raycast(new Vector3(transform.position.x - DistanceShell, transform.position.y, transform.position.z), transform.TransformDirection(Vector2.left), DistanceBig);
            RightBig = Physics2D.Raycast(new Vector3(transform.position.x + DistanceShell, transform.position.y, transform.position.z), transform.TransformDirection(Vector2.right), DistanceBig);

            if (!UpBig)
                transform.Translate(0, Speed * Time.deltaTime, 0);
            else if (!DownBig)
                transform.Translate(0, -Speed * Time.deltaTime, 0);
            else if (!LeftBig)
                transform.Translate(Speed * Time.deltaTime, 0, 0);
            else if (!RightBig)
                transform.Translate(-Speed * Time.deltaTime, 0, 0);



        }
        //start move
        if (!UpSmall & !DownSmall & !LeftSmall & !RightSmall)
        {
            if(!UpBig)
                transform.Translate(0, Speed * Time.deltaTime, 0);
            else if (!DownBig)
                transform.Translate(0, -Speed * Time.deltaTime, 0);
            else if (!LeftBig)
                transform.Translate(Speed * Time.deltaTime, 0, 0);
            else if (!RightBig)
                transform.Translate(-Speed * Time.deltaTime, 0, 0);
        }




        if (UpSmall & !LeftBig & !RightBig || DownSmall & !LeftBig & !RightBig)     // select random horisontal side, after hit
        {
            if (RangeCount == 0)
            {
                transform.Translate(Speed * Time.deltaTime, 0, 0);

            }
            else
                transform.Translate(-Speed * Time.deltaTime, 0, 0);

        }

        else if (LeftSmall & !UpBig & !DownBig || RightSmall & !UpBig & !DownBig)        // select random vertical side, after hit
        {
            if (RangeCount == 0)
            {
                transform.Translate(0, Speed * Time.deltaTime, 0);

            }
            else
                transform.Translate(0, -Speed * Time.deltaTime, 0);

        }

        else if ((UpSmall & !LeftBig || UpSmall & !RightBig) || (DownSmall & !LeftBig || DownSmall & !RightBig))     // select horisontal side, after hit
        {
            if (!LeftBig)
            {
                transform.Translate(-Speed * Time.deltaTime, 0, 0);
            }
            else
                transform.Translate(Speed * Time.deltaTime, 0, 0);
        }

        else if ((LeftSmall & !UpBig || LeftSmall & !DownBig) || (RightSmall & !UpBig || RightSmall & !DownBig))     // select horisontal side, after hit
        {
            if (!UpBig)
            {
                transform.Translate(0, Speed * Time.deltaTime, 0);
            }
            else
                transform.Translate(0, -Speed * Time.deltaTime, 0);
        }

        /// тупики
        ///
        else if (UpSmall & LeftBig & RightBig & !DownBig)
        {
            transform.Translate(0, -Speed * Time.deltaTime, 0);

        }
        else if (DownSmall & LeftBig & RightBig & !UpBig)
        {
            transform.Translate(0, Speed * Time.deltaTime, 0);

        }
        else if (LeftSmall & UpBig & DownBig & !RightBig)
        {
            transform.Translate(Speed * Time.deltaTime, 0, 0);

        }
        else if (RightSmall & UpBig & DownBig & !LeftBig)
        {
            transform.Translate(-Speed * Time.deltaTime, 0, 0);

        }

        //  transform.Translate(0, Speed * Time.deltaTime,0);       // up

        //  transform.Translate(0, -Speed * Time.deltaTime, 0);     //down

        //  transform.Translate(Speed * Time.deltaTime, 0, 0);      //right

        //  transform.Translate(-Speed * Time.deltaTime, 0, 0);     //left


        Debug.DrawLine(new Vector3(transform.position.x, transform.position.y + 0.7f, transform.position.z), new Vector3(transform.position.x, transform.position.y + DistanceBig, transform.position.z), Color.red);
        Debug.DrawLine(new Vector3(transform.position.x, transform.position.y - 0.7f, transform.position.z), new Vector3(transform.position.x, transform.position.y - DistanceBig, transform.position.z), Color.red);
        Debug.DrawLine(new Vector3(transform.position.x - 0.7f, transform.position.y, transform.position.z), new Vector3(transform.position.x - DistanceBig, transform.position.y, transform.position.z), Color.red);
        Debug.DrawLine(new Vector3(transform.position.x + 0.7f, transform.position.y, transform.position.z), new Vector3(transform.position.x + DistanceBig, transform.position.y, transform.position.z), Color.red);

    }



}
