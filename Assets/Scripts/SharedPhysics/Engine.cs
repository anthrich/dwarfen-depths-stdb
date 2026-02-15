using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedPhysics
{
    public static class Engine
    {
        [ThreadStatic] private static List<Line>? _nearbyLinesBuffer;
        public const float Gravity = -19.62f;
        public const float JumpImpulse = 8f;
        public const float MaxSlopeAngle = 45f;
        public const float GroundSnapDistance = 0.1f;
        public const float TerminalVelocity = -50f;

        public static Vector2? GetIntersection(Line line1, Line line2)
        {
            var p1 = line1.Start;
            var p2 = line1.End;
            var p3 = line2.Start;
            var p4 = line2.End;

            var d1 = p2 - p1; // Direction of line 1
            var d2 = p4 - p3; // Direction of line 2

            var cross = d1.X * d2.Y - d1.Y * d2.X;

            if (Math.Abs(cross) < 0.0001f)
                return default; // Parallel lines

            var t = ((p3.X - p1.X) * d2.Y - (p3.Y - p1.Y) * d2.X) / cross;
            var u = ((p3.X - p1.X) * d1.Y - (p3.Y - p1.Y) * d1.X) / cross;

            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                return p1 + d1 * t;
            }

            return default;
        }

        public static Line[] GetNearbyLines(Line line1, Line[] allLines)
        {
            var movementBounds = BoundingBox.FromLine(line1);
            return allLines.Where(line => BoundingBox.FromLine(line).Overlaps(movementBounds)).ToArray();
        }

        public static Vector2 ProjectMovementOntoSurface(
            Vector2 normalizedDirectionXZ, float surfaceSpeed, Triangle triangle, float maxSlopeAngle)
        {
            var normal = Triangle.GetNormal(triangle);
            var slopeAngle = Triangle.GetSlopeAngle(triangle);

            var direction3D = new Vector3(normalizedDirectionXZ.X, 0, normalizedDirectionXZ.Y);

            var projected = direction3D - normal * Vector3.Dot(direction3D, normal);
            var projectedMag = projected.GetMagnitude();

            if (projectedMag < 0.0001f)
                return Vector2.Zero;

            var surfaceDir = projected / projectedMag;

            if (slopeAngle > maxSlopeAngle && surfaceDir.Y > 0.001f)
            {
                var contour = Vector3.Cross(normal, Vector3.Up).Normalized();
                var contourDot = Vector3.Dot(surfaceDir, contour);
                surfaceDir = contour * contourDot;
                var contourMag = surfaceDir.GetMagnitude();
                if (contourMag < 0.0001f)
                    return Vector2.Zero;
                surfaceDir = surfaceDir / contourMag;
            }

            var movement3D = surfaceDir * surfaceSpeed;
            return new Vector2(movement3D.X, movement3D.Z);
        }

        public static Entity[] Simulate(
            float deltaTime, ulong sequenceId, Entity[] entities, Line[] lines)
        {
            return Simulate(deltaTime, sequenceId, entities, new LineGrid(lines), null);
        }

        public static Entity[] Simulate(
            float deltaTime, ulong sequenceId, Entity[] entities, Line[] lines, TerrainGrid? terrain)
        {
            return Simulate(deltaTime, sequenceId, entities, new LineGrid(lines), terrain);
        }

        public static Entity[] Simulate(
            float deltaTime, ulong sequenceId, Entity[] entities, LineGrid lineGrid, TerrainGrid? terrain)
        {
            var processed = new Entity[entities.Length];
            var i = 0;
            _nearbyLinesBuffer ??= new List<Line>();

            foreach (var entity in entities)
            {
                var forwardDirection = Entity.GetForwardDirection(entity);
                var normalizedDirection = entity.Direction.Normalized();
                var isBackpedalling = Vector2.Dot(normalizedDirection, forwardDirection) < -0.01f;
                var backpedalMultiplier = isBackpedalling ? 0.5f : 1f;
                var surfaceSpeed = entity.Speed * backpedalMultiplier * deltaTime;

                Vector2 targetMovement;

                if (terrain != null)
                {
                    var currentTriangle = terrain.GetTriangle(entity.Position.ToXZ());
                    if (currentTriangle.HasValue && normalizedDirection.SqrMagnitude > 0.0001f)
                    {
                        targetMovement = ProjectMovementOntoSurface(
                            normalizedDirection, surfaceSpeed, currentTriangle.Value, MaxSlopeAngle);
                    }
                    else
                    {
                        targetMovement = normalizedDirection * surfaceSpeed;
                    }
                }
                else
                {
                    targetMovement = normalizedDirection * surfaceSpeed;
                }

                var currentPositionXZ = entity.Position.ToXZ();
                var targetPositionXZ = currentPositionXZ + targetMovement;
                var movementLine = new Line(currentPositionXZ, targetPositionXZ);
                lineGrid.GetNearbyLines(BoundingBox.FromLine(movementLine), _nearbyLinesBuffer);

                foreach (var line in _nearbyLinesBuffer)
                {
                    var intersection = GetIntersection(movementLine, line);
                    if (!intersection.HasValue) continue;
                    var safeDistance = (intersection.Value - currentPositionXZ).Normalized() * 0.05f;
                    var safeMovement = intersection.Value - safeDistance - currentPositionXZ;
                    var safePosition = currentPositionXZ + safeMovement;
                    var remainingMovement = targetMovement - safeMovement;
                    var glideMovement = Line.GlideAlong(line, remainingMovement);
                    targetPositionXZ = safePosition + glideMovement;
                    movementLine = new Line(currentPositionXZ, targetPositionXZ);
                }

                processed[i] = entity;
                processed[i].SequenceId = sequenceId;

                if (terrain != null)
                {
                    var groundHeight = terrain.GetGroundHeight(targetPositionXZ);

                    if (entity.IsGrounded)
                    {
                        if (groundHeight.HasValue)
                        {
                            float heightDifference = entity.Position.Y - groundHeight.Value;
                            if (heightDifference > GroundSnapDistance)
                            {
                                // Walking off a cliff - become airborne
                                processed[i].Position = Vector3.FromXZ(targetPositionXZ, entity.Position.Y);
                                processed[i].IsGrounded = false;
                                processed[i].VerticalVelocity = 0;
                            }
                            else
                            {
                                // Normal ground following
                                processed[i].Position = Vector3.FromXZ(targetPositionXZ, groundHeight.Value);
                                processed[i].IsGrounded = true;
                                processed[i].VerticalVelocity = 0;
                            }
                        }
                        else
                        {
                            // Off-mesh while grounded: become airborne at current height
                            processed[i].Position = Vector3.FromXZ(targetPositionXZ, entity.Position.Y);
                            processed[i].IsGrounded = false;
                            processed[i].VerticalVelocity = 0;
                        }
                    }
                    else
                    {
                        // Airborne: apply gravity
                        var verticalVelocity = entity.VerticalVelocity + Gravity * deltaTime;
                        verticalVelocity = Math.Max(verticalVelocity, TerminalVelocity);
                        var newY = entity.Position.Y + verticalVelocity * deltaTime;

                        if (groundHeight.HasValue && newY <= groundHeight.Value)
                        {
                            // Landing
                            processed[i].Position = Vector3.FromXZ(targetPositionXZ, groundHeight.Value);
                            processed[i].VerticalVelocity = 0;
                            processed[i].IsGrounded = true;
                        }
                        else
                        {
                            processed[i].Position = Vector3.FromXZ(targetPositionXZ, newY);
                            processed[i].VerticalVelocity = verticalVelocity;
                            processed[i].IsGrounded = false;
                        }
                    }
                }
                else
                {
                    // Legacy 2D mode: position stays as Vector2-equivalent
                    processed[i].Position = Vector3.FromXZ(targetPositionXZ, entity.Position.Y);
                }

                i++;
            }

            return processed;
        }
    }
}
