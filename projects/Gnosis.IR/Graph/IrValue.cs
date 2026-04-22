namespace Gnosis.IR.Graph;

public sealed class IrValue
{
    #region Properties

    public int Id { get; }

    public IrType Type { get; }

    public string? Name { get; internal set; }

    #endregion

    #region Constructors

    public IrValue(int id, IrType type, string? name = null)
    {
        Id = id;
        Type = type;
        Name = name;
    }

    #endregion

    #region Public Methods

    public override string ToString()
    {
        return Name is not null ? $"%{Name}" : $"%{Id}";
    }

    #endregion
}
