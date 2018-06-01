using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace NetXr {
    public class UnityIK : MonoBehaviour {

        Animator anim;
        PlayerPhysics playerPhysics;
        Transform vrCameraTransform;

        private float ikWeight = 1;

        public Transform leftHandIkTarget;
        public Transform leftElbowIkTarget;

        public Transform rightHandIkTarget;
        public Transform rightElbowIkTarget;

        public Vector3 leftHandRotationOffset;
        public Vector3 rightHandRotationOffset;

        public Vector3 leftHandPositionOffset;
        public Vector3 rightHandPositionOffset;

        public Vector3 leftFootOffset = new Vector3(-.3f, 0, 0);
        public Vector3 rightFootOffset = new Vector3(.3f, 0, 0);

        public Vector3 leftKneeOffset = Vector3.up;
        public Vector3 rightKneeOffset = Vector3.up;

        // Use this for initialization
        void Start() {
            anim = gameObject.GetComponent<Animator>();
            playerPhysics = gameObject.GetComponentInParent<PlayerPhysics>();
            vrCameraTransform = gameObject.transform.parent.parent;
        }

        void OnAnimatorIK(int layerIndex) {
            //Debug.Log("UnityIK.OnAnimatorIK");
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeight);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, ikWeight);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, ikWeight);

            anim.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, ikWeight);
            anim.SetIKHintPositionWeight(AvatarIKHint.RightElbow, ikWeight);

            anim.SetIKPosition(AvatarIKGoal.LeftHand, leftHandIkTarget.position + leftHandIkTarget.TransformVector(leftHandPositionOffset));
            anim.SetIKPosition(AvatarIKGoal.RightHand, rightHandIkTarget.position + rightHandIkTarget.TransformVector(rightHandPositionOffset));
            anim.SetIKRotation(AvatarIKGoal.LeftHand, leftHandIkTarget.rotation * Quaternion.Euler(leftHandRotationOffset));
            anim.SetIKRotation(AvatarIKGoal.RightHand, rightHandIkTarget.rotation * Quaternion.Euler(rightHandRotationOffset));
            anim.SetIKHintPosition(AvatarIKHint.LeftElbow, leftElbowIkTarget.position);
            anim.SetIKHintPosition(AvatarIKHint.RightElbow, rightElbowIkTarget.position);

            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, ikWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, ikWeight);

            Quaternion forwardVector = Quaternion.Euler(new Vector3(0, vrCameraTransform.rotation.eulerAngles.y, 0));
            anim.SetIKPosition(AvatarIKGoal.LeftFoot, new Vector3(vrCameraTransform.position.x, playerPhysics.transform.position.y, vrCameraTransform.position.z) + forwardVector * leftFootOffset);
            anim.SetIKPosition(AvatarIKGoal.RightFoot, new Vector3(vrCameraTransform.position.x, playerPhysics.transform.position.y, vrCameraTransform.position.z) + forwardVector * rightFootOffset);
            anim.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.identity);
            anim.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.identity);

            anim.SetIKHintPosition(AvatarIKHint.LeftKnee, new Vector3(vrCameraTransform.position.x, playerPhysics.transform.position.y, vrCameraTransform.position.z) + forwardVector * leftFootOffset + leftKneeOffset);
            anim.SetIKHintPosition(AvatarIKHint.RightKnee, new Vector3(vrCameraTransform.position.x, playerPhysics.transform.position.y, vrCameraTransform.position.z) + forwardVector * rightFootOffset + rightKneeOffset);
        }
    }
}