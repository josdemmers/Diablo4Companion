namespace D4Companion.Entities
{
    public class AppLanguage
    {
        public AppLanguage()
        {
            
        }
        public AppLanguage(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
