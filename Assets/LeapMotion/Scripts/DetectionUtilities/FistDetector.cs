using UnityEngine;
using Leap.Unity.Attributes;
using UnityEngine.Serialization;

namespace Leap.Unity {

  /// <summary>
  /// A basic utility class to aid in creating fist based actions.  Once linked with an IHandModel, it can
  /// be used to detect fist gestures that the hand makes.
  /// </summary>
  public class FistDetector : AbstractGrabDetector {
    protected const float MM_TO_M = 0.001f;

    [Tooltip("The strength at which to enter the fisting state.")]
    [Header("Strength Settings")]
    [MinValue(0)]
    [Units("meters")]
    [FormerlySerializedAs("_activateFistDist")]
    public float ActivateStrength = .7f; //meters
    [Tooltip("The strength at which to leave the fisting state.")]
    [MinValue(0)]
    [Units("meters")]
    [FormerlySerializedAs("_deactivateFistDist")]
    public float DeactivateStrength = .3f; //meters

    public bool IsFisting { get { return this.IsHolding; } }
    public bool DidStartFist { get { return this.DidStartHold; } }
    public bool DidEndFist { get { return this.DidRelease; } }

    protected bool _isFisting = false;

    protected float _lastFistTime = 0.0f;
    protected float _lastUnfistTime = 0.0f;

    protected Vector3 _fistPos;
    protected Quaternion _fistRotation;

    protected virtual void OnValidate() {
      ActivateStrength = Mathf.Max(0, ActivateStrength);
      DeactivateStrength = Mathf.Max(0, DeactivateStrength);

      //Activate value cannot be less than deactivate value
      if (DeactivateStrength > ActivateStrength) {
        DeactivateStrength = ActivateStrength;
      }
    }

    protected override void ensureUpToDate() {
      if (Time.frameCount == _lastUpdateFrame) {
        return;
      }
      _lastUpdateFrame = Time.frameCount;

      _didChange = false;

      Hand hand = _handModel.GetLeapHand();

      if (hand == null || !_handModel.IsTracked) {
        changeState(false);
        return;
      }

      _strength = hand.GrabStrength;
      _rotation = hand.Basis.CalculateRotation();
      _position = hand.PalmPosition.ToVector3();

      if (IsActive) {
        if (_strength < DeactivateStrength) {
          changeState(false);
          //return;
        }
      } else {
        if (_strength > ActivateStrength) {
          changeState(true);
        }
      }

      if (IsActive) {
        _lastPosition = _position;
        _lastRotation = _rotation;
        _lastStrength = _strength;
        _lastDirection = _direction;
        _lastNormal = _normal;
      }
      if (ControlsTransform) {
        transform.position = _position;
        transform.rotation = _rotation;
      }
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos () {
      if (ShowGizmos && _handModel != null && _handModel.IsTracked) {
        Color centerColor = Color.clear;
        Vector3 centerPosition = Vector3.zero;
        Quaternion circleRotation = Quaternion.identity;
        if (IsHolding) {
          centerColor = Color.green;
          centerPosition = Position;
          circleRotation = Rotation;
        } else {
          Hand hand = _handModel.GetLeapHand();
          if (hand != null) {
            Finger thumb = hand.Fingers[0];
            Finger index = hand.Fingers[1];
            centerColor = Color.red;
            centerPosition = ((thumb.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint + index.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint) / 2).ToVector3();
            circleRotation = hand.Basis.CalculateRotation();
          }
        }
        Vector3 axis;
        float angle;
        circleRotation.ToAngleAxis(out angle, out axis);
        Utils.DrawCircle(centerPosition, axis, ActivateStrength / 2, centerColor);
        Utils.DrawCircle(centerPosition, axis, DeactivateStrength / 2, Color.blue);
      }
    }
    #endif
  }
}
