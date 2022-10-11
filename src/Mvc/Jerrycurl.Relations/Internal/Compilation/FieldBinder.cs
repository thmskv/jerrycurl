namespace Jerrycurl.Relations.Internal.Compilation
{
    internal delegate void FieldBinder<TParent, TValue>(TParent parent, int index, TValue value);
}
