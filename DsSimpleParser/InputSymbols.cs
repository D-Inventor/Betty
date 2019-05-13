namespace DsSimpleParser
{
    public struct InputSymbols<T>
    {
        public InputSymbols(T[] symbols, int index = 0)
        {
            Symbols = symbols;
            Index = index;
        }

        public T this[int i] { get { return Symbols[i]; } }
        public T[] Symbols { get; }
        public int Index { get; set; }
        public T Current { get { return Symbols[Index]; } }
        public bool EndOfInput { get { return Symbols.Length <= Index; } }
    }
}
