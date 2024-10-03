namespace DataAccessProvider.Abstractions
{
    public abstract class BaseDataSourceParams<T> where T : class
    {
        private T? _value;

        public T? Value => _value;

        public void SetValue(T value) => _value = value;
    }

    public abstract class BaseDataSourceParams
    {
        private object? _value;

        public object? Value => _value;

        public void SetValue(object value) => _value = value;
    }

}
