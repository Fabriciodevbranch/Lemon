using LemonWriter.Domain.Common;

namespace LemonWriter.Domain.Entities;

public sealed class CharacterMediaReference : Entity<Guid>
{
    public Guid BookId { get; private set; }
    public Guid CharacterId { get; private set; }
    public Guid MediaId { get; private set; }
    public string Role { get; private set; } = "Other";
    public int DisplayOrder { get; private set; }
    private CharacterMediaReference() { }
    public static CharacterMediaReference Create(Guid bookId, Guid characterId, Guid mediaId, string role, int order) => new()
        { Id = Guid.NewGuid(), BookId = bookId, CharacterId = characterId, MediaId = mediaId, Role = role, DisplayOrder = order };
    public void Update(string role, int order) { Role = role; DisplayOrder = Math.Max(0, order); }
}

public sealed class CharacterTimelineReference : Entity<Guid>
{
    public Guid BookId { get; private set; }
    public Guid CharacterId { get; private set; }
    public Guid EventId { get; private set; }
    public string Role { get; private set; } = "Participant";
    public string? Note { get; private set; }
    private CharacterTimelineReference() { }
    public static CharacterTimelineReference Create(Guid bookId, Guid characterId, Guid eventId, string role, string? note) => new()
        { Id = Guid.NewGuid(), BookId = bookId, CharacterId = characterId, EventId = eventId, Role = role, Note = note?.Trim() };
    public void Update(string role, string? note) { Role = role; Note = note?.Trim(); }
}

public sealed class CharacterCustomAttribute : Entity<Guid>
{
    public Guid BookId { get; private set; }
    public Guid CharacterId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string ValueType { get; private set; } = "ShortText";
    public string Value { get; private set; } = string.Empty;
    public string? GroupName { get; private set; }
    public int DisplayOrder { get; private set; }
    private CharacterCustomAttribute() { }
    public static CharacterCustomAttribute Create(Guid bookId, Guid characterId, string label, string type, string value, string? group, int order) => new()
        { Id = Guid.NewGuid(), BookId = bookId, CharacterId = characterId, Label = label.Trim(), ValueType = type, Value = value.Trim(), GroupName = group?.Trim(), DisplayOrder = Math.Max(0, order) };
    public void Update(string label, string type, string value, string? group, int order)
        { Label = label.Trim(); ValueType = type; Value = value.Trim(); GroupName = group?.Trim(); DisplayOrder = Math.Max(0, order); }
}

public sealed class CharacterAttributeOption : Entity<Guid>
{
    public Guid AttributeId { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    private CharacterAttributeOption() { }
    public static CharacterAttributeOption Create(Guid attributeId, string value, int order) => new()
        { Id = Guid.NewGuid(), AttributeId = attributeId, Value = value.Trim(), DisplayOrder = order };
}
