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
}
