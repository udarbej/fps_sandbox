using System;
using System.Collections.Generic;
using UnityEngine;

public static class scr_Models
{
    #region - Player -

    public enum PlayerStance {stand, crouch, prone}

    [Serializable]
    public class PlayerSettingsModel{
        [Header("Viewing Settings")]
        public float ViewXSensitivity;
        public float ViewYSensitivity;

        public bool ViewXInverted;
        public bool ViewYInverted;

        [Header("Movement Settings")]
        public float default_smoothing;
        

        [Header("Sprinting")]
        public float sprint_forward_speed;
        public float sprint_strafe_speed;

        [Header("Walking")]
        public float walk_forward_speed;
        public float walk_strafe_speed;

        [Header("Jumping")]
        public float jump_height;
        public float jump_terminal;

        [Header("Speed Modifiers")]
        public float current_mod = 1;
        public float crouch_mod;
        public float prone_mod;
        public float slide_mod;

        [Header("Is Grounded / Falling")]
        public float isGroundedRadius;
        public float isFallingSpeed;
    }

    [Serializable]
    public class CharacterStance{
        public float camera_height;
        public CapsuleCollider stance_collider;
    }

    #endregion

    #region - Weapons -

    [Serializable]
    public class WeaponSettingsModel
    {
        [Header("Weapon Sway")]
        public float SwayAmount;
        public bool SwayYInverted;
        public bool SwayXInverted;
        public float SwaySmoothing;
        public float SwayResetSmoothing;
        public float SwayClampX;
        public float SwayClampY;
        
        [Header("Weapon Movement Sway")]
        public float MovementSwayX;
        public float MovementSwayY;
        public bool MovementSwayXInverted;
        public bool MovementSwayYInverted;
        public float MovementSwaySmoothing;

    }

    #endregion
}
