/********************************************************************************//**
\file      VelocityTracker.cs
\brief     Tracks velocity of an object during a window of time.
\copyright Copyright 2015 Oculus VR, LLC All Rights reserved.
************************************************************************************/

using UnityEngine;

//namespace OvrTouch.Hands {
namespace Tumbler {

    public class VelocityTracker : MonoBehaviour {

        //==============================================================================
        // Nested Types
        //==============================================================================

        private static class Const {

            public const float WindowTime = 1.0f / 90.0f;
            public const float WindowEpsilon = 0.0001f;
            public const float LinearSpeedWindow = WindowTime * 8.0f;
            public const float LinearVelocityWindow = WindowTime * 4.0f;
            public const float AngularVelocityWindow = WindowTime * 2.0f;
            public const int MaxSamples = 45;

        }

        private struct Sample {

            public float Time;
            public float LinearSpeed;
            public Vector3 LinearVelocity;
            public Vector3 AngularVelocity;

        }

        //==============================================================================
        // Fields
        //==============================================================================

        [SerializeField] private bool m_showGizmos = false;

        private int m_index = -1;
        private int m_count = 0;
        private Sample[] m_samples = new Sample[Const.MaxSamples];

        private Vector3 m_position = Vector3.zero;
        private Quaternion m_rotation = Quaternion.identity;

        private Vector3 m_frameLinearVelocity = Vector3.zero;
        private Vector3 m_frameAngularVelocity = Vector3.zero;
        private Vector3 m_trackedLinearVelocity = Vector3.zero;
        private Vector3 m_trackedAngularVelocity = Vector3.zero;

        //==============================================================================
        // Properties
        //==============================================================================

        public Vector3 FrameAngularVelocity {
            get { return m_frameAngularVelocity; }
        }

        public Vector3 FrameLinearVelocity {
            get { return m_frameLinearVelocity; }
        }

        public Vector3 TrackedAngularVelocity {
            get { return m_trackedAngularVelocity; }
        }

        public Vector3 TrackedLinearVelocity {
            get { return m_trackedLinearVelocity; }
        }

        //==============================================================================
        // Public
        //==============================================================================

        //==============================================================================
		public void Activate ()
		{
			enabled = true;
		}

        //==============================================================================
		public void Deactivate ()
		{
			enabled = false;
		}

        //==============================================================================
        // MonoBehaviour
        //==============================================================================

        //==============================================================================
        private void Awake () {
            m_position = this.transform.position;
            m_rotation = this.transform.rotation;
        }

        //==============================================================================
        private void FixedUpdate () {
            // Compute delta position
            Vector3 finalPosition = this.transform.position;
            Vector3 deltaPosition = finalPosition - m_position;
            m_position = finalPosition;

            // Compute delta rotation
            Quaternion finalRotation = this.transform.rotation;
            Vector3 deltaRotation = DeltaRotation(finalRotation, m_rotation) * Mathf.Deg2Rad;
            m_rotation = finalRotation;

            // Add the sample
            AddSample(deltaPosition, deltaRotation);

            // Update tracked velocities
            m_frameLinearVelocity = m_samples[m_index].LinearVelocity;
            m_frameAngularVelocity = m_samples[m_index].AngularVelocity;
            m_trackedLinearVelocity = ComputeAverageLinearVelocity().normalized * ComputeMaxLinearSpeed();
            m_trackedAngularVelocity = ComputeAverageAngularVelocity();
        }

        //==============================================================================
        private void OnDrawGizmos () {
            if (!m_showGizmos) {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawRay(this.transform.position, TrackedLinearVelocity);
        }

        //==============================================================================
        // Private
        //==============================================================================

        //==============================================================================
        private Vector3 DeltaRotation (Quaternion final, Quaternion initial) {
            Vector3 finalEuler = final.eulerAngles;
            Vector3 initialEuler = initial.eulerAngles;
            Vector3 deltaRotation = new Vector3(
                Mathf.DeltaAngle(initialEuler.x, finalEuler.x),
                Mathf.DeltaAngle(initialEuler.y, finalEuler.y),
                Mathf.DeltaAngle(initialEuler.z, finalEuler.z)
            );
            return deltaRotation;
        }

        //==============================================================================
        private void AddSample (Vector3 deltaPosition, Vector3 deltaRotation) {
            // Compute the next index and count
            m_index = (m_index + 1) % m_samples.Length;
            m_count = Mathf.Min(m_count + 1, m_samples.Length);

            // Compute sample values
            float sampleTime = Time.time;
            Vector3 sampleLinearVelocity = deltaPosition / Time.deltaTime;
            Vector3 sampleAngularVelocity = deltaRotation / Time.deltaTime;

            // Add the sample
            m_samples[m_index] = new Sample {
                Time = sampleTime,
                LinearVelocity = sampleLinearVelocity,
                AngularVelocity = sampleAngularVelocity,
            };
            m_samples[m_index].LinearSpeed = ComputeAverageLinearVelocity().magnitude;
        }

        //==============================================================================
        private int Count () {
            return Mathf.Min(m_count, m_samples.Length);
        }

        //==============================================================================
        private int IndexPrev (int index) {
            return (index == 0) ? m_count - 1 : index - 1;
        }

        //==============================================================================
        private bool IsSampleValid (int index, float windowSize) {
            float dt = Time.time - m_samples[index].Time;
            bool isSampleValid = (
                (windowSize - dt >= Const.WindowEpsilon) || // Determine if delta time falls within the time window size
                (index == m_index)                          // Use at least one sample regardless of how much time has elapsed
            );
            return isSampleValid;
        }

        //==============================================================================
        private Vector3 ComputeAverageAngularVelocity () {
            int index = m_index;
            int count = Count();

            int velocityCount = 0;
            Vector3 angularVelocity = Vector3.zero;
            for (int i = 0; i < count; ++i) {
                // Determine if the sample is valid
                if (!IsSampleValid(index, Const.AngularVelocityWindow)) {
                    break;
                }

                // Store the velocity
                velocityCount += 1;
                angularVelocity += m_samples[index].AngularVelocity;
                index = IndexPrev(index);
            }

            if (velocityCount > 1) {
                // Average the velocity
                angularVelocity /= (float)velocityCount;
            }

            return angularVelocity;
        }

        //==============================================================================
        private Vector3 ComputeAverageLinearVelocity () {
            int index = m_index;
            int count = Count();

            int velocityCount = 0;
            Vector3 linearVelocity = Vector3.zero;
            for (int i = 0; i < count; ++i) {
                // Determine if the sample is valid
                if (!IsSampleValid(index, Const.LinearVelocityWindow)) {
                    break;
                }

                // Store the velocity
                velocityCount += 1;
                linearVelocity += m_samples[index].LinearVelocity;
                index = IndexPrev(index);
            }

            if (velocityCount > 1) {
                // Average the velocity
                linearVelocity /= (float)velocityCount;
            }

            return linearVelocity;
        }

        //==============================================================================
        private float ComputeMaxLinearSpeed () {
            int index = m_index;
            int count = Count();

            float maxSpeed = 0.0f;
            for (int i = 0; i < count; ++i) {
                // Determine if the sample is valid
                if (!IsSampleValid(index, Const.LinearSpeedWindow)) {
                    break;
                }

                // Store the speed
                maxSpeed = Mathf.Max(maxSpeed, m_samples[index].LinearSpeed);
                index = IndexPrev(index);
            }

            return maxSpeed;
        }

    }

}
