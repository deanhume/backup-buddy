namespace backup_buddy.tests;

public class ProgramTests
{
    [Fact]
    public void SanitizeFileName_RemovesInvalidCharacters()
    {
        // Arrange
        string unsafeFileName = "test:file<name>with|invalid*chars?.txt";
        
        // Act
        string safeFileName = Program.SanitizeFileName(unsafeFileName);
        
        // Assert
        Assert.DoesNotContain(':', safeFileName);
        Assert.DoesNotContain('<', safeFileName);
        Assert.DoesNotContain('>', safeFileName);
        Assert.DoesNotContain('|', safeFileName);
        Assert.DoesNotContain('*', safeFileName);
        Assert.DoesNotContain('?', safeFileName);
    }
}
