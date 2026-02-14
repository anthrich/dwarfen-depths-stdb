using SharedPhysics;

namespace UnitTests;

public class SharedPhysicsEngineTests
{
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
                Position = new Vector2(1, 1),
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
            result[0].Position
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
                Position = new Vector2(1, 1),
                SequenceId = 1,
                Direction = new Vector2(-1, 0).Normalized(),
                Speed = 1f,
                Rotation = 90
            }
        };
        
        // Act
        var result = Engine.Simulate(1, 2, entities, []);
        
        // Assert
        Assert.Equal(new Vector2 { X = 0.5f, Y = 1 }, result[0].Position);
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
                Position = new Vector2(1, 1),
                SequenceId = 1,
                Direction = new Vector2(strafeDirection, 0).Normalized(),
                Speed = 1f,
                Rotation = 0
            }
        };
        
        // Act
        var result = Engine.Simulate(1, 2, entities, []);
        
        // Assert
        Assert.Equal(new Vector2 { X = 1 + strafeDirection, Y = 1 }, result[0].Position);
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
                Position = new Vector2(0, 0),
                SequenceId = 1,
                Direction = new Vector2(0, 1f).Normalized(),
                Speed = 1f,
                Rotation = -10.7f
            }
        };
        
        // Act
        var result = Engine.Simulate(1, 2, entities, []);
        
        // Assert
        Assert.Equal(new Vector2 { X = 0, Y = 1f }, result[0].Position);
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
                Position = new Vector2(2, 4),
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
            result[0].Position
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
                Position = new Vector2(2, 4),
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
                Position = new Vector2(1, 1),
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
}