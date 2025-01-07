﻿using UnityEngine;

namespace NAK.RCCVirtualSteeringWheel;

public class SteeringWheelRoot : MonoBehaviour
{
    #region Static Variables

    private static readonly Dictionary<RCC_CarControllerV3, SteeringWheelRoot> ActiveWheels = new();

    #endregion Static Variables

    #region Static Methods

    public static bool TryGetWheelInput(RCC_CarControllerV3 carController, out float steeringInput)
    {
        if (ActiveWheels.TryGetValue(carController, out SteeringWheelRoot wheel) && wheel._averageAngle != 0f)
        {
            steeringInput = wheel.GetNormalizedValue();
            return true;
        }

        steeringInput = 0f;
        return false;
    }

    public static void SetupSteeringWheel(RCC_CarControllerV3 carController, Bounds steeringWheelBounds)
    {
        Transform steeringWheel = carController.SteeringWheel;
        if (carController == null) return;

        SteeringWheelRoot wheel = steeringWheel.gameObject.AddComponent<SteeringWheelRoot>();
        wheel._carController = carController;
        
        Array.Resize(ref wheel._pickups, 2);
        CreatePickup(out wheel._pickups[0]);
        CreatePickup(out wheel._pickups[1]);
        
        return;

        void CreatePickup(out SteeringWheelPickup wheelPickup)
        {
            GameObject pickup = new()
            {
                transform =
                {
                    parent = steeringWheel.transform,
                    localPosition = Vector3.zero,
                    localRotation = Quaternion.identity,
                    localScale = Vector3.one
                }
            };

            BoxCollider collider = pickup.AddComponent<BoxCollider>();
            collider.size = steeringWheelBounds.size;
            collider.center = steeringWheelBounds.center;

            wheelPickup = pickup.AddComponent<SteeringWheelPickup>();
            wheelPickup.root = wheel;
        }
    }

    #endregion Static Methods

    #region Public Properties

    private bool IsWheelBeingHeld => _pickups[0].IsPickedUp || _pickups[1].IsPickedUp;

    #endregion Public Properties

    #region Private Variables

    private RCC_CarControllerV3 _carController;
    private SteeringWheelPickup[] _pickups;
    
    private float _originalSteeringWheelAngleMultiplier;
    private float _originalSteeringWheelSign;
    
    private readonly List<Transform> _trackedTransforms = new();
    private readonly List<Vector3> _lastPositions = new();
    private readonly List<float> _totalAngles = new();
    
    private bool _isTracking;
    private float _averageAngle;
    private float _timeWheelReleased = -1f;
    private const float RETURN_TO_CENTER_DURATION = 2f;

    #endregion Private Variables

    #region Unity Events

    private void Start()
    {
        ActiveWheels.TryAdd(_carController, this);
        InitializeWheel();
    }

    private void Update()
    {
        if (_carController == null) return;
        UpdateWheelState();
        UpdateSteeringBehavior();
    }

    private void OnDestroy()
    {
        ActiveWheels.Remove(_carController);
    }

    #endregion Unity Events

    #region Public Methods

    internal void StartTrackingTransform(Transform trans)
    {
        if (trans == null) return;

        var currentAngle = 0f;
        if (_isTracking)
        {
            var sum = 0f;
            var validTransforms = 0;
            for (var i = 0; i < _trackedTransforms.Count; i++)
                if (_trackedTransforms[i] != null)
                {
                    sum += _totalAngles[i];
                    validTransforms++;
                }

            if (validTransforms > 0)
                currentAngle = sum / validTransforms;
        }

        _trackedTransforms.Add(trans);
        _lastPositions.Add(GetLocalPositionWithoutRotation(transform.position));
        _totalAngles.Add(currentAngle);
        _isTracking = true;
    }

    internal void StopTrackingTransform(Transform trans)
    {
        var index = _trackedTransforms.IndexOf(trans);
        if (index == -1) return;

        var currentAverage = CalculateCurrentAverage();
        _trackedTransforms.RemoveAt(index);
        _lastPositions.RemoveAt(index);
        _totalAngles.RemoveAt(index);

        if (_trackedTransforms.Count <= 0) 
            return;
        
        for (var i = 0; i < _totalAngles.Count; i++)
            _totalAngles[i] = currentAverage;
    }

    private float CalculateCurrentAverage()
    {
        var sum = 0f;
        var validTransforms = 0;
        for (var i = 0; i < _trackedTransforms.Count; i++)
            if (_trackedTransforms[i] != null)
            {
                sum += _totalAngles[i];
                validTransforms++;
            }

        return validTransforms > 0 ? sum / validTransforms : 0f;
    }

    #endregion Public Methods

    #region Private Methods

    private void InitializeWheel()
    {
        _originalSteeringWheelAngleMultiplier = _carController.steeringWheelAngleMultiplier;
        _originalSteeringWheelSign = Mathf.Sign(_originalSteeringWheelAngleMultiplier);
        _carController.useCounterSteering = false;
        _carController.useSteeringSmoother = false;
    }

    private void UpdateWheelState()
    {
        var isHeld = IsWheelBeingHeld;
        if (!isHeld && _timeWheelReleased < 0f)
            _timeWheelReleased = Time.time;
        else if (isHeld)
            _timeWheelReleased = -1f;
    }

    private void UpdateSteeringBehavior()
    {
        UpdateSteeringMultiplier();

        if (IsWheelBeingHeld)
            UpdateRotationTracking();
        else if (_timeWheelReleased >= 0f)
            HandleWheelReturn();
    }

    private void UpdateSteeringMultiplier()
    {
        _carController.steeringWheelAngleMultiplier = ModSettings.EntryOverrideSteeringRange.Value
            ? ModSettings.EntryCustomSteeringRange.Value * _originalSteeringWheelSign / _carController.steerAngle
            : _originalSteeringWheelAngleMultiplier;
    }

    private void HandleWheelReturn()
    {
        var timeSinceRelease = Time.time - _timeWheelReleased;
        if (timeSinceRelease < RETURN_TO_CENTER_DURATION)
        {
            var t = timeSinceRelease / RETURN_TO_CENTER_DURATION;
            _averageAngle = Mathf.Lerp(_averageAngle, 0f, t);

            for (var i = 0; i < _totalAngles.Count; i++)
                _totalAngles[i] = _averageAngle;
        }
        else
        {
            _averageAngle = 0f;
            for (var i = 0; i < _totalAngles.Count; i++)
                _totalAngles[i] = 0f;
        }
    }

    private float GetMaxSteeringRange()
    {
        return _carController.steerAngle * Mathf.Abs(_carController.steeringWheelAngleMultiplier);
    }

    private float GetSteeringWheelSign()
    {
        return _originalSteeringWheelSign * (ModSettings.EntryInvertSteering.Value ? 1f : -1f);
    }

    private Vector3 GetSteeringWheelLocalAxis()
    {
        return _carController.steeringWheelRotateAround switch
        {
            RCC_CarControllerV3.SteeringWheelRotateAround.XAxis => Vector3.right,
            RCC_CarControllerV3.SteeringWheelRotateAround.YAxis => Vector3.up,
            RCC_CarControllerV3.SteeringWheelRotateAround.ZAxis => Vector3.forward,
            _ => Vector3.forward
        };
    }

    private Vector3 GetLocalPositionWithoutRotation(Vector3 worldPosition)
    {
        Transform steeringTransform = _carController.SteeringWheel;
        Quaternion localRotation = steeringTransform.localRotation;
        steeringTransform.localRotation = _carController.orgSteeringWheelRot;
        Vector3 localPosition = steeringTransform.InverseTransformPoint(worldPosition);
        steeringTransform.localRotation = localRotation;
        return localPosition;
    }

    private float GetNormalizedValue()
    {
        return Mathf.Clamp(_averageAngle / GetMaxSteeringRange(), -1f, 1f) * GetSteeringWheelSign();
    }

    private void UpdateRotationTracking()
    {
        if (!_isTracking || _trackedTransforms.Count == 0) return;

        Vector3 trackingAxis = GetSteeringWheelLocalAxis();
        UpdateTransformAngles(trackingAxis);
        UpdateAverageAngle();
    }

    private void UpdateTransformAngles(Vector3 trackingAxis)
    {
        for (var i = 0; i < _trackedTransforms.Count; i++)
        {
            if (_trackedTransforms[i] == null) continue;

            Vector3 currentPosition = GetLocalPositionWithoutRotation(_trackedTransforms[i].position);
            if (currentPosition == _lastPositions[i]) continue;

            Vector3 previousVector = Vector3.ProjectOnPlane(_lastPositions[i], trackingAxis).normalized;
            Vector3 currentVector = Vector3.ProjectOnPlane(currentPosition, trackingAxis).normalized;

            if (previousVector.sqrMagnitude > 0.001f && currentVector.sqrMagnitude > 0.001f)
            {
                var deltaAngle = Vector3.SignedAngle(previousVector, currentVector, trackingAxis);
                if (Mathf.Abs(deltaAngle) < 90f)
                    _totalAngles[i] += deltaAngle;
            }

            _lastPositions[i] = currentPosition;
        }
    }

    private void UpdateAverageAngle()
    {
        var sumAngles = 0f;
        var validTransforms = 0;

        for (var i = 0; i < _trackedTransforms.Count; i++)
        {
            if (_trackedTransforms[i] == null) continue;
            sumAngles += _totalAngles[i];
            validTransforms++;
        }

        if (validTransforms > 0)
            _averageAngle = sumAngles / validTransforms;
    }

    #endregion Private Methods
}