namespace WebAPI.Models
{
    public class TodoItem
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; } //TODO: Multiple access
        public bool IsComplete { get; set; }
    }
}
