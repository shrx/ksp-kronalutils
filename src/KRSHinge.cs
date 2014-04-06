using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KronalUtils
{
    class KRSHinge : PartModule
    {
        [KSPField]
        public Vector3 axis = Vector3.down; /// Default is down so positive angle is CW and negative is CCW
        [KSPField]
        public Vector3 anchor = Vector3.zero;
        [KSPField]
        public float angularVelocity = 22.5f;
        [KSPField]
        public float angularVelocityPrecision = 7.2f;
        [KSPField]
        public float power = 100f;
        [KSPField]
        public float powerConsumption = 0.25f; /// ElectricCharge/deg
        [KSPField]
        public float angleMin = float.NegativeInfinity;
        [KSPField]
        public float angleMax = float.PositiveInfinity;
        [KSPField]
        public float angleSnap = 1f;
        [KSPField]
        public bool autoReverse = false;
        [KSPField]
        public string meshStatic = "";
        [KSPField(isPersistant = true, guiActive = true, guiName = "Enabled")]
        public bool IsEnabled;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Angle", guiUnits = " deg", guiFormat="F1")]
        public float angle = 0f;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Angular Velocity", guiUnits = " deg/s")]
        public float currentAngularVelocity;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Fixed")]
        public bool isFixed = false;

        [KRSInput("Rotate CW", true)]
        public string inputRotateCW = "";
        [KRSInput("Rotate CCW", true)]
        public string inputRotateCCW = "";
        [KRSInput("Rotate Analog", true)]
        public string inputRotateAnalog = ""; /// Allows to use a gamepad analog stick to control rotation

        private ConfigurableJoint joint;
        private float angleStart;
        private float direction;
        private Vector3 parentDir;
        private Vector3 parentAxis;
        private bool targetAngleSet;
        private float targetAngle;
        private int targetAngleWaitTime;

        public override void OnStart(PartModule.StartState state)
        {
            print(new StackTrace(true).GetFrame(0).GetFileLineNumber());
            this.part.force_activate();
            this.angleStart = this.angle;
            InitializeStatic(this.angle);
            if (this.part.parent)
            {
                this.parentAxis = this.part.parent.transform.InverseTransformDirection(this.transform.TransformDirection(this.axis));
                this.parentDir = this.part.parent.transform.InverseTransformDirection(this.transform.TransformDirection(KRSUtils.VectorSwap(this.axis)));
            }

            if (this.isFixed)
            {
                SetTargetAngle(this.angle);
            }
        }

        private void InitializeStatic(float delta)
        {
            var t = KSPUtil.FindInPartModel(this.transform, meshStatic);
            if (!t) return;

            t.RotateAround(this.transform.TransformPoint(this.anchor), this.part.transform.TransformDirection(this.axis), delta);
            if (this.part.parent)
            {
                t.parent = this.part.parent.transform;
            }
        }

        private void InitializeJoint()
        {
            if ((!this.part.isAttached) || (this.part.attachJoint == this.joint))
            {
                return;
            }

            if (this.part.attachJoint != null)
            {
                GameObject.Destroy(this.part.attachJoint);
            }

            this.joint = gameObject.AddComponent<ConfigurableJoint>();
            this.joint.connectedBody = this.part.parent.Rigidbody;
            this.joint.axis = Vector3.Cross(this.axis.normalized, Vector3.right);
            this.joint.anchor = this.anchor;
            this.joint.breakForce = this.part.breakingForce;
            this.joint.breakTorque = this.part.breakingTorque;
            this.joint.xMotion = ConfigurableJointMotion.Locked;
            this.joint.yMotion = ConfigurableJointMotion.Locked;
            this.joint.zMotion = ConfigurableJointMotion.Locked;
            this.joint.angularXMotion = ConfigurableJointMotion.Free;
            this.joint.angularYMotion = ConfigurableJointMotion.Free;
            this.joint.angularZMotion = ConfigurableJointMotion.Free;
            this.joint.projectionMode = JointProjectionMode.PositionAndRotation;
            this.joint.rotationDriveMode = RotationDriveMode.Slerp;
            this.joint.slerpDrive = new JointDrive
            {
                mode = JointDriveMode.Position,
                maximumForce = 10f,
                positionDamper = 0f,
                positionSpring = this.power
            };

            this.part.attachMethod = AttachNodeMethod.HINGE_JOINT;
            this.part.attachJoint = this.joint;

            foreach (Part child in this.part.FindChildParts<Part>(true))
            {
                child.force_activate();
            }
        }

        public override void OnUpdate()
        {
            if ((this.vessel == null) || (this.vessel != FlightGlobals.ActiveVessel)) return;

            float dir = Mathf.Clamp(KRSInputAttribute.GetInputValue(inputRotateAnalog) +
                                    KRSInputAttribute.GetInputValue(inputRotateCW) -
                                    KRSInputAttribute.GetInputValue(inputRotateCCW), -1f, 1f);
            if (Math.Abs(dir) > 0.001f)
            {
                Rotate(dir);
            }
            else if (Math.Abs(this.currentAngularVelocity) > 0.001f)
            {
                Rotate(0f);
            }
        }

        public override void OnFixedUpdate()
        {
            CalcAngle();

            if ((TimeWarp.CurrentRate > 1f && TimeWarp.WarpMode == TimeWarp.Modes.HIGH) || (!this.IsEnabled) || (this.vessel == null)) return;

            InitializeJoint();
            CalcCurrentAngularVelocity();

            float dT = TimeWarp.fixedDeltaTime;
            var dA = this.currentAngularVelocity * dT;
            var nextAngle = this.angle + dA;
            var maxAngularVelocity = this.angularVelocity * dT;

            if (nextAngle < this.angleMin)
            {
                dA = CalcDirectionAndGetDeltaAForLimit(this.angleMin, maxAngularVelocity);
            }
            else if (nextAngle > this.angleMax)
            {
                dA = CalcDirectionAndGetDeltaAForLimit(this.angleMax, maxAngularVelocity);
            }
            else if (this.targetAngleSet)
            {
                if (Math.Abs(nextAngle - this.targetAngle) > 0.05f)
                {
                    dA = CalcDeltaAForTargetAngle(maxAngularVelocity) * 0.75f;
                    this.targetAngleWaitTime = 10;
                }
                else
                {
                    dA = 0f;
                    if (this.targetAngleWaitTime-- == 0)
                    {
                        TargetAngleReached();
                    }
                }
            }

            if (Math.Abs(dA) < 0.00001f) return;

            var dP = this.powerConsumption * Math.Abs(dA);
            if (this.part.RequestResource("ElectricCharge", dP) < dP) return;

            this.joint.targetRotation = Quaternion.Euler(this.joint.targetRotation.eulerAngles + (this.axis * dA));

            UpdateOrgPosAndRot();
        }

        private void CalcAngle()
        {
            /// Calculate angle base upon the difference of the previous parentDir and current.
            /// parentDir is the direction of a vector perpendicular to the axis relative to the parent of this part
            var vdir = this.part.parent.transform.InverseTransformDirection(this.transform.TransformDirection(KRSUtils.VectorSwap(this.axis)));
            var delta = -KRSUtils.VectorSignedAngle(vdir, parentDir, parentAxis);
            this.angle = KRSUtils.Wrap(this.angleStart + delta, -180f, 179.999f);
        }

        private void CalcCurrentAngularVelocity()
        {
            var fih = FlightInputHandler.fetch;
            if ((fih != null) && fih.precisionMode)
            {
                this.currentAngularVelocity = this.angularVelocityPrecision * this.direction;
            }
            else
            {
                this.currentAngularVelocity = this.angularVelocity * this.direction;
            }
        }

        private float CalcDirectionAndGetDeltaAForLimit(float angleLimit, float maxAngularVelocity)
        {
            var dA = Mathf.Clamp(angleLimit - this.angle, -maxAngularVelocity, maxAngularVelocity);
            if (this.autoReverse)
            {
                Rotate(this.direction * -1);
            }
            else
            {
                Rotate(0f);
            }
            return dA;
        }

        private float CalcDeltaAForTargetAngle(float maxAngularVelocity)
        {
            var dA = this.targetAngle - this.angle;
            if (Math.Abs(dA) > 180f) dA = (-1 * Math.Sign(dA) * 360f) - dA;
            dA = Mathf.Clamp(dA, -maxAngularVelocity, maxAngularVelocity);
            return dA;
        }

        private void UpdateOrgPosAndRot()
        {
            this.part.UpdateOrgPosAndRot(this.vessel.rootPart);
            foreach (Part p in this.part.FindChildParts<Part>(true))
            {
                p.UpdateOrgPosAndRot(this.vessel.rootPart);
            }
        }

        private void TargetAngleReached()
        {
            if (!this.isFixed)
            {
                UnsetTargetAngle();
            }
        }

        public void SetTargetAngle(float angle)
        {
            this.targetAngleSet = true;
            this.targetAngle = angle;
            this.targetAngleWaitTime = 10;
        }

        public void UnsetTargetAngle()
        {
            this.targetAngleSet = false;
        }

        public void Rotate(float newDirection)
        {
            this.direction = newDirection;
            if (Math.Abs(newDirection) < 0.001f)
            {
                SetTargetAngle((float)Math.Round(this.angle / this.angleSnap) * this.angleSnap);
            }
            else
            {
                UnsetTargetAngle();
            }
        }

        [KSPEvent(active = true, category = "KSR", guiName = "Toggle", guiIcon = "Toggle", name = "KSR/Toggle", guiActive = true, guiActiveUnfocused = true)]
        public void Toggle(BaseEventData e)
        {          
            IsEnabled = !IsEnabled;
        }

        [KSPEvent(active = true, category = "KRS", guiName = "Fixate", name = "KRS/Fixate", guiActive = true)]
        public void EventFixate()
        {
            this.isFixed = !this.isFixed;
            Rotate(0f);
        }

        [KSPEvent(active = true, category = "KRS", guiName = "Bork!", name = "KRS/Bork", guiActive = true)]
        public void EventBork()
        {
            object a = null;
            print(a.ToString());
        }

        [KSPAction("Rotate CW")]
        public void RotateClockwise(KSPActionParam param)
        {
            if (this.direction != 1)
            {
                Rotate(1f);
            }
            else
            {
                Rotate(0f);
            }
        }

        [KSPAction("Rotate CCW")]
        public void RotateCounterClockwise(KSPActionParam param)
        {
            if (this.direction != -1)
            {
                Rotate(-1f);
            }
            else
            {
                Rotate(0f);
            }
        }

        [KSPAction("Stop")]
        public void RotateNone(KSPActionParam param)
        {
            Rotate(0f);
        }
    }
}
