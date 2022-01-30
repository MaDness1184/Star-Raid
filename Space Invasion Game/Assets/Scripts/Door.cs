using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private bool isLocked, isBroken, isOpen;
    [SerializeField] private GameObject lockedIndicator;

    // Components
    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (lockedIndicator.activeSelf)
            lockedIndicator.SetActive(false);

        if (isLocked)
        {
            animator.SetBool("isLocked", true);
            animator.Play("Locked");
            lockedIndicator.SetActive(true);
        }
        else if (isBroken) // added else if as to not cause a bug if isLocked and isBroken are true
        {
            animator.SetBool("isBroken", true);
            animator.Play("Broken");
        }
    }

    public void Unlock()
    {
        animator.SetBool("isLocked", false);
        lockedIndicator.SetActive(false);
    }

    public void Fix()
    {
        animator.SetBool("isBroken", false);
    }

    public void Open()
    {
        if (!isLocked && !isBroken && !isOpen)
        {
            animator.SetTrigger("OpenTrigger");
        }
    }

    public void Close()
    {
        if (!isLocked && !isBroken && isOpen)
        {
            animator.SetTrigger("CloseTrigger");
        }
    }
}
