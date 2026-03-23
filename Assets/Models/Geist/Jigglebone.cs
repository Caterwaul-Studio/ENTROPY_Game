//
// Jiggle Bone (Simple version)
//   -  by WarpZone
//
//  TO USE:
//  Simply attach this script to any Transform in your model's hierarchy.
//
//  FEATURES:
//    -  It doesn't matter what the forward normal of your bone is, it just works.
//    -  Bounce and sway can be configured independently
//    -  Simple script is Simple.
//
// If you want to call the bounce code using some sort of manager script, comment out the JiggleBonesUpdate call and SendMessage JiggleBonesUpdate from your Manager instead.
// This is useful if you want to control which bones update first, or strategically layer other procedural animation effects at runtime.
//
 
using UnityEngine;
 
public class jiggleBone2 : MonoBehaviour {
 
    public float bounceFactor = 20;
    public float wobbleFactor = 10;
 
    public float maxTranslation = 0.05f;
    public float maxRotationDegrees = 5;
 
    private Vector3 oldBoneWorldPosition;
    private Quaternion oldBoneWorldRotation;
    private Vector3 animatedBoneWorldPosition;
    private Quaternion animatedBoneWorldRotation;
    private Quaternion goalRotation;
    private Vector3 goalPosition;
 
void Awake()
    {
        oldBoneWorldPosition = transform.position;
        oldBoneWorldRotation = transform.rotation;
    }
 
    void LateUpdate()
    {
        JiggleBonesUpdate();
    }
 
    void JiggleBonesUpdate()
    {
        //Mesh has just been animated
        animatedBoneWorldPosition = transform.position;
        animatedBoneWorldRotation = transform.rotation;
        goalPosition = Vector3.Slerp(oldBoneWorldPosition, transform.position, Time.deltaTime * bounceFactor);
        goalRotation = Quaternion.Slerp(oldBoneWorldRotation, transform.rotation, Time.deltaTime * wobbleFactor);
 
        transform.rotation = Quaternion.RotateTowards(animatedBoneWorldRotation, goalRotation, maxRotationDegrees);
        transform.position = Vector3.MoveTowards(animatedBoneWorldPosition, goalPosition, maxTranslation);
 
        oldBoneWorldPosition = transform.position;
        oldBoneWorldRotation = transform.rotation;
    }
 
}
