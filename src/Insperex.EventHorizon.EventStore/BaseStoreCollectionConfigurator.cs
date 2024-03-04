namespace Insperex.EventHorizon.EventStore
{
    public class BaseStoreCollectionConfigurator<T>
    {
        internal string Database;
        internal string Collection;

        public BaseStoreCollectionConfigurator<T> WithDatabase(string database)
        {
            Database = database;
            return this;
        }

        public BaseStoreCollectionConfigurator<T> WithCollection(string database)
        {
            Collection = database;
            return this;
        }
    }
}
