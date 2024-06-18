using System.Collections;
using ABI_RC.Core;
using ABI_RC.Core.Player;
using NAK.OriginShift.Components;
using NAK.OriginShift.Extensions;
using UnityEngine;

#if !UNITY_EDITOR
using ABI_RC.Systems.Movement;
#endif

namespace NAK.OriginShift
{
    [DefaultExecutionOrder(int.MaxValue)]
    public class OriginShiftMonitor : MonoBehaviour
    {
#if !UNITY_EDITOR
        private PlayerSetup _playerSetup;
        private BetterBetterCharacterController _characterController;
#endif
        
        #region Unity Events

        private void Start()
        {
#if !UNITY_EDITOR
            _playerSetup = GetComponent<PlayerSetup>();
            _characterController = GetComponent<BetterBetterCharacterController>();
#endif
            OriginShiftManager.OnPostOriginShifted += OnPostOriginShifted;
            StartCoroutine(FixedUpdateCoroutine());
        }

        private void OnDestroy()
        {
            OriginShiftManager.OnPostOriginShifted -= OnPostOriginShifted;
            StopAllCoroutines();
        }
        
        private void LateFixedUpdate()
        {
            // in CVR use GetPlayerPosition to account for VR offset
            Vector3 position = PlayerSetup.Instance.GetPlayerPosition();
            
            // respawn height check
            Vector3 absPosition = OriginShiftManager.GetAbsolutePosition(position);
            if (absPosition.y < BetterBetterCharacterController.Instance.respawnHeight)
            {
                RootLogic.Instance.Respawn();
                return;
            }
            
            float halfThreshold = OriginShiftController.ORIGIN_SHIFT_THRESHOLD / 2; // i keep forgetting this
            if (Mathf.Abs(position.x) > halfThreshold
                || Mathf.Abs(position.y) > halfThreshold
                || Mathf.Abs(position.z) > halfThreshold)
                OriginShiftManager.Instance.ShiftOrigin(position);
        }
        
        #endregion Unity Events
        
        #region Origin Shift Events
        
        private void OnPostOriginShifted(Vector3 shift)
        {
#if UNITY_EDITOR
            // shift our transform back
            transform.position += shift;
#else 
            _characterController.OffsetBy(shift);
            _playerSetup.OffsetAvatarMovementData(shift);
#endif
        }
    
        #endregion Origin Shift Events
        
        #region LateFixedUpdate Implementation
        
        private readonly YieldInstruction _fixedUpdateYield = new WaitForFixedUpdate();
        
        private IEnumerator FixedUpdateCoroutine()
        {
            while (true)
            {
                yield return _fixedUpdateYield;
                LateFixedUpdate(); // we need to run after all physics (specifically, character controller)
            }
            // ReSharper disable once IteratorNeverReturns
        }

        #endregion LateFixedUpdate Implementation
    }
}