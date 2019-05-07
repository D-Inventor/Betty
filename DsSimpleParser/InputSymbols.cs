namespace DsSimpleParser
{
    public struct InputSymbols<T>
    {
        public InputSymbols(T[] symbols, int index = 0)
        {
            Symbols = symbols;
            Index = index;
        }

        public T[] Symbols { get; }
        public int Index { get; set; }
        public T Current { get { return Symbols[Index]; } }
    }
}
