using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FireExtinguisher : MonoBehaviour
{
    [SerializeField] private bool canPuff = true;
    [SerializeField] private List<GameObject> puffObjects;
    [SerializeField] private int puffCycle = 0;
    [SerializeField] private Transform fireExtinguisherPosition;


    public void OnThrow(InputAction.CallbackContext context)
    {
        if (canPuff)
            StartCoroutine(MakePuff());
    }

        // Update is called once per frame
    void Update()
    {
    }
    System.Collections.IEnumerator MakePuff()
    {
        puffObjects[puffCycle].GetComponent<PuffMovement>().Shoot(fireExtinguisherPosition);
        puffCycle++;
        if (puffCycle >= puffObjects.Count)
            puffCycle = 0;
        canPuff = false;
        yield return new WaitForSeconds(1f);
        canPuff = true;
    }
}
