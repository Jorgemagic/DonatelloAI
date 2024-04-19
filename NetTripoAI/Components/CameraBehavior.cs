﻿using Evergine.Common.Input.Keyboard;
using Evergine.Common.Input.Mouse;
using Evergine.Common.Input.Pointer;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System;
using System.Text;

namespace NetTripoAI.Components
{
    public class CameraBehavior : Behavior
    {
        private struct MoveStruct
        {
            public float moveForward;
            public float moveBackward;
            public float moveLeft;
            public float moveRight;
            public float moveUp;
            public float moveDown;

            public float yaw;
            public float pitch;
            public float roll;

            public void Clear()
            {
                this.moveForward = 0.0f;
                this.moveBackward = 0.0f;
                this.moveLeft = 0.0f;
                this.moveRight = 0.0f;
                this.moveUp = 0.0f;
                this.moveDown = 0.0f;

                this.yaw = 0.0f;
                this.pitch = 0.0f;
                this.roll = 0.0f;
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append("FW = ");
                sb.Append(this.moveForward);
                sb.Append("; BW = ");
                sb.Append(this.moveBackward);
                sb.Append("; L = ");
                sb.Append(this.moveLeft);
                sb.Append("; R = ");
                sb.Append(this.moveRight);
                sb.Append("; U = ");
                sb.Append(this.moveUp);
                sb.Append("; D = ");
                sb.Append(this.moveDown);

                sb.Append("; Yaw = ");
                sb.Append(this.yaw);
                sb.Append("; Pitch = ");
                sb.Append(this.pitch);
                sb.Append("; Roll = ");
                sb.Append(this.roll);

                return sb.ToString();
            }
        }

        private MoveStruct moveStruct = new MoveStruct();
        private PointerPoint currentTouchMovePoint;
        private PointerPoint currentTouchOrientationPoint;
        private Point initPosition;
        private Point initOrientation;

        /// <summary>
        /// The Transform component of the entity to spin (own entity by default).
        /// </summary>
        [BindComponent(false)]
        private Transform3D transform = null;

        /// <summary>
        /// The Camera component of the entity (own entity by default).
        /// </summary>
        [BindComponent(false)]
        private Camera camera = null;

        /// <summary>
        /// The Camera3D Graphics Presenter.
        /// </summary>
        [BindService]
        protected GraphicsPresenter graphicsPresenter;

        /// <summary>
        /// Gets or sets the move speed of the camera.
        /// </summary>
        public float MoveSpeed { get; set; }

        /// <summary>
        /// Gets or sets the rotation speed of the camera.
        /// </summary>
        public float RotationSpeed { get; set; }

        /// <summary>
        /// Gets or sets the touch sensibility.
        /// </summary>
        /// <remarks>
        /// 0.5 is for stop, 1 is for raw delta, 2 is twice delta.
        /// </remarks>
        public float TouchSensibility { get; set; }

        /// <summary>
        /// Gets or sets the Mouse sensibility.
        /// </summary>
        /// <remarks>
        /// 0.5 is for stop, 1 is for raw delta, 2 is twice delta.
        /// </remarks>
        public float MouseSensibility { get; set; }

        /// <summary>
        /// Gets or sets the maximum pitch angle.
        /// </summary>
        public float MaxPitch { get; set; }

        /// <summary>
        /// Gets or sets the Move/Orientation screen ratio.
        /// </summary>
        /// <remarks>
        /// 0.5f sets the same area to move and orientation actions. 0.1f sets the 10% of the screen to move action and 90% to orientation area.
        /// </remarks>
        public float TouchMoveAndOrientationRatio { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FreeCamera3D"/> class.
        /// </summary>
        public CameraBehavior()
        {
            this.MoveSpeed = 5.0f;
            this.RotationSpeed = 5.0f;
            this.TouchSensibility = 1.0f;
            this.MouseSensibility = 0.03f;
            this.TouchMoveAndOrientationRatio = 0.5f;
            this.MaxPitch = MathHelper.PiOver2 * 0.95f;

            this.moveStruct.Clear();
        }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            this.moveStruct.Clear();

            this.HandleInput();
            this.UpdatePositionAndOrientation((float)gameTime.TotalSeconds);
        }

        /// <summary>
        /// Helper method to calculate displacement using configurable speed and amount.
        /// </summary>
        /// <param name="director">Direction Vector.</param>
        /// <param name="maxCurrentSpeed">Max Speed.</param>
        /// <param name="amount">Movement proportion. 0 = stop, 1 = max movement.</param>
        /// <param name="displacement">Output vector.</param>
        private void Displacement(Vector3 director, float maxCurrentSpeed, float amount, ref Vector3 displacement)
        {
            var elapsedAmount = maxCurrentSpeed * amount;

            // Manual in-line: position += speed * forward;
            displacement.X = displacement.X + (elapsedAmount * director.X);
            displacement.Y = displacement.Y + (elapsedAmount * director.Y);
            displacement.Z = displacement.Z + (elapsedAmount * director.Z);
        }

        /// <summary>
        /// Updates the entity transform component.
        /// </summary>
        /// <param name="elapsed">Elapsed time in seconds.</param>
        private void UpdatePositionAndOrientation(float elapsed)
        {
            Vector3 displacement = Vector3.Zero;
            Matrix4x4 localTransform = this.transform.LocalTransform;

            var elapsedMaxSpeed = elapsed * this.MoveSpeed;

            if (this.moveStruct.moveForward != 0.0f)
            {
                this.Displacement(localTransform.Forward, elapsedMaxSpeed, this.moveStruct.moveForward, ref displacement);
            }
            else if (this.moveStruct.moveBackward != 0.0f)
            {
                this.Displacement(localTransform.Backward, elapsedMaxSpeed, this.moveStruct.moveBackward, ref displacement);
            }

            if (this.moveStruct.moveLeft != 0.0f)
            {
                this.Displacement(localTransform.Left, elapsedMaxSpeed, this.moveStruct.moveLeft, ref displacement);
            }
            else if (this.moveStruct.moveRight != 0.0f)
            {
                this.Displacement(localTransform.Right, elapsedMaxSpeed, this.moveStruct.moveRight, ref displacement);
            }

            if (this.moveStruct.moveUp != 0.0f)
            {
                this.Displacement(localTransform.Up, elapsedMaxSpeed, this.moveStruct.moveUp, ref displacement);
            }
            else if (this.moveStruct.moveDown != 0.0f)
            {
                this.Displacement(localTransform.Down, elapsedMaxSpeed, this.moveStruct.moveDown, ref displacement);
            }

            // Manual in-line: camera.Position = position;
            this.transform.LocalPosition += displacement;

            // Rotation:
            var rotation = this.transform.LocalRotation;
            rotation.Y += this.moveStruct.yaw * this.RotationSpeed * (1 / 60f);
            rotation.X += this.moveStruct.pitch * this.RotationSpeed * (1 / 60f);

            // Limit Pitch Angle
            rotation.X = MathHelper.Clamp(rotation.X, -this.MaxPitch, this.MaxPitch);
            this.transform.LocalRotation = rotation;
        }

        /// <summary>
        /// Handles every input device.
        /// </summary>
        private void HandleInput()
        {
            this.HandleKeyboard();
            this.HandleMouse();
            this.HandleTouch();
        }

        /// <summary>
        /// Handles Keyboard Input.
        /// </summary>
        private void HandleKeyboard()
        {
            var display = this.camera.Display;

            if (display == null)
            {
                return;
            }

            var keyboardDispatcher = display.KeyboardDispatcher;

            // Keyboard Speed modifier
            var currentSpeed = 1.0f;

            if (keyboardDispatcher == null)
            {
                return;
            }

            if (keyboardDispatcher.IsKeyDown(Keys.LeftShift))
            {
                currentSpeed *= 2.0f;
            }
            else if (keyboardDispatcher.IsKeyDown(Keys.LeftControl))
            {
                currentSpeed /= 2.0f;
            }

            if (keyboardDispatcher.IsKeyDown(Keys.W))
            {
                this.moveStruct.moveForward = currentSpeed;
            }
            else if (keyboardDispatcher.IsKeyDown(Keys.S))
            {
                this.moveStruct.moveBackward = currentSpeed;
            }

            if (keyboardDispatcher.IsKeyDown(Keys.A))
            {
                this.moveStruct.moveLeft = currentSpeed;
            }
            else if (keyboardDispatcher.IsKeyDown(Keys.D))
            {
                this.moveStruct.moveRight = currentSpeed;
            }

            if (keyboardDispatcher.IsKeyDown(Keys.Q))
            {
                this.moveStruct.moveUp = currentSpeed;
            }
            else if (keyboardDispatcher.IsKeyDown(Keys.E))
            {
                this.moveStruct.moveDown = currentSpeed;
            }

            if (keyboardDispatcher.IsKeyDown(Keys.Up))
            {
                this.moveStruct.pitch = currentSpeed;
            }
            else if (keyboardDispatcher.IsKeyDown(Keys.Down))
            {
                this.moveStruct.pitch = -currentSpeed;
            }

            if (keyboardDispatcher.IsKeyDown(Keys.Left))
            {
                this.moveStruct.yaw = currentSpeed;
            }
            else if (keyboardDispatcher.IsKeyDown(Keys.Right))
            {
                this.moveStruct.yaw = -currentSpeed;
            }
        }

        /// <summary>
        /// Handles Mouse Input.
        /// </summary>
        private void HandleMouse()
        {
            var display = this.camera.Display;

            if (display == null)
            {
                return;
            }

            var mouseDispatcher = display.MouseDispatcher;

            if (mouseDispatcher?.IsButtonDown(MouseButtons.Left) == true)
            {
                var positionDelta = mouseDispatcher.PositionDelta;
                this.moveStruct.yaw = -positionDelta.X * this.MouseSensibility;
                this.moveStruct.pitch = -positionDelta.Y * this.MouseSensibility;
            }
        }

        /// <summary>
        /// Handles Touch Input.
        /// </summary>
        private void HandleTouch()
        {
            var display = this.camera.Display;

            if (display == null)
            {
                return;
            }

            var touchDispatcher = display.TouchDispatcher;

            if (touchDispatcher == null)
            {
                return;
            }

            float moveDividerSensibility = this.TouchSensibility / 100;
            for (int i = 0; i < touchDispatcher.Points.Count; i++)
            {
                var point = touchDispatcher.Points[i];

                // Release Move and/or Orientation Points
                if (point.State == Evergine.Common.Input.ButtonState.Releasing)
                {
                    if (this.currentTouchMovePoint != null && point.Id == this.currentTouchMovePoint.Id)
                    {
                        this.currentTouchMovePoint = null;
                    }

                    if (this.currentTouchOrientationPoint != null && point.Id == this.currentTouchOrientationPoint.Id)
                    {
                        this.currentTouchOrientationPoint = null;
                    }
                }

                // Move
                if (this.currentTouchMovePoint != null && point.Id == this.currentTouchMovePoint.Id)
                {
                    var delta = this.initPosition - point.Position;

                    if (delta.Y > 0)
                    {
                        this.moveStruct.moveForward = (float)delta.Y * moveDividerSensibility;
                    }
                    else
                    {
                        this.moveStruct.moveBackward = -(float)delta.Y * moveDividerSensibility;
                    }

                    if (delta.X > 0)
                    {
                        this.moveStruct.moveLeft = (float)delta.X * moveDividerSensibility;
                    }
                    else
                    {
                        this.moveStruct.moveRight = -(float)delta.X * moveDividerSensibility;
                    }
                }

                // Orientation
                if (this.currentTouchOrientationPoint != null && point.Id == this.currentTouchOrientationPoint.Id)
                {
                    var delta = this.initOrientation - point.Position;

                    this.moveStruct.yaw = ((float)delta.X * this.TouchSensibility) / display.Width;
                    this.moveStruct.pitch = ((float)delta.Y * this.TouchSensibility) / display.Height;
                }

                // Press Move and/or Orientation Points
                if (point.State == Evergine.Common.Input.ButtonState.Pressing)
                {
                    var ratio = display.Width * this.TouchMoveAndOrientationRatio;

                    // Left and Right screen part press: move and orientation
                    if ((point.Position.X < ratio)
                        && this.currentTouchMovePoint == null)
                    {
                        this.currentTouchMovePoint = point;
                        this.initPosition = point.Position;
                    }
                    else if ((point.Position.X > ratio)
                        && this.currentTouchOrientationPoint == null)
                    {
                        this.currentTouchOrientationPoint = point;
                        this.initOrientation = point.Position;
                    }
                }
            }
        }
    }
}
