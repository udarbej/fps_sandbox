using UnityEngine;
using UnityEngine.UI;
using System.Collections;

using static scr_Models;

public class scr_WeaponController : MonoBehaviour
{
    private scr_CharacterController character_control;

    [Header("References")]
    public Animator weaponAnimator;


    [Header("Settings")]
    public WeaponSettingsModel settings;

    bool isInitialized;

    //weapon sway
    Vector3 newWeaponRotation;
    Vector3 newWeaponRotationVelocity;

    Vector3 targetWeaponRotation;
    Vector3 targetWeaponRotationVelocity;

    Vector3 newWeaponMovementRotation;
    Vector3 newWeaponMovementRotationVelocity;

    Vector3 targetWeaponMovementRotation;
    Vector3 targetWeaponMovementRotationVelocity;

    private bool isGroundedTrigger;
    private float fallingDelay;
    
    [Header("Idle")]
    public Transform WeaponIdleObject;
    public float swayAmountA = 1;
    public float swayAmountB = 2;
    public float swayScale = 400;
    public float swayLerpSpeed = 14;
    public float swayTime;
    public Vector3 swayPosition;

    [Header("Weapon Properties")]
    public float damage = 10f;
    public float range = 500f;
    public float impactForce = 100f;
    public int maxAmmo = 8;
    public Text ammoDisplay;
    public int currentAmmo;
    public float reloadTime = 1f;
    public bool isReloading = false;
    public float shootTime = 0.25f;

    //shooting
    public Camera fpsCam;
    public bool isShooting;
    public ParticleSystem muzzleFlash;
    public GameObject impactEffect;
    public AudioSource source;
    public AudioClip sound_shoot;
    public AudioClip sound_reload;
    
    private void Start(){
        if(currentAmmo == -1) currentAmmo = maxAmmo;
        newWeaponRotation = transform.localRotation.eulerAngles;
    }

    void OnEnable(){
        isReloading = false;
        weaponAnimator.SetBool("isReloading", false);
    }

    public void initialize(scr_CharacterController CharacterController){
        character_control = CharacterController;
        isInitialized = true;
    }

    private void Update(){
        ammoDisplay.text = currentAmmo.ToString();
        if(!isInitialized)return;
        if(isReloading)return;
        if(currentAmmo <= 0){
            StartCoroutine(Reload());
            return;
        }
        //shooting mechanics
        Calculate_Shooting();
        CalculateWeaponRotation();
        SetWeaponAnimations();
        CalculateWeaponSway();

    }

    public void TriggerJump(){
        isGroundedTrigger = false;
        weaponAnimator.SetTrigger("Jump");
    }

    private void CalculateWeaponRotation(){
        //match weapon rotation with camera rotation
        targetWeaponRotation.x += settings.SwayAmount * (settings.SwayYInverted ? character_control.input_view.y : -character_control.input_view.y) * Time.deltaTime;
        targetWeaponRotation.y += settings.SwayAmount * (settings.SwayXInverted ? -character_control.input_view.x : character_control.input_view.x) * Time.deltaTime; 
        targetWeaponRotation.x = Mathf.Clamp(targetWeaponRotation.x, -settings.SwayClampX, settings.SwayClampX);
        targetWeaponRotation.y = Mathf.Clamp(targetWeaponRotation.y, -settings.SwayClampY, settings.SwayClampY);
        targetWeaponRotation.z = -targetWeaponRotation.y;
        //reset the weapon rotation to align with camera angle
        targetWeaponRotation =  Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero, ref targetWeaponRotationVelocity, settings.SwayResetSmoothing);
        newWeaponRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponRotation, ref newWeaponRotationVelocity, settings.SwaySmoothing);
        
        targetWeaponMovementRotation.z = settings.MovementSwayX * (settings.MovementSwayXInverted ? -character_control.input_movement.x : character_control.input_movement.x);
        targetWeaponMovementRotation.x = settings.MovementSwayY * (settings.MovementSwayYInverted ? -character_control.input_movement.y : character_control.input_movement.y);

        targetWeaponMovementRotation =  Vector3.SmoothDamp(targetWeaponMovementRotation, Vector3.zero, ref targetWeaponMovementRotationVelocity, settings.MovementSwaySmoothing);
        newWeaponMovementRotation = Vector3.SmoothDamp(newWeaponMovementRotation, targetWeaponMovementRotation, ref newWeaponMovementRotationVelocity, settings.MovementSwaySmoothing);
        transform.localRotation = Quaternion.Euler(newWeaponRotation + newWeaponMovementRotation);   
    }

    private void SetWeaponAnimations(){
        //Debug.Log("is grounded: " + character_control.isGrounded + " is ground trigger: " + isGroundedTrigger + " falling delay: "+ fallingDelay);
        if(character_control.isGrounded && !isGroundedTrigger && fallingDelay > 0.1f){
            weaponAnimator.SetTrigger("Land");
            isGroundedTrigger = true;
        }
        else if(!character_control.isGrounded && isGroundedTrigger){
            weaponAnimator.SetTrigger("Falling");
            isGroundedTrigger = false;
        }
        
        if(character_control.isGrounded){
            fallingDelay = 0;
        }
        else{
            fallingDelay += Time.deltaTime;
        }

        
        //weaponAnimator.speed = character_control.weaponAnimationSpeed;
        weaponAnimator.SetBool("isSprinting", character_control.is_sprinting);
        weaponAnimator.SetFloat("WeaponAnimationSpeed", character_control.weaponAnimationSpeed);
    }

    private void CalculateWeaponSway(){
        var targetPosition = LissajousCurve(swayTime, swayAmountA, swayAmountB) / swayScale;
        swayPosition = Vector3.Lerp(swayPosition, targetPosition, Time.smoothDeltaTime * swayLerpSpeed);
        swayTime += Time.deltaTime;
        if (swayTime > 6.3f){
            swayTime = 0;
        }
        WeaponIdleObject.localPosition = swayPosition;
    }

    private Vector3 LissajousCurve(float Time, float A, float B){
        return new Vector3(Mathf.Sin(Time), A * Mathf.Sin(B * Time + Mathf.PI));
    }

    private void Calculate_Shooting(){
        if(isShooting && !character_control.is_sprinting){
            weaponAnimator.SetTrigger("isShooting");
            Shoot();
            //semiauto
            isShooting = false;
        }
    }

    private IEnumerator Reload(){
        isReloading = true;
        source.PlayOneShot(sound_reload);
        weaponAnimator.SetBool("isReloading", true);
        yield return new WaitForSeconds(reloadTime-.25f);
        weaponAnimator.SetBool("isReloading", false);
        yield return new WaitForSeconds(.25f);
        currentAmmo = maxAmmo;
        isReloading = false;
    }

    private void Shoot(){
        muzzleFlash.Play();
        source.PlayOneShot(sound_shoot);
        currentAmmo--;
        RaycastHit hit;
        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range)){
            Debug.Log(hit.transform.name);

            Target target = hit.transform.GetComponent<Target>();
            if (target != null){
                target.TakeDamage(damage);
            }
            if(hit.rigidbody != null){
                hit.rigidbody.AddForce(-hit.normal * impactForce);
            }

            GameObject impactGO = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impactGO, 2f);
        }


    }
    

}
