namespace SqlMimic.Core.Abstractions
{
    /// <summary>
    /// Type of SQL statement
    /// </summary>
    public enum SqlStatementType
    {
        Unknown,
        Select,
        Insert,
        Update,
        Delete,
        Merge,
        CreateTable,
        AlterTable,
        DropTable,
        CreateIndex,
        DropIndex,
        Truncate
    }
}