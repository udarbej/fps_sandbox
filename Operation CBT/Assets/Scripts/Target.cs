using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Target : MonoBehaviour
{
    public NavMeshAgent enemy;
    public Transform player;
    public float health;


    private void Update(){
        enemy.destination = player.position;
        if(health <= 0f){
            Brap();
        }
    }

    public void TakeDamage(float amount){
        health -= amount;
        Debug.Log(health);
        if(health <= 0f){
            Brap();
        }
    }

    void Brap(){
        Destroy(gameObject);
    }

}
