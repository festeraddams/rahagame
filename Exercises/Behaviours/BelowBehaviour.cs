﻿namespace HSA.RehaGame.Exercises.Behaviours
{
    using System;
    using UI.VisualExercise;
    using User;
    using Windows.Kinect;

    public class BelowBehaviour : BaseJointBehaviour
    {
        public BelowBehaviour(string unityObjectName, PatientJoint activeJoint, PatientJoint passiveJoint, Drawing drawing) : base(unityObjectName, activeJoint, passiveJoint, drawing)
        {

        }

        public override bool IsFulfilled(Body body)
        {
            isFulfilled = body.Joints[activeJoint.JointType].Position.Y < body.Joints[passiveJoint.JointType].Position.Y;
            return isFulfilled;
        }

        public override void VisualInformation(Body body)
        {
            
        }
    }
}