using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : BaseHealth
{
    [SerializeField] private float maxTargetingPosition;
    [SerializeField] private Transform hitPos;
    [SerializeField] private float hitRadius;
    [SerializeField] private Transform hitPosRight, hitPosLeft;

    private NavMeshAgent agent;
    private Animator anim;
    private PhotonView pv;
    private bool isHitting = false;

    public override void Start()
    {
        base.Start();

        anim = GetComponent<Animator>();

        pv = GetComponent<PhotonView>();

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (isHitting)
                return;

            List<Transform> players = new List<Transform>();

            foreach (var player in FindObjectsOfType<PlayerMovement>())
            {
                players.Add(player.transform);
            }

            try
            {
                GameObject nothing = players[0].gameObject;
            }
            catch
            {
               return;
            }

            float minDistance = Vector3.Distance(players[0].position, transform.position);
            int minDistancePlayerIndex = 0;
            int currentIterationIndex = 0;
            foreach (var player in players)
            {
                if (Vector3.Distance(player.position, transform.position) < minDistance)
                {
                    minDistance = Vector3.Distance(player.position, transform.position);
                    minDistancePlayerIndex = currentIterationIndex;
                }
                currentIterationIndex++;
            }

            if (Vector3.Distance(players[minDistancePlayerIndex].position, transform.position) > maxTargetingPosition)
            {
                agent.isStopped = true;

            }
            else
            {
                agent.isStopped = false;
            }


            CentrializeAndSetDistination(players[minDistancePlayerIndex].position);


            Vector3 playerLocalPos = transform.InverseTransformPoint(players[minDistancePlayerIndex].position);
            if (playerLocalPos.x >= 0)
            {
                pv.RPC("NetworkChangeDirection", RpcTarget.AllBuffered, (byte)Direction.Right);
            }
            else
            {
                pv.RPC("NetworkChangeDirection", RpcTarget.AllBuffered, (byte)Direction.Left);
            }

            if (agent.velocity != Vector3.zero)
            {
                anim.SetBool("IsRunning", true);
            }
            else
            {
                anim.SetBool("IsRunning", false);            
            }

            if (Vector3.Distance(transform.position, players[minDistancePlayerIndex].position) <= 2)
            {
                TryAttackPlayer(players[minDistancePlayerIndex].gameObject);
            }
        }

    }

    private void OnDrawGizmosSelected()
    {
        if (hitPos == null)
            return;

        Gizmos.DrawWireSphere(hitPos.position, hitRadius);
    }

    private void TryAttackPlayer(GameObject targetPlayer)
      {
        Collider2D[] hitPlayer = Physics2D.OverlapCircleAll(hitPos.position, hitRadius);

        foreach(Collider2D player in hitPlayer)
        {
            if (player.tag == "Other Player" || player.tag == "Player")
            {
                pv.RPC("NetworkAttackPlayer", RpcTarget.All, targetPlayer.name);
                return;
            }
        }
      }

    private void CentrializeAndSetDistination(Vector3 target)
    {
        if (Vector3.Distance(target, transform.position) < agent.stoppingDistance)
        {
            agent.SetDestination(transform.position);
        }
        else
        {
            agent.SetDestination(target);
        }
    }

    [PunRPC]
    public void NetworkAttackPlayer(string targetPlayerName)
    {
        anim.SetTrigger("Hit");       
        agent.isStopped = true;
        isHitting = true;
    }

    [PunRPC]
    public void NetworkChangeDirection(byte dir)
    {
        if(dir == (byte)Direction.Left)
        {
            foreach (Transform child in transform.GetChild(0))
            {
                child.GetComponent<SpriteRenderer>().flipX = true;
            }

            hitPos.position = hitPosLeft.position;
        }
        else
        {
            foreach (Transform child in transform.GetChild(0))
            {
                child.GetComponent<SpriteRenderer>().flipX = false;
            }

            hitPos.position = hitPosRight.position;
        }
    }

    public void StopHitting()
    {
        isHitting = false;
        agent.isStopped = false;
    }

    public void Hit()
    {
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(hitPos.position, hitRadius);

        foreach (Collider2D player in hitPlayers)
        {
            if (player.tag == "Other Player" || player.tag == "Player")
                player.GetComponent<IDamageable>().Damage(1);
        }
    }
}
public enum Direction { Right, Left }
