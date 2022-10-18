using System;
using FishNet;
using FishNet.Object;
using FishNet.Object.Prediction;
using UnityEngine;

namespace SpaceEdge
{
    public class PlayerShipMovement : NetworkBehaviour
    {
        public bool CanMove { set; private get; } = true;

        [SerializeField, Range(0.1f, 100)] private float moveRate = 48f;
        [SerializeField, Range(0.1f, 100)] private float rotateRate = 4.8f;
        [SerializeField, Range(0.1f, 100)] private float pitchClamp = 48f;

        private CharacterController _characterController;

        public Action OnMoveComplete;

        private void Awake()
        {
            InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;
            _characterController = GetComponent<CharacterController>();
        }

        private void OnDestroy()
        {
            if (InstanceFinder.TimeManager != null) InstanceFinder.TimeManager.OnTick -= TimeManager_OnTick;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _characterController.enabled = IsServer || IsOwner;
        }

        private void TimeManager_OnTick()
        {
            if (!CanMove) return;
            if (IsOwner)
            {
                Reconciliation(default, false);
                CheckInput(out var md);
                Move(md, false);
            }

            if (IsServer)
            {
                Move(default, true);
                var rd = new ReconcileData(transform.position, transform.rotation);
                Reconciliation(rd, true);
            }
        }

        private void CheckInput(out MoveData md)
        {
            md = default;


            md = new MoveData(InputPollingSystem.MoveInput, InputPollingSystem.RotateInput);
        }

        [Replicate]
        private void Move(MoveData md, bool asServer, bool replaying = false)
        {
            var delta = (float)TimeManager.TickDelta;

            var rotation = transform.eulerAngles;
            rotation.x = rotation.x - md.Delta.y * rotateRate * delta;
            rotation.x = ClampAngle(rotation.x, -pitchClamp, pitchClamp);

            rotation.y = rotation.y + md.Delta.x * rotateRate * delta;
            transform.rotation = Quaternion.Euler(rotation);


            var move = md.Direction.x * moveRate * transform.right + md.Direction.y * moveRate * transform.forward;
            move *= moveRate * delta;
            _characterController.Move(move);

            if (!asServer && !replaying)
                OnMoveComplete?.Invoke();
        }

        [Reconcile]
        private void Reconciliation(ReconcileData rd, bool asServer)
        {
            transform.position = rd.Position;
            transform.rotation = rd.Rotation;
        }



        private float ClampAngle(float angle, float min, float max)
        {
            if (min < 0 && max > 0 && (angle > max || angle < min))
            {
                angle -= 360;
                if (angle > max || angle < min)
                {
                    if (Mathf.Abs(Mathf.DeltaAngle(angle, min)) < Mathf.Abs(Mathf.DeltaAngle(angle, max))) return min;
                    else return max;
                }
            }
            else if (min > 0 && (angle > max || angle < min))
            {
                angle += 360;
                if (angle > max || angle < min)
                {
                    if (Mathf.Abs(Mathf.DeltaAngle(angle, min)) < Mathf.Abs(Mathf.DeltaAngle(angle, max))) return min;
                    else return max;
                }
            }

            if (angle < min) return min;
            else if (angle > max) return max;
            else return angle;
        }

        #region Structs.

        public struct MoveData
        {
            public Vector2 Direction;
            public Vector2 Delta;

            public MoveData(Vector2 direction, Vector2 delta)
            {
                Direction = direction;
                Delta = delta;
            }
        }

        public struct ReconcileData
        {
            public Vector3 Position;
            public Quaternion Rotation;

            public ReconcileData(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }
        }

        #endregion
    }
}