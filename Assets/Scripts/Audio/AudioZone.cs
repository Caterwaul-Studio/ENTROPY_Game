using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioZone : MonoBehaviour
{
    public AudioZone[] adjacentZones;

    [SerializeField] private List<GameObject> audioObjects;
    [SerializeField] private AudioCulling cullingManager;
    [SerializeField] private GameObject player;

    public Bounds bounds;

    void Start()
    {
        //make it so the bounds are just the transform
        bounds = new Bounds(transform.position, transform.localScale);

        //this might look odd, but basically the box collider is just a gizmo so we can see how big the bounding box is
        //because the bounding box does not render in editor but the box collider component does, and they are both inheriting the same things from the transform
        Destroy(this.gameObject.GetComponent<BoxCollider>());

        StartCoroutine(Populate());
    }


    public void Repopulate()
    {
        StartCoroutine(Populate());
    }

    IEnumerator Populate()
    {
        yield return new WaitForSeconds(1);

        audioObjects.Clear(); // clear stake references from past scene load

        if (cullingManager == null || cullingManager.cullable == null) yield break;

        for (int i = 0; i < cullingManager.cullable.Length; i++)
        {
            if (bounds.Contains(cullingManager.cullable[i].gameObject.transform.position))
            {
                audioObjects.Add(cullingManager.cullable[i].gameObject);
            }
        }
    }

    public void Activate()
    {
        for (int i = 0; i < audioObjects.Count; i++)
        {
            if (!cullingManager.exempt.Contains<AudioSource>(audioObjects[i].GetComponent<AudioSource>())) //make sure it isnt on the exemption list
            {
                audioObjects[i].SetActive(true);
            }
        }
    }

    public void Deactivate()
    {
        for (int i = 0; i < audioObjects.Count; i++)
        {
            if (!cullingManager.exempt.Contains<AudioSource>(audioObjects[i].GetComponent<AudioSource>())) //make sure it isnt on the exemption list
            {
                audioObjects[i].SetActive(false);
            }
        }
    }

    void Update()
    {
        //USE THIS FUNCTION TO SEE THE SIZE OF THE BOUNDS AT RUNTIME IF YOU NEED TO
        DrawBounds(bounds, Color.red);
    }

    //this function is taken from Unity documentation, it's just for visuals
    //https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Bounds.html
    void DrawBounds(Bounds b, Color color)
    {
        Vector3 min = b.min;
        Vector3 max = b.max;

        Vector3[] corners = new Vector3[8];
        // Bottom
        corners[0] = new Vector3(min.x, min.y, min.z);
        corners[1] = new Vector3(max.x, min.y, min.z);
        corners[2] = new Vector3(max.x, min.y, max.z);
        corners[3] = new Vector3(min.x, min.y, max.z);
        // Top
        corners[4] = new Vector3(min.x, max.y, min.z);
        corners[5] = new Vector3(max.x, max.y, min.z);
        corners[6] = new Vector3(max.x, max.y, max.z);
        corners[7] = new Vector3(min.x, max.y, max.z);

        // Bottom rectangle
        Debug.DrawLine(corners[0], corners[1], color);
        Debug.DrawLine(corners[1], corners[2], color);
        Debug.DrawLine(corners[2], corners[3], color);
        Debug.DrawLine(corners[3], corners[0], color);

        // Top rectangle
        Debug.DrawLine(corners[4], corners[5], color);
        Debug.DrawLine(corners[5], corners[6], color);
        Debug.DrawLine(corners[6], corners[7], color);
        Debug.DrawLine(corners[7], corners[4], color);

        // Vertical edges
        Debug.DrawLine(corners[0], corners[4], color);
        Debug.DrawLine(corners[1], corners[5], color);
        Debug.DrawLine(corners[2], corners[6], color);
        Debug.DrawLine(corners[3], corners[7], color);
    }

}
