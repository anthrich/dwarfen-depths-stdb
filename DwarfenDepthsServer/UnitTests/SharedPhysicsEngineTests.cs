using SharedPhysics;

namespace UnitTests;

public class SharedPhysicsEngineTests
{
    private static Vector3 Pos(float x, float z) => new Vector3(x, 0, z);

    private static TerrainGrid FlatTerrain(float y = 0f, float size = 1000f)
    {
        // CCW winding from above for upward-facing normal
        return new TerrainGrid(new[]
        {
            new Triangle(
                new Vector3(-size, y, -size),
                new Vector3(-size, y, size * 2),
                new Vector3(size * 2, y, -size)
            ),
            new Triangle(
                new Vector3(size * 2, y, -size),
                new Vector3(-size, y, size * 2),
                new Vector3(size * 2, y, size * 2)
            )
        });
    }

    [Fact]
    public void Line_intersection_is_detected_correctly()
    {
        // Arrange
        var line1 = new Line { Start = new Vector2(2, 4), End = new Vector2(0, 6)};
        var line2 = new Line { Start = new Vector2(1, 1), End = new Vector2(1, 10)};

        // Act
        var intersection = Engine.GetIntersection(line1, line2);

        // Assert
        Assert.Equal(new Vector2(1, 5), intersection);
    }

    [Fact]
    public void Line_nearness_is_detected_correctly()
    {
        // Arrange
        var line1 = new Line { Start = new Vector2(2, 4), End = new Vector2(0, 6)};
        var nearLine = new Line { Start = new Vector2(1, 1), End = new Vector2(1, 10)};

        // Act
        var nearbyLines = Engine.GetNearbyLines(line1, [nearLine]);

        // Assert
        Assert.Equal([nearLine], nearbyLines);
    }

    [Fact]
    public void Far_lines_are_detected_correctly()
    {
        // Arrange
        var line1 = new Line { Start = new Vector2(2, 4), End = new Vector2(0, 6)};
        var nearLine = new Line { Start = new Vector2(1, 1), End = new Vector2(1, 10)};
        var farLine = new Line { Start = new Vector2(1, 10), End = new Vector2(10, 10)};

        // Act
        var nearbyLines = Engine.GetNearbyLines(line1, [nearLine, farLine]);

        // Assert
        Assert.Equal([nearLine], nearbyLines);
    }

    [Fact]
    public void Simulating_moves_entities_forward()
    {
        // Arrange
        var entities = new Entity[]
        {
            new()
            {
                Id = 1,
                Position = Pos(1, 1),
                SequenceId = 1,
                Direction = new Vector2(1, 1).Normalized(),
                Speed = 10f,
                Rotation = 45f
            }
        };
        const float deltaTime = 0.05f;

        // Act
        var result = Engine.Simulate(deltaTime, 2, entities, []);

        // Assert
        Assert.Equal(
            new Vector2
            {
                X = 1.3535533905932737622004221810524f,
                Y = 1.3535533905932737622004221810524f
            },
            result[0].Position.ToXZ()
        );
    }

    [Fact]
    public void Simulating_moves_entities_backwards_at_half_speed()
    {
        // Arrange
        var entities = new Entity[]
        {
            new()
            {
                Id = 1,
                Position = Pos(1, 1),
                SequenceId = 1,
                Direction = new Vector2(-1, 0).Normalized(),
                Speed = 1f,
                Rotation = 90
            }
        };

        // Act
        var result = Engine.Simulate(1, 2, entities, []);

        // Assert
        Assert.Equal(new Vector2 { X = 0.5f, Y = 1 }, result[0].Position.ToXZ());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Simulating_strafes_entities_at_full_speed(float strafeDirection)
    {
        // Arrange
        var entities = new Entity[]
        {
            new()
            {
                Id = 1,
                Position = Pos(1, 1),
                SequenceId = 1,
                Direction = new Vector2(strafeDirection, 0).Normalized(),
                Speed = 1f,
                Rotation = 0
            }
        };

        // Act
        var result = Engine.Simulate(1, 2, entities, []);

        // Assert
        Assert.Equal(new Vector2 { X = 1 + strafeDirection, Y = 1 }, result[0].Position.ToXZ());
    }

    [Fact]
    public void Simulating_handles_negative_rotations_correctly()
    {
        // Arrange
        var entities = new Entity[]
        {
            new()
            {
                Id = 1,
                Position = Pos(0, 0),
                SequenceId = 1,
                Direction = new Vector2(0, 1f).Normalized(),
                Speed = 1f,
                Rotation = -10.7f
            }
        };

        // Act
        var result = Engine.Simulate(1, 2, entities, []);

        // Assert
        Assert.Equal(new Vector2 { X = 0, Y = 1f }, result[0].Position.ToXZ());
    }

    [Fact]
    public void Simulating_glides_entities_along_lines()
    {
        // Arrange
        var entities = new Entity[]
        {
            new()
            {
                Id = 1,
                Position = Pos(2, 4),
                SequenceId = 1,
                Direction = new Vector2(-2, 2).Normalized(),
                Rotation = 315f,
                Speed = 2f
            }
        };

        var lines = new Line[]
        {
            new() { Start = new Vector2(1, 1), End = new Vector2(1, 10) },
        };

        const float deltaTime = 1f;

        // Act
        var result = Engine.Simulate(deltaTime, 2, entities, lines);

        // Assert
        Assert.Equal(
            new Vector2
            {
                X = 1.0353553f,
                Y = 5.4142137f
            },
            result[0].Position.ToXZ()
        );
    }

    [Fact]
    public void Simulating_with_wall_glide_does_not_trap_entities_inside_walls()
    {
        // Arrange
        var entities = new Entity[]
        {
            new()
            {
                Id = 1,
                Position = Pos(2, 4),
                SequenceId = 1,
                Direction = new Vector2(-2, 2).Normalized(),
                Speed = 2f
            }
        };

        var lines = new Line[]
        {
            new() { Start = new Vector2(1, 1), End = new Vector2(1, 10) },
        };

        const float deltaTime = 1f;

        entities = Engine.Simulate(deltaTime, 2, entities, lines);
        entities[0].Direction = new Vector2(1, 0);

        // Act
        var result = Engine.Simulate(deltaTime, 2, entities, lines);

        // Assert
        Assert.True(result[0].Position.X > 1, "X position should not be stuck in the wall");
    }

    [Fact]
    public void GetMap_returns_valid_map_for_known_name()
    {
        var map = MapData.GetMap("Default");
        Assert.NotEmpty(map.Lines);
    }

    [Fact]
    public void GetMap_throws_for_unknown_name()
    {
        Assert.Throws<ArgumentException>(() => MapData.GetMap("NonexistentMap"));
    }

    [Fact]
    public void Simulating_does_not_clear_rotation()
    {
        // Arrange
        var entities = new Entity[]
        {
            new()
            {
                Id = 1,
                Position = Pos(1, 1),
                SequenceId = 1,
                Direction = new Vector2(1, 1).Normalized(),
                Speed = 10f,
                Rotation = 65f
            }
        };
        const float deltaTime = 0.05f;

        // Act
        var result = Engine.Simulate(deltaTime, 2, entities, []);

        // Assert
        Assert.NotEqual(0, result[0].Rotation);
    }

    // --- 3D Terrain Tests ---

    [Fact]
    public void Simulating_3D_moves_entity_on_flat_terrain()
    {
        var terrain = FlatTerrain();
        var entities = new Entity[]
        {
            new()
            {
                Id = 1,
                Position = new Vector3(0, 0, 0),
                Direction = new Vector2(0, 1).Normalized(),
                Speed = 10f,
                Rotation = 0f,
                IsGrounded = true
            }
        };

        var result = Engine.Simulate(1f, 1, entities, [], terrain);

        Assert.True(result[0].IsGrounded);
        Assert.Equal(0f, result[0].Position.Y, 0.01f);
        Assert.True(result[0].Position.Z > 0, "Entity should have moved forward");
    }

    [Fact]
    public void Simulating_3D_maintains_surface_speed_on_slope()
    {
        // 30-degree slope: rises by D*tan(30°) over D units in Z.
        // Using Z from -50 to 50 (100 units), height from -H to H where H = 50*tan(30°)
        float halfRun = 50f;
        float halfRise = halfRun * MathF.Tan(30f * MathF.PI / 180f);
        var slopedTerrain = new TerrainGrid(new[]
        {
            new Triangle(
                new Vector3(-100, -halfRise, -halfRun),
                new Vector3(-100, halfRise, halfRun),
                new Vector3(100, -halfRise, -halfRun)
            ),
            new Triangle(
                new Vector3(100, -halfRise, -halfRun),
                new Vector3(-100, halfRise, halfRun),
                new Vector3(100, halfRise, halfRun)
            )
        });

        var flatTerrain = FlatTerrain();

        // Entity at origin, ground height at Z=0 is 0
        var slopedEntity = new Entity
        {
            Id = 1, Position = new Vector3(0, 0, 0),
            Direction = new Vector2(0, 1), Speed = 10f, IsGrounded = true
        };

        var flatEntity = new Entity
        {
            Id = 2, Position = new Vector3(0, 0, 0),
            Direction = new Vector2(0, 1), Speed = 10f, IsGrounded = true
        };

        var slopedResult = Engine.Simulate(1f, 1, new[] { slopedEntity }, [], slopedTerrain);
        var flatResult = Engine.Simulate(1f, 1, new[] { flatEntity }, [], flatTerrain);

        // On slope: horizontal distance should be less than on flat
        Assert.True(slopedResult[0].Position.Z < flatResult[0].Position.Z,
            "Horizontal distance on slope should be less than on flat ground");

        // On slope: Y should have increased (entity went uphill)
        Assert.True(slopedResult[0].Position.Y > 0,
            "Entity should have gained height on the slope");
    }

    [Fact]
    public void Simulating_3D_slides_downhill_on_steep_slope()
    {
        // 70-degree slope (exceeds MaxSlopeAngle of 60°)
        float halfRun = 50f;
        float halfRise = halfRun * MathF.Tan(70f * MathF.PI / 180f);
        var steepTerrain = new TerrainGrid(new[]
        {
            new Triangle(
                new Vector3(-100, -halfRise, -halfRun),
                new Vector3(-100, halfRise, halfRun),
                new Vector3(100, -halfRise, -halfRun)
            ),
            new Triangle(
                new Vector3(100, -halfRise, -halfRun),
                new Vector3(-100, halfRise, halfRun),
                new Vector3(100, halfRise, halfRun)
            )
        });

        var entity = new Entity
        {
            Id = 1, Position = new Vector3(0, 0, 0),
            Direction = new Vector2(0, 1), // Attempting to move uphill
            Speed = 10f, IsGrounded = true
        };

        var result = Engine.Simulate(1f, 1, new[] { entity }, [], steepTerrain);

        // Entity should slide downhill (negative Z) despite trying to move uphill
        Assert.True(result[0].Position.Z < -0.1f,
            $"Entity should slide downhill on a steep slope, but Z={result[0].Position.Z}");
    }

    [Fact]
    public void Simulating_3D_slides_downhill_with_no_input_on_steep_slope()
    {
        // 70-degree slope (exceeds MaxSlopeAngle of 60°)
        float halfRun = 50f;
        float halfRise = halfRun * MathF.Tan(70f * MathF.PI / 180f);
        var steepTerrain = new TerrainGrid(new[]
        {
            new Triangle(
                new Vector3(-100, -halfRise, -halfRun),
                new Vector3(-100, halfRise, halfRun),
                new Vector3(100, -halfRise, -halfRun)
            ),
            new Triangle(
                new Vector3(100, -halfRise, -halfRun),
                new Vector3(-100, halfRise, halfRun),
                new Vector3(100, halfRise, halfRun)
            )
        });

        var entity = new Entity
        {
            Id = 1, Position = new Vector3(0, 0, 0),
            Direction = Vector2.Zero, // No input
            Speed = 10f, IsGrounded = true
        };

        var result = Engine.Simulate(1f, 1, new[] { entity }, [], steepTerrain);

        // Even with no input, entity should slide downhill
        Assert.True(result[0].Position.Z < -0.1f,
            $"Entity should slide downhill with no input on steep slope, but Z={result[0].Position.Z}");
    }

    [Fact]
    public void Simulating_3D_stays_grounded_going_downhill_on_slope()
    {
        // 30-degree slope: height decreases as Z increases (downhill in +Z)
        float halfRun = 50f;
        float halfRise = halfRun * MathF.Tan(30f * MathF.PI / 180f);
        var terrain = new TerrainGrid(new[]
        {
            new Triangle(
                new Vector3(-100, halfRise, -halfRun),
                new Vector3(-100, -halfRise, halfRun),
                new Vector3(100, halfRise, -halfRun)
            ),
            new Triangle(
                new Vector3(100, halfRise, -halfRun),
                new Vector3(-100, -halfRise, halfRun),
                new Vector3(100, -halfRise, halfRun)
            )
        });

        var entity = new Entity
        {
            Id = 1, Position = new Vector3(0, 0, 0),
            Direction = new Vector2(0, 1), // Moving downhill (+Z)
            Speed = 10f, Rotation = 0f, IsGrounded = true
        };

        var result = Engine.Simulate(1f, 1, new[] { entity }, [], terrain);

        Assert.True(result[0].IsGrounded,
            $"Entity should stay grounded going downhill, but IsGrounded={result[0].IsGrounded}, Y={result[0].Position.Y}");
        Assert.True(result[0].Position.Y < 0, "Entity should have descended on the slope");
        Assert.True(result[0].Position.Z > 0, "Entity should have moved forward");
    }

    [Fact]
    public void Simulating_3D_entity_falls_with_gravity_when_airborne()
    {
        var terrain = FlatTerrain(y: -10f); // Ground is at Y=-10

        var entity = new Entity
        {
            Id = 1, Position = new Vector3(0, 5, 0),
            Direction = Vector2.Zero, Speed = 0f,
            IsGrounded = false, VerticalVelocity = 0f
        };

        var result = Engine.Simulate(0.1f, 1, new[] { entity }, [], terrain);

        Assert.False(result[0].IsGrounded);
        Assert.True(result[0].Position.Y < 5f, "Entity should have fallen");
        Assert.True(result[0].VerticalVelocity < 0, "Vertical velocity should be negative");
    }

    [Fact]
    public void Simulating_3D_entity_lands_after_falling()
    {
        var terrain = FlatTerrain(y: 0f);

        var entity = new Entity
        {
            Id = 1, Position = new Vector3(0, 0.05f, 0),
            Direction = Vector2.Zero, Speed = 0f,
            IsGrounded = false, VerticalVelocity = -5f
        };

        var result = Engine.Simulate(0.1f, 1, new[] { entity }, [], terrain);

        Assert.True(result[0].IsGrounded, "Entity should have landed");
        Assert.Equal(0f, result[0].Position.Y, 0.01f);
        Assert.Equal(0f, result[0].VerticalVelocity, 0.01f);
    }

    [Fact]
    public void Simulating_3D_entity_walks_off_cliff_becomes_airborne()
    {
        // Terrain with a cliff: ground at Y=10 on left, ground at Y=0 on right
        var terrain = new TerrainGrid(new[]
        {
            // High platform (X: -100 to 5) - CCW winding from above
            new Triangle(
                new Vector3(-100, 10, -100),
                new Vector3(-100, 10, 100),
                new Vector3(5, 10, -100)
            ),
            new Triangle(
                new Vector3(5, 10, -100),
                new Vector3(-100, 10, 100),
                new Vector3(5, 10, 100)
            ),
            // Low ground (X: 5 to 200) - CCW winding from above
            new Triangle(
                new Vector3(5, 0, -100),
                new Vector3(5, 0, 100),
                new Vector3(200, 0, -100)
            ),
            new Triangle(
                new Vector3(200, 0, -100),
                new Vector3(5, 0, 100),
                new Vector3(200, 0, 100)
            )
        });

        var entity = new Entity
        {
            Id = 1, Position = new Vector3(0, 10, 0),
            Direction = new Vector2(1, 0), // Moving +X toward cliff edge
            Speed = 10f, Rotation = 90f, IsGrounded = true
        };

        var result = Engine.Simulate(1f, 1, new[] { entity }, [], terrain);

        // Entity moved past the cliff edge. Ground at new position is Y=0, but entity was at Y=10.
        // The height difference (10) exceeds GroundSnapDistance (0.1), so entity should become airborne.
        Assert.False(result[0].IsGrounded, "Entity should become airborne after walking off cliff");
        Assert.Equal(10f, result[0].Position.Y, 0.01f);
    }

    [Fact]
    public void Simulating_3D_jump_sets_vertical_velocity()
    {
        var terrain = FlatTerrain();

        var entity = new Entity
        {
            Id = 1, Position = new Vector3(0, 0, 0),
            Direction = Vector2.Zero, Speed = 0f,
            IsGrounded = false, // Jump has been initiated (caller sets this)
            VerticalVelocity = Engine.JumpImpulse
        };

        var result = Engine.Simulate(0.1f, 1, new[] { entity }, [], terrain);

        Assert.True(result[0].Position.Y > 0, "Entity should have moved upward");
        Assert.False(result[0].IsGrounded);
    }

    [Fact]
    public void Simulating_3D_jump_and_land_cycle()
    {
        var terrain = FlatTerrain();

        var entity = new Entity
        {
            Id = 1, Position = new Vector3(0, 0, 0),
            Direction = Vector2.Zero, Speed = 0f,
            IsGrounded = false,
            VerticalVelocity = Engine.JumpImpulse
        };

        // Simulate multiple ticks until entity lands
        var current = new[] { entity };
        bool wentUp = false;
        bool landed = false;

        for (int tick = 0; tick < 100; tick++)
        {
            current = Engine.Simulate(0.05f, (ulong)tick, current, [], terrain);
            if (current[0].Position.Y > 0.5f) wentUp = true;
            if (wentUp && current[0].IsGrounded)
            {
                landed = true;
                break;
            }
        }

        Assert.True(wentUp, "Entity should have gone up");
        Assert.True(landed, "Entity should have landed");
        Assert.Equal(0f, current[0].Position.Y, 0.01f);
    }

    [Fact]
    public void Simulating_3D_wall_collision_still_works()
    {
        var terrain = FlatTerrain();
        var lines = new Line[]
        {
            new() { Start = new Vector2(5, -10), End = new Vector2(5, 10) }
        };

        var entity = new Entity
        {
            Id = 1, Position = new Vector3(0, 0, 0),
            Direction = new Vector2(1, 0), // Moving +X toward wall
            Speed = 100f, Rotation = 90f, IsGrounded = true
        };

        var result = Engine.Simulate(1f, 1, new[] { entity }, lines, terrain);

        Assert.True(result[0].Position.X < 5f, "Entity should be stopped by wall");
        Assert.True(result[0].IsGrounded);
    }

    [Fact]
    public void Simulating_3D_off_mesh_clamps_grounded_entity()
    {
        // Small terrain patch
        var terrain = new TerrainGrid(new[]
        {
            new Triangle(
                new Vector3(0, 0, 0),
                new Vector3(0, 0, 10),
                new Vector3(10, 0, 0)
            )
        });

        var entity = new Entity
        {
            Id = 1, Position = new Vector3(5, 0, 2),
            Direction = new Vector2(-1, 0), // Moving toward off-mesh area
            Speed = 100f, Rotation = 270f, IsGrounded = true
        };

        var result = Engine.Simulate(1f, 1, new[] { entity }, [], terrain);

        // Should become airborne since destination is off-mesh
        Assert.False(result[0].IsGrounded);
        Assert.Equal(0f, result[0].Position.Y);
    }

    // --- Vector3 Tests ---

    [Fact]
    public void Vector3_addition_works()
    {
        var a = new Vector3(1, 2, 3);
        var b = new Vector3(4, 5, 6);
        Assert.Equal(new Vector3(5, 7, 9), a + b);
    }

    [Fact]
    public void Vector3_cross_product_works()
    {
        var x = new Vector3(1, 0, 0);
        var y = new Vector3(0, 1, 0);
        var cross = Vector3.Cross(x, y);
        Assert.Equal(new Vector3(0, 0, 1), cross);
    }

    [Fact]
    public void Vector3_dot_product_works()
    {
        var a = new Vector3(1, 0, 0);
        var b = new Vector3(0, 1, 0);
        Assert.Equal(0f, Vector3.Dot(a, b));

        var c = new Vector3(1, 2, 3);
        var d = new Vector3(4, 5, 6);
        Assert.Equal(32f, Vector3.Dot(c, d));
    }

    [Fact]
    public void Vector3_normalize_works()
    {
        var v = new Vector3(3, 0, 4);
        var n = v.Normalized();
        Assert.Equal(1f, n.GetMagnitude(), 0.001f);
        Assert.Equal(0.6f, n.X, 0.001f);
        Assert.Equal(0.8f, n.Z, 0.001f);
    }

    [Fact]
    public void Vector3_ToXZ_and_FromXZ_roundtrip()
    {
        var v3 = new Vector3(1, 5, 3);
        var v2 = v3.ToXZ();
        Assert.Equal(1f, v2.X, 0.001f);
        Assert.Equal(3f, v2.Y, 0.001f);
        var back = Vector3.FromXZ(v2, 5);
        Assert.Equal(v3, back);
    }

    // --- Triangle Tests ---

    [Fact]
    public void Triangle_GetNormal_returns_upward_for_flat_triangle()
    {
        var tri = new Triangle(
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 10),
            new Vector3(10, 0, 0)
        );
        var normal = Triangle.GetNormal(tri);
        Assert.Equal(0f, normal.X, 0.001f);
        Assert.Equal(1f, normal.Y, 0.001f);
        Assert.Equal(0f, normal.Z, 0.001f);
    }

    [Fact]
    public void Triangle_GetSlopeAngle_returns_zero_for_flat()
    {
        var tri = new Triangle(
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 10),
            new Vector3(10, 0, 0)
        );
        Assert.Equal(0f, Triangle.GetSlopeAngle(tri), 0.1f);
    }

    [Fact]
    public void Triangle_GetSlopeAngle_returns_45_for_45_degree_slope()
    {
        var tri = new Triangle(
            new Vector3(0, 0, 0),
            new Vector3(0, 10, 10), // rises 10 units over 10 units = 45 degrees
            new Vector3(10, 0, 0)
        );
        Assert.Equal(45f, Triangle.GetSlopeAngle(tri), 1f);
    }

    [Fact]
    public void Triangle_ContainsXZ_returns_true_for_point_inside()
    {
        var tri = new Triangle(
            new Vector3(0, 0, 0),
            new Vector3(10, 0, 0),
            new Vector3(0, 0, 10)
        );
        Assert.True(Triangle.ContainsXZ(tri, new Vector2(2, 2)));
    }

    [Fact]
    public void Triangle_ContainsXZ_returns_false_for_point_outside()
    {
        var tri = new Triangle(
            new Vector3(0, 0, 0),
            new Vector3(10, 0, 0),
            new Vector3(0, 0, 10)
        );
        Assert.False(Triangle.ContainsXZ(tri, new Vector2(8, 8)));
    }

    [Fact]
    public void Triangle_InterpolateHeight_returns_correct_for_sloped_triangle()
    {
        var tri = new Triangle(
            new Vector3(0, 0, 0),
            new Vector3(0, 10, 10), // height rises linearly with Z
            new Vector3(10, 0, 0)
        );
        // At center-ish point
        var height = Triangle.InterpolateHeight(tri, new Vector2(1, 5));
        Assert.Equal(5f, height, 0.5f);
    }

    // --- TerrainGrid Tests ---

    [Fact]
    public void TerrainGrid_finds_correct_triangle()
    {
        var terrain = FlatTerrain();
        var idx = terrain.FindTriangleIndex(new Vector2(5, 5));
        Assert.True(idx >= 0);
    }

    [Fact]
    public void TerrainGrid_returns_null_off_mesh()
    {
        var terrain = new TerrainGrid(new[]
        {
            new Triangle(
                new Vector3(0, 0, 0),
                new Vector3(0, 0, 10),
                new Vector3(10, 0, 0)
            )
        });
        Assert.Null(terrain.GetGroundHeight(new Vector2(-5, -5)));
    }

    [Fact]
    public void TerrainGrid_returns_correct_height()
    {
        var terrain = FlatTerrain(y: 5f);
        var height = terrain.GetGroundHeight(new Vector2(10, 10));
        Assert.NotNull(height);
        Assert.Equal(5f, height!.Value, 0.01f);
    }

    [Fact]
    public void TerrainGrid_handles_empty_array()
    {
        var terrain = new TerrainGrid(Array.Empty<Triangle>());
        Assert.Null(terrain.GetGroundHeight(new Vector2(0, 0)));
        Assert.Equal(-1, terrain.FindTriangleIndex(new Vector2(0, 0)));
    }

    // --- Heightmap Tests ---

    private static Heightmap FlatHeightmap(float y = 0f, float size = 100f, int resolution = 5)
    {
        var heights = new float[resolution * resolution];
        for (int i = 0; i < heights.Length; i++) heights[i] = y;
        return new Heightmap(heights, resolution, 0f, 0f, size, size);
    }

    [Fact]
    public void Heightmap_returns_correct_height_on_flat_terrain()
    {
        var hm = FlatHeightmap(y: 5f);
        var height = hm.GetGroundHeight(new Vector2(50, 50));
        Assert.NotNull(height);
        Assert.Equal(5f, height!.Value, 0.001f);
    }

    [Fact]
    public void Heightmap_returns_null_outside_bounds()
    {
        var hm = FlatHeightmap(y: 0f, size: 100f);
        Assert.Null(hm.GetGroundHeight(new Vector2(-1, 50)));
        Assert.Null(hm.GetGroundHeight(new Vector2(50, -1)));
        Assert.Null(hm.GetGroundHeight(new Vector2(101, 50)));
        Assert.Null(hm.GetGroundHeight(new Vector2(50, 101)));
    }

    [Fact]
    public void Heightmap_returns_height_at_exact_boundary()
    {
        var hm = FlatHeightmap(y: 3f, size: 100f, resolution: 5);
        var height = hm.GetGroundHeight(new Vector2(100, 100));
        Assert.NotNull(height);
        Assert.Equal(3f, height!.Value, 0.001f);
    }

    [Fact]
    public void Heightmap_returns_height_at_origin()
    {
        var hm = FlatHeightmap(y: 7f, size: 100f, resolution: 5);
        var height = hm.GetGroundHeight(new Vector2(0, 0));
        Assert.NotNull(height);
        Assert.Equal(7f, height!.Value, 0.001f);
    }

    [Fact]
    public void Heightmap_interpolates_height_on_slope()
    {
        // 3x3 heightmap: height increases linearly with X
        var heights = new float[]
        {
            0, 5, 10,
            0, 5, 10,
            0, 5, 10
        };
        var hm = new Heightmap(heights, 3, 0f, 0f, 10f, 10f);

        // At X=2.5, Z=5 (mid-cell): should be ~2.5
        var height = hm.GetGroundHeight(new Vector2(2.5f, 5f));
        Assert.NotNull(height);
        Assert.Equal(2.5f, height!.Value, 0.1f);
    }

    [Fact]
    public void Heightmap_interpolates_correctly_in_second_triangle()
    {
        // 2x2 heightmap: topLeft=0, topRight=10, bottomLeft=10, bottomRight=20
        var heights = new float[] { 0, 10, 10, 20 };
        var hm = new Heightmap(heights, 2, 0f, 0f, 10f, 10f);

        // Point at (7.5, 7.5): fracX=0.75, fracZ=0.75, sum=1.5 -> triangle 2
        // bottomRight=20, (1-fx)*(bottomLeft-bottomRight) + (1-fz)*(topRight-bottomRight)
        // = 20 + 0.25*(10-20) + 0.25*(10-20) = 20 - 2.5 - 2.5 = 15
        var height = hm.GetGroundHeight(new Vector2(7.5f, 7.5f));
        Assert.NotNull(height);
        Assert.Equal(15f, height!.Value, 0.1f);
    }

    [Fact]
    public void Heightmap_point_on_diagonal_uses_triangle_1()
    {
        var heights = new float[] { 0, 10, 10, 20 };
        var hm = new Heightmap(heights, 2, 0f, 0f, 10f, 10f);

        // At (5, 5): fracX=0.5, fracZ=0.5, sum=1.0 -> triangle 1 (<=)
        // height = 0 + 0.5*(10-0) + 0.5*(10-0) = 10
        var height = hm.GetGroundHeight(new Vector2(5f, 5f));
        Assert.NotNull(height);
        Assert.Equal(10f, height!.Value, 0.01f);
    }

    [Fact]
    public void Heightmap_GetTriangle_returns_valid_triangle()
    {
        var hm = FlatHeightmap(y: 5f, size: 100f, resolution: 5);
        var tri = hm.GetTriangle(new Vector2(10, 10));
        Assert.NotNull(tri);
        var normal = Triangle.GetNormal(tri!.Value);
        Assert.Equal(1f, MathF.Abs(normal.Y), 0.001f);
        Assert.Equal(0f, Triangle.GetSlopeAngle(tri.Value), 0.1f);
    }

    [Fact]
    public void Heightmap_GetTriangle_returns_null_outside_bounds()
    {
        var hm = FlatHeightmap(y: 0f, size: 100f);
        Assert.Null(hm.GetTriangle(new Vector2(-1, 50)));
    }

    [Fact]
    public void Heightmap_GetTriangle_has_correct_slope_angle()
    {
        // 2x2 heightmap: height rises with Z
        // topLeft=0, topRight=0, bottomLeft=10, bottomRight=10
        // size = 10x10, rise=10 over run=10 -> 45 degree slope
        var heights = new float[] { 0, 0, 10, 10 };
        var hm = new Heightmap(heights, 2, 0f, 0f, 10f, 10f);

        var tri = hm.GetTriangle(new Vector2(2, 2));
        Assert.NotNull(tri);
        Assert.Equal(45f, Triangle.GetSlopeAngle(tri!.Value), 1f);
    }

    [Fact]
    public void Heightmap_constructor_rejects_wrong_array_size()
    {
        Assert.Throws<ArgumentException>(() => new Heightmap(new float[10], 5, 0, 0, 100, 100));
    }

    [Fact]
    public void Heightmap_with_non_zero_origin()
    {
        var heights = new float[9]; // 3x3, all zeros
        var hm = new Heightmap(heights, 3, 50f, 50f, 100f, 100f);

        Assert.Null(hm.GetGroundHeight(new Vector2(0, 0)));
        Assert.NotNull(hm.GetGroundHeight(new Vector2(100, 100)));
        Assert.NotNull(hm.GetGroundHeight(new Vector2(50, 50)));
    }

    [Fact]
    public void Simulating_3D_with_heightmap_moves_entity_on_flat_terrain()
    {
        var hm = FlatHeightmap(y: 0f, size: 1000f, resolution: 33);
        var entities = new Entity[]
        {
            new()
            {
                Id = 1, Position = new Vector3(500, 0, 500),
                Direction = new Vector2(0, 1).Normalized(),
                Speed = 10f, Rotation = 0f, IsGrounded = true
            }
        };

        var result = Engine.Simulate(1f, 1, entities, Array.Empty<Line>(), hm);

        Assert.True(result[0].IsGrounded);
        Assert.Equal(0f, result[0].Position.Y, 0.01f);
        Assert.True(result[0].Position.Z > 500, "Entity should have moved forward");
    }

    [Fact]
    public void Simulating_3D_with_heightmap_entity_falls_off_edge()
    {
        var hm = FlatHeightmap(y: 10f, size: 20f, resolution: 3);
        var entities = new Entity[]
        {
            new()
            {
                Id = 1, Position = new Vector3(10, 10, 10),
                Direction = new Vector2(1, 0), Speed = 100f,
                Rotation = 90f, IsGrounded = true
            }
        };

        var result = Engine.Simulate(1f, 1, entities, Array.Empty<Line>(), hm);
        Assert.False(result[0].IsGrounded);
    }
}
