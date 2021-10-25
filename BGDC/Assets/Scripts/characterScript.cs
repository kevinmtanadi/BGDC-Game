﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class characterScript : MonoBehaviour
{

    dashParticleManager dashManager;
    Rigidbody rb;
    float playerHeight = 2f;
    wallRun WallRun;

    //Moving
    public float speedMultiplier = 10f;
    public float moveSpeed = 12f;
    public Vector3 dir;
    float xMovement, yMovement;
    public float groundDrag = 6f;

    //Jumping
    public float gravity = -18.3f;
    public Transform groundCheck;
    public float groundDistance = .4f;
    public LayerMask ground;
    public float jumpHeight;   
    public float jumpDrag = 2f;
    public float jumpSpeedMultiplier = 0.4f;
    public float fallSpeed = 2f;
    bool inAir = false;
    public bool isGrounded;

    //Slope & Stair Detection
    RaycastHit slopeHit;
    Vector3 slopeDirection;

    //Dashing
    Vector3 dashDirection;
    public float dashForce = 5f;
    public float dashDuration = .5f;
    public float timeSinceLastDash;
    public float dashCooldown = 3f;

    //Audio
    soundManagerScript soundManager;
    bool runningAudioPlaying = false;

    //Pick Up Items
    public GameObject[] itemOnField;
    public float distanceToPickUp = 2f;
    public GameObject closestItem;
    public TMP_Text showText;
    public weaponManager weaponSystem;

    
    void Start()
    {
        WallRun = GetComponent<wallRun>();
        rb = GetComponent<Rigidbody>();
        dashManager = GetComponent<dashParticleManager>();
        rb.freezeRotation = true;
        timeSinceLastDash = dashCooldown;
        soundManager = FindObjectOfType<soundManagerScript>();
    }

    private void Update()
    {
        Friction();
        Jumping();

        closestItem = FindWeapon();
        if (Vector3.Distance(transform.position, closestItem.transform.position) < distanceToPickUp)
        {
            Debug.Log("Takeable " + closestItem.name);
            showText.GetComponent<TMP_Text>().text = ("Press F to take " + closestItem.name);
            showText.gameObject.SetActive(true);
            if (Input.GetKeyDown(KeyCode.F))
            {
                weaponSystem.takeNewWeapon(closestItem);
            }
        }
        else
        {
            showText.gameObject.SetActive(false);
        }

        timeSinceLastDash += Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.LeftShift) && timeSinceLastDash > dashCooldown)
        {
            StartCoroutine(Dash());
            timeSinceLastDash = 0;
        }

        slopeDirection = Vector3.ProjectOnPlane(dir, slopeHit.normal);

        if (!isGrounded)
        {
            inAir = true;
        }
        if (inAir && isGrounded)
        {
            soundManager.Play("falling");
            inAir = false;
        }
    }
    void FixedUpdate()
    {
        Moving();
    }

    void Friction()
    {
        if (isGrounded)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = jumpDrag;
        }
    }

    void Moving()
    {
        xMovement = Input.GetAxisRaw("Horizontal");
        yMovement = Input.GetAxisRaw("Vertical");

        dir = transform.right * xMovement + transform.forward * yMovement;

        if (isGrounded && !onSlope())
        {
            rb.AddForce(dir.normalized * moveSpeed * speedMultiplier);
        }
        else if (!isGrounded)
        {
            rb.AddForce(dir.normalized * moveSpeed * speedMultiplier * jumpSpeedMultiplier, ForceMode.Acceleration);
        }
        else if (isGrounded && onSlope())
        {
            rb.AddForce(slopeDirection.normalized * moveSpeed * speedMultiplier, ForceMode.Acceleration);
        }

        if (isGrounded && dir.magnitude > 0f)
        {
            if (!runningAudioPlaying)
            {
                soundManager.Play("running");
                runningAudioPlaying = true;
            }          
        }
        else
        {
            soundManager.Pause("running");
            runningAudioPlaying = false;
        }
        
    }

    void Jumping()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, ground);

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(transform.up * jumpHeight, ForceMode.Impulse);
            soundManager.Play("jumping");
        }
        else if (!isGrounded && !WallRun.isWallRunning)
        {
            rb.AddForce(-transform.up * fallSpeed, ForceMode.Acceleration);
        }
    }

    bool onSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight/2 + .5f))
        {
            if (slopeHit.normal != Vector3.up)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    public IEnumerator Dash()
    {
        if (xMovement == 0 && yMovement == 0)
        {
            dashDirection = transform.forward;
        }
        else if (yMovement > 0)
        {
            dashDirection = transform.forward;
        }
        else if (yMovement < 0)
        {
            dashDirection = -transform.forward;   
        }
        else if (yMovement == 0)
        {
            if (xMovement > 0)
            {
                dashDirection = transform.right;
            }
            else if (xMovement < 0)
            {
                dashDirection = -transform.right;
            }
        }
        rb.useGravity = false;
        rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);
        soundManager.Play("dash");
        dashManager.manageDash();
        yield return new WaitForSeconds(dashDuration);

        rb.velocity = dir * moveSpeed;
        rb.useGravity = true;
    }

    public GameObject FindWeapon()
    {
        itemOnField = GameObject.FindGameObjectsWithTag("Weapon");
        GameObject closestItem = null;
        float distance = Mathf.Infinity;
        Vector3 curPos = transform.position;
        foreach(GameObject item in itemOnField)
        {
            Vector3 diff = item.transform.position - curPos;
            float curDistance = diff.sqrMagnitude;
            if (curDistance < distance)
            {
                closestItem = item;
                distance = curDistance;
            }
        }
        return closestItem;
    }
}
