using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserMoveScript : MonoBehaviour
{
    public static HashSet<BoxCollider> boxes = new HashSet<BoxCollider>();
    public static bool moving = true;
    
    void Start()
    {
        
    }

    void Update()
    {
        if (moving)
        {
            GetComponent<Rigidbody>().AddForce(10 * (Input.GetAxis("Horizontal") * Camera.main.transform.right + Input.GetAxis("Vertical") * Camera.main.transform.forward), ForceMode.Acceleration);
        } else
        {
            GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger)
            return;

        if (other.GetType() == typeof(BoxCollider))
        {
            //print("added " + other.name);
            boxes.Add((BoxCollider)other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (boxes.Contains((BoxCollider)other))
        {
            boxes.Remove((BoxCollider)other);
            //print("removed " + other.name);
        }
    }

}
