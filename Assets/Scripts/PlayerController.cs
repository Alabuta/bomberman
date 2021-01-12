using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public int speed;
    public Transform PlayerPosition;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            transform.Translate(-speed, 0, 0);

        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            transform.Translate(speed, 0, 0);

        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            transform.Translate(0, speed, 0);

        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            transform.Translate(0, -speed, 0);

        }








        /*
        var moveX = Input.GetAxis("Horizontal");
        var moveY = Input.GetAxis("Vertical");

        gameObject.SetActive(false);

        transform.Translate(moveX * speed * Time.deltaTime, moveY * speed * Time.deltaTime, 0);
        */

    }
}
