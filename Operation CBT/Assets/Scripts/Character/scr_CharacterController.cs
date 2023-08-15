using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static scr_Models;

public class scr_CharacterController : MonoBehaviour
{
    private CharacterController character_control;
    private DefaultInput default_input;
    [HideInInspector]
    public Vector2 input_movement;
    [HideInInspector]
    public Vector2 input_view;
    private Vector3 new_cam_rot;
    private Vector3 new_player_rot;

    [Header("References")]
    public bool test;
    public Transform camera_holder;
    public Transform feet_transform;

    [Header("Settings")]
    public PlayerSettingsModel player_settings;
    public LayerMask player_mask;
    public LayerMask ground_mask;
    private float viewclamp_y_min = -90;
    private float viewclamp_y_max = 90;

    [Header("Gravity")]
    public float gravity_amount;
    public float gravity_min;
    private float player_gravity;
    private Vector3 jump_initial;
    private Vector3 jump_velocity;

    [Header("Movement")]
    public float set_slide_time;
    private float temp_slide_time;
    [HideInInspector]
    public bool is_sprinting;
    [HideInInspector]
    public bool is_sliding;
    private Vector3 default_speed;
    private Vector3 default_velocity;
    private Vector3 direction;

    [Header("Stance")]
    public PlayerStance player_stance;
    public float player_stance_smooth;
    public CharacterStance player_stand;
    public CharacterStance player_crouch;
    public CharacterStance player_prone;
    private float stance_check_margin = 0.05f;
    private float cam_pos;
    private float cam_pos_velocity;
    private Vector3 center_velocity;
    private float height_velocity;

    [Header("Leaning")]
    public Transform LeanPivot;
    private float currentLean;
    private float targetLean;
    public float leanAngle;
    public float leanSmoothing;
    private float leanVelocity;
    private bool isLeaningLeft;
    private bool isLeaningRight;

    [Header("Weapon")]
    public scr_WeaponController current_weapon;
    public float weaponAnimationSpeed;

    [HideInInspector]
    public bool isGrounded;
    [HideInInspector]
    public bool isFalling;



    private void Awake(){
        //read actuation of inputs
        default_input = new DefaultInput();
        default_input.Character.Movement.performed += e => input_movement = e.ReadValue<Vector2>();
        default_input.Character.View.performed += e => input_view = e.ReadValue<Vector2>();
        default_input.Character.Jump.performed += e => jump();
        default_input.Character.Crouch.performed += e => crouch();
        default_input.Character.Prone.performed += e => prone();
        default_input.Character.Sprint.performed += e => sprint();
        default_input.Character.Sprint_Release.performed += e => stop_sprint();
        default_input.Character.Lean_Left_Pressed.performed += e => isLeaningLeft = true;
        default_input.Character.Lean_Left_Released.performed += e => isLeaningLeft = false;
        default_input.Character.Lean_Right_Pressed.performed += e => isLeaningRight = true;
        default_input.Character.Lean_Right_Released.performed += e => isLeaningRight = false;

        default_input.Weapon.Fire1Pressed.performed += e => ShootingPressed();
        default_input.Weapon.Fire1Released.performed += e => ShootingReleased();
        default_input.Weapon.Reload.performed += e => ReloadPressed();

        default_input.Enable();
        //get player and camera rotation amounts
        new_cam_rot = camera_holder.localRotation.eulerAngles;
        new_player_rot = transform.localRotation.eulerAngles;
        character_control = GetComponent<CharacterController>();
        cam_pos = camera_holder.localPosition.y;
        if(current_weapon){
            current_weapon.initialize(this);
        }
    }

    private void Update(){
        //get new player values
        SetIsGrounded();
        SetIsFalling();
        calculate_movment();
        calculate_view();
        calculate_stance();
        calculate_leaning();
        calculate_jump();
    }

    private void SetIsGrounded(){
        isGrounded = Physics.CheckSphere(feet_transform.position, player_settings.isGroundedRadius, ground_mask);
    }

    private void SetIsFalling(){
        isFalling = (!isGrounded && character_control.velocity.magnitude > player_settings.isFallingSpeed);
        
    }

    private void ShootingPressed(){
        if (current_weapon){
            current_weapon.isShooting = true;
        }
    }

    private void ShootingReleased(){
        if (current_weapon){
            current_weapon.isShooting = false;
        }
    }

    private void ReloadPressed(){
        if (current_weapon && current_weapon.currentAmmo != current_weapon.maxAmmo){
            current_weapon.currentAmmo = 0;
        }
    }

    private void calculate_movment(){
        //check sprinting and slide threshold
        if (input_movement.y <= 0.25f) is_sprinting = false;
        if (is_sliding) {
            slide();
        }
        else {
            //initialize movement speeds
            var vertical_speed = is_sprinting ? player_settings.sprint_forward_speed : player_settings.walk_forward_speed;
            var horizontal_speed = is_sprinting ? player_settings.sprint_strafe_speed : player_settings.walk_strafe_speed;
            //adjust speed according to stance
            switch(player_stance){
                case PlayerStance.crouch:
                    player_settings.current_mod = is_sliding ? player_settings.slide_mod : player_settings.crouch_mod;
                    break;
                case PlayerStance.prone:
                    player_settings.current_mod = player_settings.prone_mod;
                    break;
                default:
                    player_settings.current_mod = 1;
                    break;
            }

            weaponAnimationSpeed = character_control.velocity.magnitude / (player_settings.walk_forward_speed * player_settings.current_mod);
            if (weaponAnimationSpeed > 1){
                weaponAnimationSpeed = 1;
            }

            vertical_speed *= player_settings.current_mod;
            horizontal_speed *= player_settings.current_mod;
            //set movement direction
            direction = new Vector3(horizontal_speed * input_movement.x * Time.deltaTime, 0, vertical_speed * input_movement.y * Time.deltaTime);
        }
        default_speed = Vector3.SmoothDamp(default_speed, direction, ref default_velocity, player_settings.default_smoothing);
        Vector3 move_speed = Vector3.zero;
        move_speed = transform.TransformDirection(default_speed);
        //force of gravity
        if (player_gravity > gravity_min){
            player_gravity -= gravity_amount * Time.deltaTime;
        }
        if (player_gravity < -0.1f && isGrounded){
            player_gravity = -1;
        }
        move_speed.y += player_gravity;
        move_speed += jump_initial * Time.deltaTime;
        //output
        character_control.Move(move_speed);
    }

    private void calculate_view(){
        //rotate player object to x-axis mouse movement
        if (!is_sliding) {
            new_player_rot.y += player_settings.ViewXSensitivity * (player_settings.ViewXInverted ? -input_view.x : input_view.x) * Time.deltaTime;   
            transform.rotation = Quaternion.Euler(new_player_rot);
        } else {
            new_cam_rot.y += player_settings.ViewXSensitivity * (player_settings.ViewXInverted ? -input_view.x : input_view.x) * Time.deltaTime;
            camera_holder.localRotation = Quaternion.Euler(new_cam_rot);
        }
        //rotate camera according to view clamp bounds y-axis
        new_cam_rot.x += player_settings.ViewYSensitivity * (player_settings.ViewYInverted ? input_view.y : -input_view.y) * Time.deltaTime;
        new_cam_rot.x = Mathf.Clamp(new_cam_rot.x, viewclamp_y_min, viewclamp_y_max);
        camera_holder.localRotation = Quaternion.Euler(new_cam_rot);
    }

    private void calculate_leaning(){
        if(isLeaningLeft){
            targetLean = leanAngle;
        }
        else if(isLeaningRight){
            targetLean = -leanAngle;
        }
        else{
            targetLean = 0;
        }
        currentLean = Mathf.SmoothDamp(currentLean, targetLean, ref leanVelocity, leanSmoothing);
        LeanPivot.localRotation = Quaternion.Euler(new Vector3(0,0, currentLean));
    }

    private void calculate_jump(){
        //set jump start velocity
        jump_initial = Vector3.SmoothDamp(jump_initial, Vector3.zero, ref jump_velocity, player_settings.jump_terminal);
    }

    private void calculate_stance(){
        //match camera position to player object height
        CharacterStance current_stance = null;
        switch (player_stance){
            case PlayerStance.crouch:
                current_stance = player_crouch;
                break;
            case PlayerStance.prone:
                current_stance = player_prone;
                break;
            default:
                current_stance = player_stand;
                break;
        }

        cam_pos = Mathf.SmoothDamp(camera_holder.localPosition.y, current_stance.camera_height, ref cam_pos_velocity, player_stance_smooth);
        camera_holder.localPosition = new Vector3(camera_holder.localPosition.x, cam_pos, camera_holder.localPosition.z);

        character_control.height = Mathf.SmoothDamp(character_control.height, current_stance.stance_collider.height, ref height_velocity, player_stance_smooth);
        character_control.center = Vector3.SmoothDamp(character_control.center, current_stance.stance_collider.center, ref center_velocity, player_stance_smooth);
    }

    private void jump(){
        if (!isGrounded){
            return;
        }
        //set new stance if player is not standing
        if (player_stance == PlayerStance.crouch || player_stance == PlayerStance.prone){
            if (no_headspace(player_stand.stance_collider.height)){
                return;
            }
            player_stance = PlayerStance.stand;
            return;
        }
        jump_initial = Vector3.up * player_settings.jump_height;
        player_gravity = 0;
        current_weapon.TriggerJump();
    }

    private void crouch(){
        if (player_stance == PlayerStance.crouch){
            if (no_headspace(player_stand.stance_collider.height)){
                return;
            }
            player_stance = PlayerStance.stand;
            return;
        }
        if (no_headspace(player_crouch.stance_collider.height)){
                return;
        }
        if (player_stance == PlayerStance.stand && is_sprinting){
            is_sliding = true;
        }
        player_stance = PlayerStance.crouch;
    }

    private void prone(){
        if (player_stance == PlayerStance.prone){
            if (no_headspace(player_crouch.stance_collider.height)){
                return;
            }
            player_stance = PlayerStance.crouch;
            return;
        }
        if (no_headspace(player_prone.stance_collider.height)){
                return;
        }
        player_stance = PlayerStance.prone;
    }

    private bool no_headspace(float check_space){
        var start = new Vector3(feet_transform.position.x, feet_transform.position.y + character_control.radius + stance_check_margin, feet_transform.position.z);
        var end = new Vector3(feet_transform.position.x, feet_transform.position.y - character_control.radius - stance_check_margin + check_space, feet_transform.position.z);
        
        return Physics.CheckCapsule(start, end, character_control.radius, player_mask);
    }

    private void sprint(){
        if (input_movement.y <= 0.25f || player_stance == PlayerStance.prone){
            return;
        }
        is_sprinting = true;
    }

    private void stop_sprint(){
        is_sprinting = false;
    }

    private void slide(){
        if (isGrounded){
            temp_slide_time -= Time.deltaTime;
            //reduce the slide speed over time
            if(temp_slide_time <= 0 || player_stance != PlayerStance.crouch){
                //match camera rotation with player rotation
                new_player_rot.y += new_cam_rot.y;
                transform.rotation = Quaternion.Euler(new_player_rot);
                new_cam_rot.y = 0;
                camera_holder.localRotation = Quaternion.Euler(new_cam_rot);
                temp_slide_time = set_slide_time;
                is_sliding = false;
            }
        }
    }

    private void OnDrawGizmos(){
        Gizmos.DrawWireSphere(feet_transform.position, player_settings.isGroundedRadius);
    }

}

