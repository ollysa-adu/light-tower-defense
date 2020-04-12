﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    public float pickupRange;
    public float turningSpeed;
    public float speed;
    public BoxCollider boxColl;
    public BoxCollider armColl;
    public GameObject raycastOrigin;
    public GameObject back;

    private bool holding;
    private Rigidbody rb;
    private bool onTurret;
    private GameObject followObj;
    private GameObject turret;
    private float RotationSpeed;
    private float turretState;
    private float[] range;
    private bool isNorthTurret;

    void Start()
    {
        isNorthTurret = false;
        RotationSpeed = 90f;
        onTurret = false;
        followObj = null;
        holding = false;
        rb = gameObject.GetComponent<Rigidbody>();
    }
  
    void Update()
    {
        if (onTurret)
        {
            turretcontrol();
        }
        else
        {
            PlInteract();
            PlMovement();
        }

    }

    /*
     * controls turret (when mounted)
     * "a" and "d" to turn
     * "space" to shoot
     */
    private void turretcontrol()
    {
        if (Input.GetKey(KeyCode.Space))//shooting
        {
            turret.GetComponent<Turret>().Fire();
        }
        if (Input.GetKeyDown("e"))//getting off turret
        {
            onTurret = false;
            transform.position = new Vector3(turret.transform.position.x, transform.position.y, turret.transform.position.z) - turret.transform.forward * 1.2f;
        }

        /*
         * turning the turret
         */
        float y = turret.transform.rotation.eulerAngles.y;
        if (Input.GetKey("a"))
        {
            y -= RotationSpeed * Time.deltaTime;
        }
        if (Input.GetKey("d"))
        {
            y += RotationSpeed * Time.deltaTime;
        }
        y = ClampAngle(y, range[0], range[1], isNorthTurret);
        Quaternion newRot = Quaternion.Euler(0f, y, 0f);
        turret.transform.rotation = newRot;
        transform.position = new Vector3(turret.transform.position.x, transform.position.y, turret.transform.position.z) - turret.transform.forward;
        transform.rotation = newRot;
    }

    /*
     * player movement
     * wasd to move
     */
    private void PlMovement()
    {
        Vector3 total = new Vector3(0, 0, 0);
        if (Input.GetKey("w"))
        {
            total += Vector3.forward;
        }
        if (Input.GetKey("s"))
        {
            total += Vector3.back;
        }
        if (Input.GetKey("a"))
        {
            total += Vector3.left;
        }
        if (Input.GetKey("d"))
        {
            total += Vector3.right;
        }
        total = Vector3.Normalize(total); //unit vector
        rb.velocity = total * speed; //controlls player movement
        turn_direction(total); //adds turning to player
        if (holding)
        {
            followObj.transform.position = raycastOrigin.transform.position;
            followObj.transform.rotation = transform.rotation;
        }
    }

    /*
     * player interact
     * "space" to pick-up and drop items
     * "e" to interact with turret
     */
    private void PlInteract()
    {
        if (Input.GetKeyDown("e"))//this is to use the turrets once they are positioned into place
        {
            RaycastHit hit;
            Ray thisRaycast = new Ray(back.transform.position, transform.rotation * Vector3.forward);
            if (!holding && Physics.Raycast(thisRaycast, out hit, pickupRange) && hit.collider.tag == "turret_table" &&
                    hit.collider.gameObject.GetComponent<TableScript>().holdingStatus())
            {
                turret = hit.collider.gameObject.GetComponent<TableScript>().getItem();
                onTurret = true;
                transform.position = new Vector3(turret.transform.position.x, transform.position.y, turret.transform.position.z) - turret.transform.forward;
                transform.rotation = turret.transform.rotation;
                int num = (int)hit.collider.gameObject.transform.rotation.eulerAngles.y;
                range = new float[2] { num - 45, num + 45 };
                if (range[0] < 0)
                    isNorthTurret = true;
                else
                    isNorthTurret = false;
            }
        }
        if (Input.GetKey("e"))
        {
            RaycastHit hit;
            Ray thisRaycast = new Ray(back.transform.position, transform.rotation * Vector3.forward);
            if (!holding && Physics.Raycast(thisRaycast, out hit, pickupRange) &&
                hit.collider.gameObject.GetComponents<MonoBehaviour>()[0].GetType().Name == "CraftingTable")
            {
                hit.collider.gameObject.GetComponent<CraftingTable>().increaseCraftBar();
            }
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!holding)
            {
                grab_item();
            }
            else
            {
                drop_item();
            }
        }
    }

    /*
     * this creates fluid turning motion as the player moves
     */
    private void turn_direction(Vector3 targetDirection)
    {
        // The step size is equal to speed times frame time.
        float singleStep = speed * Time.deltaTime * turningSpeed;

        // Rotate the forward vector towards the target direction by one step
        Vector3 newDirection = Vector3.RotateTowards(transform.forward, targetDirection, singleStep, 0.0f);

        // Draw a ray pointing at our target in
        Debug.DrawRay(transform.position, newDirection, Color.red);

        // Calculate a rotation a step closer to the target and applies rotation to this object
        transform.rotation = Quaternion.LookRotation(newDirection);
    }


    /*
     * grab_item() and drop_item() are what allow the player to grab 
     * items from the floor/table and place them on any floor/table
     * 
     * note: turrets cannot be placed on tables atm (this can be changed)
     * 
     * note: an extra arm collision "armColl" is added to prevent players from 
     *      droping items off the map
     * 
     * note: when items are picked up or placed onto a table, the items 
     *      hitboxes are removed to prevent any unecessary interactions
     */
    private void grab_item()
    {
        RaycastHit hit;
        Ray thisRaycast = new Ray(back.transform.position, transform.rotation * Vector3.forward);
        if (Physics.Raycast(thisRaycast, out hit, pickupRange))//checks if raycast hits something
        {
            string Tag = hit.collider.tag;
            if (Tag == "item" || Tag == "turret")//check for the object hit is an item
            {
                followObj = hit.collider.gameObject;
                hit.collider.enabled = false;
                followObj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                followObj.transform.position = raycastOrigin.transform.position;
                holding = true;
                if (Tag == "turret")
                {
                    boxColl.size = new Vector3(1f, 1.3f, 1.8f);
                    boxColl.center = new Vector3(0f, -0.1f, 0.4f);
                }
                else
                {
                    //hit.collider.enabled = false;
                    armColl.enabled = true;
                }
            }
            else if (Tag == "table" || Tag == "turret_table")//check for the object hit is a table
            {
                string name = hit.collider.gameObject.GetComponents<MonoBehaviour>()[0].GetType().Name;
                switch (name){
                    case "TableScript":
                        followObj = hit.collider.gameObject.GetComponent<TableScript>().remove();
                        break;
                    case "ChestScript":
                        followObj = hit.collider.gameObject.GetComponent<ChestScript>().remove();
                        break;
                    case "CraftingTable":
                        followObj = hit.collider.gameObject.GetComponent<CraftingTable>().remove();
                        break;
                }
                if (followObj != null)
                {
                    holding = true;
                    armColl.enabled = true;
                    followObj.transform.position = raycastOrigin.transform.position;
                    if (followObj.tag == "turret")
                    {
                        boxColl.size = new Vector3(1f, 1.3f, 1.8f);
                        boxColl.center = new Vector3(0f, -0.1f, 0.4f);
                        followObj.GetComponent<Turret>().putdown();
                    }
                    else
                    {
                        armColl.enabled = true;
                    }
                }
            }
        }
    }
    private void drop_item()
    {
        RaycastHit hit;
        Ray thisRaycast = new Ray(back.transform.position, transform.rotation * Vector3.forward);

        //test for tables to place items
        if (Physics.Raycast(thisRaycast, out hit, pickupRange) && hit.collider.tag == "table" && followObj.tag == "item"
            && !hit.collider.gameObject.GetComponent<TableScript>().holdingStatus())
        {
            string name = hit.collider.gameObject.GetComponent<MonoBehaviour>().GetType().Name;
            switch (name)
            {
                case "TableScript":
                    hit.collider.gameObject.GetComponent<TableScript>().attach(followObj);
                    break;
                case "ChestScript":
                    hit.collider.gameObject.GetComponent<ChestScript>().attach(followObj);
                    break;
                case "CraftingTable":
                    hit.collider.gameObject.GetComponent<CraftingTable>().attach(followObj);
                    break;
            }
        }
        //test for turret box (areas where turrets can be placed)
        else if (Physics.Raycast(thisRaycast, out hit, pickupRange) && hit.collider.tag == "turret_table" )
        {
            GameObject ttable = hit.collider.gameObject;
            if (followObj.tag == "turret" && !ttable.GetComponent<TableScript>().holdingStatus())
            {
                followObj.GetComponent<Turret>().setup();
                hit.collider.gameObject.GetComponent<TableScript>().attach(followObj);
            }
            else if (followObj.tag == "item" && ttable.GetComponent<TableScript>().holdingStatus())
                hit.collider.gameObject.GetComponent<TableScript>().getItem().GetComponent<Turret>().addAmmo(followObj);
            else
            {
                followObj.transform.position = raycastOrigin.transform.position + (transform.rotation * Vector3.forward) * .1f - new Vector3(0f, 0.9f, 0f);
                followObj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                followObj.GetComponent<Collider>().enabled = true;
            }
        }
        //default drop item to floor
        else
        {
            followObj.transform.position = raycastOrigin.transform.position + (transform.rotation * Vector3.forward) * .1f - new Vector3(0f, 0.9f, 0f);
            followObj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            followObj.GetComponent<Collider>().enabled = true;
        }
        holding = false;
        armColl.enabled = false;
        boxColl.size = new Vector3(1f, 1.3f, 1f);
        boxColl.center = new Vector3(0f, -0.1f, 0f);
    }

    private float ClampAngle(float angle, float min, float max, bool inclNeg)
    {
        if (inclNeg)
        {
            if (angle < -180)
                angle += 360;
            if (angle > 180)
                angle -= 360;
        }
        return Mathf.Clamp(angle, min, max);
    }
}