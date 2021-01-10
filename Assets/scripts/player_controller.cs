using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player_controller : MonoBehaviour
{

    public int speed;
    ///public Transform position;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float move_x = Input.GetAxis("Horizontal");
        float move_y = Input.GetAxis("Vertical");

        transform.Translate(move_x * speed * Time.deltaTime, move_y * speed * Time.deltaTime, 0);
    }
}
