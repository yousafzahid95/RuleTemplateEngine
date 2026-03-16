using RuleTemplateEngine.Interfaces;

namespace RuleTemplateEngine.Helpers
{
    public static class TransformToIDataRecord<T>
    {
        public static IEnumerable<IDataRecord> TransformFromObject(T data, string namePrefix)
        {
            return
            [
                new CustomDataRecord<T>(data, namePrefix)
            ];
        }

        public static IEnumerable<IDataRecord> TransformFromList(IList<T> data, string namePrefix)
        {
            return data.Select(d => new CustomDataRecord<T>(d, namePrefix));
        }
    }

    public static class TransformToIDataRecord
    {
        public static IEnumerable<IDataRecord> TransformFromObject(object data, string namePrefix)
        {
            var recordType = typeof(EventCustomDataRecord<>).MakeGenericType(data.GetType());
            var record = (IDataRecord)Activator.CreateInstance(recordType, data, namePrefix)!;
            return [record];
        }

        public static IEnumerable<IDataRecord> TransformFromList(IList<object> data, string namePrefix)
        {
            return data.Select(d =>
            {
                var recordType = typeof(EventCustomDataRecord<>).MakeGenericType(d.GetType());
                return (IDataRecord)Activator.CreateInstance(recordType, d, namePrefix)!;
            });
        }
    }
}
