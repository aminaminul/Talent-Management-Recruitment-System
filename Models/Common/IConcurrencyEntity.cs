namespace TalentManagement.Models.Common;

public interface IConcurrencyEntity
{
    byte[]? RowVersion { get; set; }
}
