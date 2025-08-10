namespace UnitTests;

public class PhysicsTests
{
    [Fact]
    public void Line_intersection_is_detected_correctly()
    {
        // Arrange
        var line1 = new Module.Line { Start = new DbVector2(2, 4), End = new DbVector2(0, 6)};
        var line2 = new Module.Line { Start = new DbVector2(1, 1), End = new DbVector2(1, 10)};

        // Act
        var intersection = Physics.GetIntersection(line1, line2);
        
        // Assert
        Assert.Equal(new DbVector2(1, 5), intersection);
    }

    [Fact]
    public void Line_nearness_is_detected_correctly()
    {
        // Arrange
        var line1 = new Module.Line { Start = new DbVector2(2, 4), End = new DbVector2(0, 6)};
        var nearLine = new Module.Line { Start = new DbVector2(1, 1), End = new DbVector2(1, 10)};
        
        // Act
        var nearbyLines = Physics.GetNearbyLines(line1, [nearLine]);
        
        // Assert
        Assert.Equal([nearLine], nearbyLines);
    }
    
    [Fact]
    public void Far_lines_are_detected_correctly()
    {
        // Arrange
        var line1 = new Module.Line { Start = new DbVector2(2, 4), End = new DbVector2(0, 6)};
        var nearLine = new Module.Line { Start = new DbVector2(1, 1), End = new DbVector2(1, 10)};
        var farLine = new Module.Line { Start = new DbVector2(1, 10), End = new DbVector2(10, 10)};
        
        // Act
        var nearbyLines = Physics.GetNearbyLines(line1, [nearLine, farLine]);
        
        // Assert
        Assert.Equal([nearLine], nearbyLines);
    }
}