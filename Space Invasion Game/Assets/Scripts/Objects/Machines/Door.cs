using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum DoorState { unlocked, locked, broken, smashed }

public class Door : MonoBehaviour
{
    [SerializeField] private DoorState currentState;
    [SerializeField] private GameObject lockedIndicator;
    [SerializeField] private GameObject obsticleCollider;

    List<uint> netIDList = new List<uint>();

    private bool isOpen;

    // Components
    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (lockedIndicator.activeInHierarchy)
            lockedIndicator.SetActive(false);
        if (obsticleCollider == null)
            Debug.LogError("obsticleCollider not referenced on " + gameObject.name);

        if (currentState == DoorState.locked)
        {
            Lock();
        }
        else if (currentState == DoorState.broken) // added else if as to not cause a bug if isLocked and isBroken are true
        {
            Broken();
        }
        else
        {
            Unlock();
        }
    }

    #region States

    private void Lock()
    {
        obsticleCollider.SetActive(true);
        animator.SetBool("isLocked", true);
        animator.Play("Locked");
        lockedIndicator.SetActive(true);
    }

    private void Broken()
    {
        obsticleCollider.SetActive(true);
        animator.SetBool("isBroken", true);
        animator.Play("Broken");
    }

    public void Unlock()
    {
        animator.SetBool("isLocked", false);
        lockedIndicator.SetActive(false);
        obsticleCollider.SetActive(false);
        currentState = DoorState.unlocked;
    }
    #endregion

    #region Collider

    private void OnTriggerEnter2D(Collider2D otherCollision)
    {
        if(otherCollision.TryGetComponent<NetworkIdentity>(out NetworkIdentity identity))
        {
            if (!netIDList.Contains(identity.netId))
            {
                if (!isOpen)
                {
                    Open();
                }

                netIDList.Add(identity.netId);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D otherCollision)
    {
        if (otherCollision.TryGetComponent<NetworkIdentity>(out NetworkIdentity identity))
        {
            if (netIDList.Contains(identity.netId))
            {
                netIDList.Remove(identity.netId);
            }

            if (netIDList.Count <= 0)
                Close();
        }
    }
    #endregion

    #region Actions

    private void Open()
    {
        if (currentState == DoorState.unlocked)
        {
            if (!isOpen)
            {
                animator.SetTrigger("OpenTrigger");
                isOpen = true;
            }
        }
    }

    private void Close()
    {
        if (currentState == DoorState.unlocked)
        {
            if (isOpen)
            {
                animator.SetTrigger("CloseTrigger");
                isOpen = false;
            }
        }
    }

    private void Fix()
    {
        animator.SetBool("isBroken", false);
        obsticleCollider.SetActive(false);
        currentState = DoorState.unlocked;
    }
    #endregion
}
